// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Razor.Compression;

/// <summary>Represents the various types of compression algorithms supported by the system.</summary>
/// <remarks>This enumeration defines compression formats that can be used for compressing or decompressing streams or data.</remarks>
/// <example>Use this enum to specify the compression type when working with <see cref="CompressionStream"/>.</example>
public enum CompressionType
{
    /// <summary>Represents no compression.</summary>
    /// <remarks>Data remains uncompressed when this option is used. This may be used to indicate that compression is not required or supported.</remarks>
    None,

    /// <summary>Represents the RefPack compression algorithm.</summary>
    /// <remarks>RefPack is a lightweight compression method often used in scenarios requiring fast decompression and moderate compression ratios.</remarks>
    RefPack,

    /// <summary>Represents Nox LZH compression.</summary>
    /// <remarks>This compression type utilizes the Nox LZH algorithm, which is a variant of the LZH (Lempel-Ziv-Huffman) compression scheme.</remarks>
    NoxLzh,

    /// <summary>Represents the first level of compression using the ZLib algorithm.</summary>
    /// <remarks>This compression type utilizes the ZLib library to provide a balance between compression speed and efficiency. Suitable for general-purpose data compression.</remarks>
    ZLib1,

    /// <summary>Represents the ZLib compression algorithm with enhanced features or level 2.</summary>
    /// <remarks>Provides improved compression efficiency and additional options compared to earlier levels of ZLib. Commonly used for data streams requiring robust compression capabilities.</remarks>
    ZLib2,

    /// <summary>Represents the advanced ZLib compression algorithm, level 3.</summary>
    /// <remarks>This compression type provides improved performance and efficiency over earlier ZLib levels, making it suitable for use cases requiring high-speed data compression and decompression.</remarks>
    ZLib3,

    /// <summary>Represents the ZLib compression algorithm, level 4.</summary>
    /// <remarks>Provides moderate compression efficiency and speed, balancing processing time and data size. Suitable for scenarios requiring a middle-ground approach between performance and compression ratio.</remarks>
    ZLib4,

    /// <summary>Represents the fifth compression level of the ZLib compression algorithm.</summary>
    /// <remarks>Provides improved compression efficiency and speed compared to earlier levels of ZLib. Ideal for scenarios requiring balanced performance and compression ratio.</remarks>
    ZLib5,

    /// <summary>Represents the ZLib compression algorithm with level 6.</summary>
    /// <remarks>Offers a balance between compression speed and ratio, suitable for general-purpose scenarios requiring efficient compression.</remarks>
    ZLib6,

    /// <summary>Represents the ZLib compression algorithm with level 7.</summary>
    /// <remarks>This compression level balances compression speed and size, making it suitable for scenarios where moderate compression with reasonable processing time is required.</remarks>
    ZLib7,

    /// <summary>Represents Zlib compression with level 8.</summary>
    /// <remarks>Provides a high compression level, suitable for reducing data size significantly while maintaining efficient performance during compression and decompression.</remarks>
    ZLib8,

    /// <summary>Represents the highest level of ZLib compression (level 9).</summary>
    /// <remarks>Offers maximum compression ratio at the cost of increased processing time and memory usage.</remarks>
    ZLib9,

    /// <summary>Represents binary tree-based compression.</summary>
    /// <remarks>Utilizes a binary tree structure for encoding data, allowing for efficient storage and retrieval.</remarks>
    BinaryTree,

    /// <summary>Represents a compression type that combines Huffman encoding with run-length encoding.</summary>
    /// <remarks>This compression method utilizes Huffman encoding to reduce the size of data, augmented with run-length encoding to further optimize repetitive sequences within the dataset.</remarks>
    HuffmanWithRunlength,
}
