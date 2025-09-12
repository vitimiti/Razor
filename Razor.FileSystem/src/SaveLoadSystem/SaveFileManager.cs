// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Razor.FileSystem.SaveLoadSystem;

[PublicAPI]
public static partial class SaveFileManager
{
    private static readonly Regex s_saveNameRegex = SaveFileNameRegex();

    public static string Save(
        string directoryPath,
        string displayName,
        IEnumerable<ISerializableObject> roots
    )
    {
        Directory.CreateDirectory(directoryPath);

        var idx = GetNextAvailableIndex(directoryPath);
        var fileName = $"{idx:0000000000000000}.sav";
        var fullPath = Path.Combine(directoryPath, fileName);

        using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        BinaryGameStateSerializer.Save(fs, displayName, roots);

        return fullPath;
    }

    public static IReadOnlyList<ISerializableObject> Load(string path, out string? displayName)
    {
        using var fs = File.OpenRead(path);
        return BinaryGameStateSerializer.Load(fs, out displayName);
    }

    public static string? ReadDisplayName(string path)
    {
        return BinaryGameStateSerializer.TryReadDisplayName(path, out var displayName)
            ? displayName
            : null;
    }

    [GeneratedRegex(@"^\d{16}\.sav$", RegexOptions.Compiled)]
    private static partial Regex SaveFileNameRegex();

    private static int GetNextAvailableIndex(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            return 0;
        }

        var used = new HashSet<int>();
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.sav"))
        {
            var name = Path.GetFileName(file);
            if (!s_saveNameRegex.IsMatch(name))
            {
                continue;
            }

            if (!int.TryParse(Path.GetFileNameWithoutExtension(name), out var n))
            {
                continue;
            }

            if (n >= 0)
            {
                used.Add(n);
            }
        }

        // Find the smallest missing non-negative integer
        var candidate = 0;
        while (used.Contains(candidate))
        {
            candidate++;
        }

        return candidate;
    }
}
