// -----------------------------------------------------------------------
// <copyright file="ISerializableObject.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Razor.FileSystem.SaveFile;

/// <summary>Represents an interface for serializable objects that can be written to and read from binary streams, with support for handling context-specific post-load operations.</summary>
public interface ISerializableObject
{
    /// <summary>Writes the serialized representation of the object to a binary stream, utilizing the provided writer and save context.</summary>
    /// <param name="writer">The binary writer used to write the serialized data.</param>
    /// <param name="context">The save context providing state during the write process.</param>
    void Write(BinaryWriter writer, SaveContext context);

    /// <summary>Reads and deserializes the object from a binary stream, utilizing the provided reader and load context.</summary>
    /// <param name="reader">The binary reader used to read the serialized data.</param>
    /// <param name="context">The load context providing state during the read process.</param>
    void Read(BinaryReader reader, LoadContext context);

    /// <summary>Performs post-load operations for the object, utilizing the provided load context.</summary>
    /// <param name="context">The load context providing state during the post-load process.</param>
    void OnPostLoad(LoadContext context);
}
