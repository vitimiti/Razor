// -----------------------------------------------------------------------
// <copyright file="Curve3D.cs" company="Razor">
// Copyright (c) Razor. All rights reserved.
// Licensed under the MIT license.
// See LICENSE.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Razor.FileSystem.SaveFile;

namespace Razor.Math;

/// <summary>Represents a 3D curve defined by keyframes with temporal interpolation support and serialization capabilities.</summary>
/// <remarks>This class provides a time-based curve system where 3D points are associated with specific time values, enabling smooth interpolation between keyframes. The curve supports looping behavior and implements <see cref="ISerializableObject"/> for persistence.</remarks>
public abstract class Curve3D() : ISerializableObject
{
    /// <summary>Gets or sets a value indicating whether the curve should loop from the end back to the beginning.</summary>
    /// <value><c>true</c> if the curve loops; otherwise, <c>false</c>.</value>
    /// <remarks>When enabled, curve evaluation beyond the end time will wrap to the beginning, creating a seamless loop.</remarks>
    public bool IsLooping { get; set; }

    /// <summary>Gets the time value of the first keyframe in the curve.</summary>
    /// <value>The time of the first keyframe, or <see cref="TimeSpan.Zero"/> if no keyframes exist.</value>
    /// <remarks>This property represents the earliest time point for which the curve has data.</remarks>
    public TimeSpan StartTime => Keys.Count > 0 ? Keys[0].Time : TimeSpan.Zero;

    /// <summary>Gets the time value of the last keyframe in the curve.</summary>
    /// <value>The time of the last keyframe, or <see cref="TimeSpan.Zero"/> if no keyframes exist.</value>
    /// <remarks>This property represents the latest time point for which the curve has data.</remarks>
    public TimeSpan EndTime => Keys.Count > 0 ? Keys[^1].Time : TimeSpan.Zero;

    /// <summary>Gets the total number of keyframes in the curve.</summary>
    /// <value>The count of keyframes stored in the curve.</value>
    public int KeyCount => Keys.Count;

    /// <summary>Gets or sets the keyframe at the specified index.</summary>
    /// <param name="index">The zero-based index of the keyframe to access.</param>
    /// <value>A tuple containing the 3D point and time value of the keyframe at the specified index.</value>
    /// <returns>A tuple with the <see cref="Vector3"/> point and <see cref="TimeSpan"/> time of the keyframe.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the valid range of keyframes.</exception>
    /// <remarks>This indexer provides direct access to keyframe data.</remarks>
    public (Vector3 Point, TimeSpan Time) this[int index]
    {
        get => (Keys[index].Point, Keys[index].Time);
        set => Keys[index] = new Key(value.Point, value.Time);
    }

    /// <summary>Gets the internal collection of keyframes that define the curve.</summary>
    /// <value>A list containing the keyframe data ordered by time.</value>
    /// <remarks>This property provides access to the underlying keyframe storage for derived classes. Keyframes are maintained in chronological order by their time values.</remarks>
    protected Collection<Key> Keys { get; } = [];

    /// <summary>Evaluates the curve at the specified time to compute the interpolated 3D point.</summary>
    /// <param name="time">The time at which to evaluate the curve.</param>
    /// <returns>The interpolated <see cref="Vector3"/> point at the specified time.</returns>
    /// <remarks>This method must be implemented by derived classes to define the specific interpolation algorithm used for the curve evaluation. The base implementation is abstract and requires override.</remarks>
    public abstract Vector3 Evaluate(TimeSpan time);

    /// <summary>Adds a new keyframe to the curve at the specified point and time.</summary>
    /// <param name="point">The 3D point to associate with the keyframe.</param>
    /// <param name="time">The time value for the new keyframe.</param>
    /// <returns>The index at which the keyframe was inserted in the chronologically ordered list.</returns>
    /// <remarks>Keyframes are automatically inserted in chronological order based on their time values. If a keyframe with the same time already exists, the new keyframe will be inserted adjacent to it.</remarks>
    public int AddKey(Vector3 point, TimeSpan time)
    {
        var idx = 0;
        while (idx < Keys.Count && Keys[idx].Time < time)
        {
            idx++;
        }

        Key newKey = new(point, time);
        Keys.Insert(idx, newKey);
        return idx;
    }

