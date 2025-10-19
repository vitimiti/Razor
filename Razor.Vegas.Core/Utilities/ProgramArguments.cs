// -----------------------------------------------------------------------
// <copyright file="ProgramArguments.cs" company="Razor Project Authors">
// Copyright (c) Razor Project Authors. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Razor.Vegas.Core.Utilities;

/// <summary>
/// Helper that expands and stores program arguments. Supports response files
/// (arguments prefixed with a configurable <c>filePrefix</c>) where each line
/// in the referenced file is treated as an argument. Lines that are empty or
/// start with ';' are ignored when expanding a response file.
/// </summary>
public class ProgramArguments
{
    private readonly string[] _args;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramArguments"/> class.
    /// The constructor processes the supplied <paramref name="args"/>, expanding
    /// any response files (arguments that start with <paramref name="filePrefix"/>)
    /// into their constituent lines.
    /// </summary>
    /// <param name="args">The raw argument array passed to the program.</param>
    /// <param name="filePrefix">Optional prefix that marks a response-file argument (default is "@"). If <c>null</c>, response-file expansion is skipped.</param>
    /// <remarks>
    /// If the response file does not exist, the argument is ignored.
    /// </remarks>
    public ProgramArguments([NotNull] string[] args, string? filePrefix = "@")
    {
        var expanded = new List<string>();

        foreach (var arg in args)
        {
            if (filePrefix is not null && arg.StartsWith(filePrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                var fileName = arg[filePrefix.Length..].Trim();
                if (fileName.StartsWith('"') && fileName.EndsWith('"') && fileName.Length >= 2)
                {
                    fileName = fileName[1..^1];
                }

                // Ignore the file if it doesn't exist, process it otherwise
                if (File.Exists(fileName))
                {
                    expanded.AddRange(
                        from rawLine in File.ReadAllLines(fileName)
                        select rawLine.Trim() into line
                        where !string.IsNullOrEmpty(line)
                        where !line.StartsWith(';')
                        select line
                    );
                }
            }
            else
            {
                expanded.Add(arg);
            }
        }

        _args = [.. expanded];
    }

    /// <summary>
    /// Gets the processed argument list as a read-only collection. This includes
    /// any expanded response-file lines.
    /// </summary>
    public IReadOnlyCollection<string> Registered => _args;
}
