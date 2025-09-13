// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Razor.Extensions;

namespace Razor.FileSystem.SaveFile;

internal static class BinaryGameStateSerializer
{
    private const string Magic = "RZRS";
    private const int Version = 1;

    public static void Save(
        Stream stream,
        string saveDisplayName,
        IEnumerable<ISerializableObject> roots
    )
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
        rootIds.AddRange(rootList.Select(r => context.GetOrAssignId(r)));

        // v2: write root count and root IDs
        writer.Write(rootIds.Count);
        foreach (var id in rootIds)
        {
            writer.Write(id);
        }

        // Serialize the pending queue of discovered objects
        while (context.TryDequeue(out var obj) && obj is not null)
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

    public static IReadOnlyList<ISerializableObject> Load(
        Stream stream,
        out string? saveDisplayName
    )
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

            var type = Type.GetType(typeName, throwOnError: true)!;
            if (Activator.CreateInstance(type) is not ISerializableObject instance)
            {
                throw new InvalidDataException(
                    $"Type {typeName} does not implement ISerializableObject."
                );
            }

            loadContext.Register(id, instance);
            objectsInOrder.Add((id, instance, payload));
        }

        // Now load data for all instances
        foreach (var entry in objectsInOrder)
        {
            using var ms = new MemoryStream(entry.payload, writable: false);
            using var r = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            entry.obj.Read(r, loadContext);
            loadContext.DeferPostLoad(entry.obj);
        }

        // Resolve references
        loadContext.RunPostLoad();

        // Resolve all roots
        var roots = new List<ISerializableObject>(rootIds.Count);
        foreach (var id in rootIds)
        {
            var root = loadContext.Resolve<ISerializableObject>(id);
            if (root is null)
            {
                throw new InvalidDataException($"Root object with id {id} not found.");
            }

            roots.Add(root);
        }

        return roots;
    }

    public static bool TryReadDisplayName(string path, out string? displayName)
    {
        displayName = null;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var fs = File.OpenRead(path);
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