    /// <summary>Removes the keyframe at the specified index from the curve.</summary>
    /// <param name="index">The zero-based index of the keyframe to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is outside the valid range of keyframes.</exception>
    /// <remarks>After removal, all subsequent keyframes will have their indices decremented by one.</remarks>
    public void RemoveKey(int index) => Keys.RemoveAt(index);

    /// <summary>Removes all keyframes from the curve, resetting it to an empty state.</summary>
    /// <remarks>After calling this method, the curve will contain no keyframes and both <see cref="StartTime"/> and <see cref="EndTime"/> will return <see cref="TimeSpan.Zero"/>.</remarks>
    public void ClearKeys() => Keys.Clear();

    /// <summary>Serializes the curve data to a binary stream using the specified writer and save context.</summary>
    /// <param name="writer">The binary writer used to write the serialized data.</param>
    /// <param name="context">The save context providing state information during serialization.</param>
    /// <remarks>The serialization format includes the looping flag, key count, and for each key: X, Y, Z coordinates and time ticks. The data is written in a compact binary format suitable for file storage or network transmission.</remarks>
    public void Write([NotNull] BinaryWriter writer, SaveContext context)
    {
        // The save chunk is:
        // [IsLooping][KeyCount][Key0.X][Key0.Y][Key0.Z][Key0.Time][Key1.X][Key1.Y][Key1.Z][Key1.Time]...
        writer.Write(IsLooping);
        writer.Write(Keys.Count);
        foreach (Key key in Keys)
        {
            writer.Write(key.Point.X);
            writer.Write(key.Point.Y);
            writer.Write(key.Point.Z);
            writer.Write(key.Time.Ticks);
        }
    }

    /// <summary>Deserializes curve data from a binary stream using the specified reader and load context.</summary>
    /// <param name="reader">The binary reader used to read the serialized data.</param>
    /// <param name="context">The load context providing state information during deserialization.</param>
    /// <remarks>This method reconstructs the curve from binary data previously written by <see cref="Write"/>. The existing keyframes are cleared before loading the new data. The method expects the same binary format as written by the Write method.</remarks>
    public void Read([NotNull] BinaryReader reader, LoadContext context)
    {
        // The save chunk is:
        // [IsLooping][KeyCount][Key0.X][Key0.Y][Key0.Z][Key0.Time][Key1.X][Key1.Y][Key1.Z][Key1.Time]...
        IsLooping = reader.ReadBoolean();
        var keyCount = reader.ReadInt32();
        Keys.Clear();
        for (var i = 0; i < keyCount; i++)
        {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            Vector3 point = new(x, y, z);
            var ticks = reader.ReadInt64();
            var time = TimeSpan.FromTicks(ticks);
            Keys.Add(new Key(point, time));
        }
    }

    /// <summary>Performs post-load processing after the curve has been deserialized from a binary stream.</summary>
    /// <param name="context">The load context providing state information during post-load processing.</param>
    /// <remarks>The base implementation of this method performs no additional processing, as keyframes are already properly loaded and ordered during the <see cref="Read"/> operation.</remarks>
    public void OnPostLoad(LoadContext context)
    {
        // No post-load processing needed for Curve3D
        // Keys are already properly loaded and ordered
    }

    /// <summary>Finds the keyframe interval containing the specified time and calculates interpolation parameters.</summary>
    /// <param name="time">The time value to locate within the curve's keyframe intervals.</param>
    /// <returns>A tuple containing the indices of the two surrounding keyframes and the normalized interpolation time between them.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="time"/> is outside the curve's valid time range.</exception>
    /// <remarks>This method is used internally by interpolation algorithms to determine which keyframes to interpolate between. The returned time value is normalized between 0 and 1, representing the position within the interval.</remarks>
    protected (int Index0, int Index1, TimeSpan Time) FindInterval(TimeSpan time)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(time, Keys[0].Time);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(time, Keys[^1].Time);

        var i = 0;
        while (time > Keys[i + 1].Time)
        {
            i++;
        }

        return (i, i + 1, new TimeSpan((long)((time - Keys[i].Time) / (Keys[i + 1].Time - Keys[i].Time))));
    }

    /// <summary>Represents a single keyframe in the curve, containing a 3D point and its associated time value.</summary>
    /// <param name="Point">The 3D position associated with this keyframe.</param>
    /// <param name="Time">The time value at which this keyframe occurs.</param>
    /// <remarks>This record type provides an immutable representation of curve keyframes, ensuring data integrity while supporting efficient storage and comparison operations.</remarks>
    protected record Key(Vector3 Point, TimeSpan Time);
}
