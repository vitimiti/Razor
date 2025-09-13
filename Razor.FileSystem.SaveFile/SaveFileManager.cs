// Licensed to the Razor contributors under one or more agreements.
// The Razor project licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Razor.FileSystem.SaveFile.Internals;

namespace Razor.FileSystem.SaveFile;

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

        while (true)
        {
            var fileName = $"{idx:D16}.sav";
            var fullPath = Path.Combine(directoryPath, fileName);

            try
            {
                using var fs = new FileStream(
                    fullPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                BinaryGameStateSerializer.Save(fs, displayName, roots);

                return fullPath;
            }
            catch (Exception exception)
            {
                if (exception is not IOException || !File.Exists(fullPath))
                {
                    throw;
                }

                // If the file already exists, another process/thread won the race for this index.
                // Increment and retry. If it's some other error, let it bubble up.
                idx++;
            }
        }
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
            if (!s_saveNameRegex.IsMatch(name))
            {
                continue;
            }

            if (!ulong.TryParse(Path.GetFileNameWithoutExtension(name), out var n))
            {
                continue;
            }

            used.Add(n);
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
