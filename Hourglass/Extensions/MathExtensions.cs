// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MathExtensions.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Extensions;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions
/// that extend those provided by the <see cref="Math"/> class.
/// </summary>
public static class MathExtensions
{
    /// <summary>
    /// Limits a value to a specified range.
    /// </summary>
    /// <param name="value">A value.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns><paramref name="value"/> limited to the specified range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double LimitToRange(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    /// <summary>
    /// Limits a value to a specified range.
    /// </summary>
    /// <param name="value">A value.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns><paramref name="value"/> limited to the specified range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LimitToRange(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    /// <summary>
    /// Clamps a value to a specified range.
    /// </summary>
    /// <param name="value">A value.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns><paramref name="value"/> clamped to the specified range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp(double value, double min, double max) => LimitToRange(value, min, max);

    /// <summary>
    /// Clamps a value to a specified range.
    /// </summary>
    /// <param name="value">A value.</param>
    /// <param name="min">The minimum value of the range (inclusive).</param>
    /// <param name="max">The maximum value of the range (inclusive).</param>
    /// <returns><paramref name="value"/> clamped to the specified range.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max) => LimitToRange(value, min, max);

    /// <summary>
    /// Computes seconds (0 to 3600) on a 60-minute dial from a delta (dx, dy) relative to center,
    /// where 12 o'clock is 0 seconds and angles increase counter-clockwise.
    /// </summary>
    /// <param name="dx">X offset from center.</param>
    /// <param name="dy">Y offset from center.</param>
    /// <returns>Rounded seconds between 0 and 3600.</returns>
    public static int AngleToSeconds(double dx, double dy)
    {
        double angleRad = Math.Atan2(-dx, -dy);
        if (angleRad < 0)
        {
            angleRad += 2.0 * Math.PI;
        }

        double deg = angleRad * (180.0 / Math.PI);
        double rawSeconds = (deg / 360.0) * 3600.0;
        return (int)Math.Round(rawSeconds);
    }

    /// <summary>
    /// Computes the new dragged seconds while preventing accidental wrap-around across 12 o'clock.
    /// </summary>
    /// <param name="previousSeconds">The previous dragged duration in seconds.</param>
    /// <param name="candidateSeconds">The candidate duration in seconds.</param>
    /// <returns>The clamped dragged duration in seconds.</returns>
    public static int ComputeDraggedSeconds(int previousSeconds, int candidateSeconds)
    {
        candidateSeconds = LimitToRange(candidateSeconds, 0, 3600);

        // Prevent accidental wrap-around across 12 o'clock (0 <-> 60 minutes)
        // 120 seconds buffer is ~12 degrees on either side of 12 o'clock
        if (previousSeconds <= 120 && candidateSeconds >= 3480)
        {
            return 0;
        }

        if (previousSeconds >= 3480 && candidateSeconds <= 120)
        {
            return 3600;
        }

        return candidateSeconds;
    }

    /// <summary>
    /// Returns the latter of two <see cref="DateTime"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="DateTime"/> to compare.</param>
    /// <param name="b">The second <see cref="DateTime"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is later.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Max(DateTime a, DateTime b)
    {
        return a > b ? a : b;
    }

    /// <summary>
    /// Returns the larger of two <see cref="TimeSpan"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="TimeSpan"/> to compare.</param>
    /// <param name="b">The second <see cref="TimeSpan"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is larger.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan Max(TimeSpan a, TimeSpan b)
    {
        return a > b ? a : b;
    }

    /// <summary>
    /// Returns the earlier of two <see cref="DateTime"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="DateTime"/> to compare.</param>
    /// <param name="b">The second <see cref="DateTime"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is earlier.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime Min(DateTime a, DateTime b)
    {
        return a < b ? a : b;
    }

    /// <summary>
    /// Returns the smaller of two <see cref="TimeSpan"/>s.
    /// </summary>
    /// <param name="a">The first <see cref="TimeSpan"/> to compare.</param>
    /// <param name="b">The second <see cref="TimeSpan"/> to compare.</param>
    /// <returns><paramref name="a"/> or <paramref name="b"/>, whichever is smaller.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan Min(TimeSpan a, TimeSpan b)
    {
        return a < b ? a : b;
    }

    // https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Double.cs,155
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsFinite(this double d)
    {
        long bits = BitConverter.DoubleToInt64Bits(d);
        return (bits & 0x7FFFFFFFFFFFFFFF) < 0x7FF0000000000000;
    }
}