// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using JetBrains.Annotations;

namespace Razor.FileSystem.SaveLoadSystem;

[PublicAPI]
public static class BinaryGameStateSerializer
{
    private const string Magic = "RZRS";
    private const int Version = 1;

    public static void Save(Stream stream, ISerializableObject root)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Header
        writer.Write(Magic);
        writer.Write(Version);

        var context = new SaveContext();
        var rootId = context.GetOrAssignId(root);
        writer.Write(rootId);

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

    public static T Load<T>(Stream stream)
        where T : class, ISerializableObject
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

        var rootId = reader.ReadInt32();

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

        var root = loadContext.Resolve<T>(rootId);
        return root ?? throw new InvalidDataException("Root object not found.");
    }
}
