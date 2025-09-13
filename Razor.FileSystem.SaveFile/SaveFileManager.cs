// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Razor.FileSystem.SaveFile.Internals;

namespace Razor.FileSystem.SaveFile;

/// <summary>Provides functionality for saving and loading game state files, including managing file names, serializing data, and retrieving save metadata.</summary>
public static partial class SaveFileManager
{
    private static readonly Regex SaveNameRegex = SaveFileNameRegex();

    /// <summary>Saves the game state to a specified directory, creating a file with a unique index, and using the provided display name and serializable root objects.</summary>
    /// <param name="directoryPath">The directory where the save file will be created.</param>
    /// <param name="displayName">A display name associated with the save, for metadata purposes.</param>
    /// <param name="roots">A collection of root objects that implement ISerializableObject to represent the game state.</param>
    /// <returns>Returns the full file path of the saved game state file.</returns>
    public static string Save(string directoryPath, string displayName, IEnumerable<ISerializableObject> roots)
    {
        _ = Directory.CreateDirectory(directoryPath);

        var idx = GetNextAvailableIndex(directoryPath);

        while (true)
        {
            var fileName = $"{idx:D16}.sav";
            var fullPath = Path.Combine(directoryPath, fileName);

            try
            {
                using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                BinaryGameStateSerializer.Save(fs, displayName, roots);

                return fullPath;
            }
            catch (Exception exception) when (exception is IOException && File.Exists(fullPath))
            {
                // If the file already exists, another process/thread won the race for this index.
                // Increment and retry. If it's some other error, let it bubble up.
                idx++;
            }
        }
    }

    /// <summary>Loads a game state from the specified file path and retrieves its associated display name.</summary>
    /// <param name="path">The file path of the saved game state to be loaded.</param>
    /// <param name="displayName">Outputs the display name associated with the loaded game state.</param>
    /// <returns>Returns a read-only list of deserialized root objects implementing ISerializableObject from the loaded game state.</returns>
    public static IReadOnlyList<ISerializableObject> Load(string path, out string? displayName)
    {
        using FileStream fs = File.OpenRead(path);
        return BinaryGameStateSerializer.Load(fs, out displayName);
    }

    /// <summary>Reads the display name metadata from a saved game file, if it exists.</summary>
    /// <param name="path">The file path of the save file from which to read the display name.</param>
    /// <returns>Returns the display name if successfully read, or null if the file does not exist or the display name cannot be determined.</returns>
    public static string? ReadDisplayName(string path) =>
        BinaryGameStateSerializer.TryReadDisplayName(path, out var displayName) ? displayName : null;

    [GeneratedRegex(@"^\d{16}\.sav$", RegexOptions.Compiled)]
    private static partial Regex SaveFileNameRegex();

    private static ulong GetNextAvailableIndex(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return 0UL;
        }

        var used = new HashSet<ulong>();
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.sav"))
        {
            var name = Path.GetFileName(file);
            if (!SaveNameRegex.IsMatch(name))
            {
                continue;
            }

            if (!ulong.TryParse(Path.GetFileNameWithoutExtension(name), out var n))
            {
                continue;
            }

            _ = used.Add(n);
        }

        // Find the smallest missing integer
        var candidate = 0UL;
        while (used.Contains(candidate))
        {
            candidate++;
        }

        return candidate;
    }
}
