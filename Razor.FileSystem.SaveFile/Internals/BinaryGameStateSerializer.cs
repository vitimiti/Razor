// -----------------------------------------------------------------------
// <copyright file="BinaryGameStateSerializer.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Razor.Extensions;

namespace Razor.FileSystem.SaveFile.Internals;

internal static class BinaryGameStateSerializer
{
    private const string Magic = "RZRS";
    private const int Version = 1;

    public static void Save(Stream stream, string saveDisplayName, IEnumerable<ISerializableObject> roots)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Header
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write((byte)0); // padding
        writer.WriteNullTerminatedUtf8(saveDisplayName, Encoding.UTF8);

        var rootList = roots.ToList();
        var context = new SaveContext();

        // Assign IDs to all declared roots first, then write root IDs
        var rootIds = new List<int>(rootList.Count);
        rootIds.AddRange(rootList.Select(context.GetOrAssignId));

        // v2: write root count and root IDs
        writer.Write(rootIds.Count);
        foreach (var id in rootIds)
        {
            writer.Write(id);
        }

        // Serialize the pending queue of discovered objects
        while (context.TryDequeue(out ISerializableObject? obj) && obj is not null)
        {
            var id = context.GetOrAssignId(obj); // already assigned, just re-get

            // Write ID
            writer.Write(id);

            // Write type name (assembly-qualified makes cross-assembly robust; you can switch to FullName if stable)
            var typeName = obj.GetType().AssemblyQualifiedName ?? obj.GetType().FullName!;
            writer.Write(typeName);

            // Serialize payload into a temporary buffer, to prefix with size
            using var payloadMs = new MemoryStream();
            using (var payloadWriter = new BinaryWriter(payloadMs, Encoding.UTF8, leaveOpen: true))
            {
                obj.Write(payloadWriter, context);
            }

            var payload = payloadMs.ToArray();
            writer.Write(payload.Length);
            writer.Write(payload);
        }
    }

    public static IReadOnlyList<ISerializableObject> Load(Stream stream, out string? saveDisplayName)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        // Header
        var magic = reader.ReadString();
        if (magic != Magic)
        {
            throw new InvalidDataException("Not a valid game state file.");
        }

        var version = reader.ReadInt32();
        if (version != Version)
        {
            throw new InvalidDataException($"Unsupported version {version}.");
        }

        _ = reader.ReadByte(); // padding

        saveDisplayName = reader.ReadNullTerminatedUtf8(Encoding.UTF8);

        var rootIds = new List<int>();

        var rootCount = reader.ReadInt32();
        for (var i = 0; i < rootCount; i++)
        {
            rootIds.Add(reader.ReadInt32());
        }

        var loadContext = new LoadContext();
        var objectsInOrder = new List<(int id, ISerializableObject obj, byte[] payload)>();

        // Read all objects: construct instances first
        while (stream.Position < stream.Length)
        {
            var id = reader.ReadInt32();
            var typeName = reader.ReadString();
            var length = reader.ReadInt32();
            var payload = reader.ReadBytes(length);

            Type type = Type.GetType(typeName, throwOnError: true)!;
            if (Activator.CreateInstance(type) is not ISerializableObject instance)
            {
                throw new InvalidDataException($"Type {typeName} does not implement ISerializableObject.");
            }

            loadContext.Register(id, instance);
            objectsInOrder.Add((id, instance, payload));
        }

        // Now load data for all instances
        foreach ((_, ISerializableObject obj, var payload) in objectsInOrder)
        {
            using var ms = new MemoryStream(payload, writable: false);
            using var r = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            obj.Read(r, loadContext);
            loadContext.DeferPostLoad(obj);
        }

        // Resolve references
        loadContext.RunPostLoad();

        // Resolve all roots
        var roots = new List<ISerializableObject>(rootIds.Count);
        roots.AddRange(
            rootIds.Select(id =>
                loadContext.Resolve<ISerializableObject>(id)
                ?? throw new InvalidDataException($"Root object with id {id} not found.")
            )
        );

        return roots;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "The method is meant to return false on failure."
    )]
    public static bool TryReadDisplayName(string path, out string? displayName)
    {
        displayName = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using FileStream fs = File.OpenRead(path);
            using var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);

            var magic = reader.ReadString();
            if (magic != Magic)
            {
                return false;
            }

            var version = reader.ReadInt32();
            if (version != Version)
            {
                return false;
            }

            _ = reader.ReadByte(); // padding
            displayName = reader.ReadNullTerminatedUtf8(Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
