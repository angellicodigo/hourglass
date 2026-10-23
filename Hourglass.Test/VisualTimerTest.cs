// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VisualTimerTest.cs" company="Chris Dziemborowicz">
//   Copyright (c) Chris Dziemborowicz. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Hourglass.Test;

using System;
using System.ComponentModel;

using Extensions;
using Serialization;
using Timing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for visual timer options, timing, and helper functionality.
/// </summary>
[TestClass]
public sealed class VisualTimerTest
{
    [TestMethod]
    public void TimerOptionsVisualTimerDefaultIsFalse()
    {
        TimerOptions options = new();
        Assert.IsFalse(options.VisualTimer);
    }

    [TestMethod]
    public void TimerOptionsVisualTimerPropertyRaisesPropertyChanged()
    {
        TimerOptions options = new();
        string? propertyChangedName = null;
        options.PropertyChanged += (sender, args) => propertyChangedName = args.PropertyName;

        options.VisualTimer = true;
        Assert.IsTrue(options.VisualTimer);
        Assert.AreEqual(nameof(TimerOptions.VisualTimer), propertyChangedName);

        // Setting same value should not fire event
        propertyChangedName = null;
        options.VisualTimer = true;
        Assert.IsNull(propertyChangedName);

        options.VisualTimer = false;
        Assert.IsFalse(options.VisualTimer);
        Assert.AreEqual(nameof(TimerOptions.VisualTimer), propertyChangedName);
    }

    [TestMethod]
    public void TimerOptionsSetCopiesVisualTimer()
    {
        TimerOptions source = new() { VisualTimer = true };
        TimerOptions target = new();

        bool propertyChangedFired = false;
        target.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(TimerOptions.VisualTimer))
            {
                propertyChangedFired = true;
            }
        };

        target.Set(source);

        Assert.IsTrue(target.VisualTimer);
        Assert.IsTrue(propertyChangedFired);
    }

    [TestMethod]
    public void TimerOptionsSerializationRoundTrip()
    {
        TimerOptions options = new() { VisualTimer = true };
        TimerOptionsInfo info = options.ToTimerOptionsInfo();

        Assert.IsTrue(info.VisualTimer);

        TimerOptions restored = new(info);
        Assert.IsTrue(restored.VisualTimer);
    }

    [TestMethod]
    public void TimerStartFromTimeSpanWorks()
    {
        TimeSpan span = TimeSpan.FromMinutes(25).Add(TimeSpan.FromSeconds(30));
        TimerStart timerStart = TimerStart.FromTimeSpan(span);

        Assert.AreEqual(TimerStartType.TimeSpan, timerStart.Type);
        Assert.IsTrue(timerStart.IsCurrent);

        DateTime now = DateTime.Now;
        bool success = timerStart.TryGetEndTime(now, out DateTime endTime);

        Assert.IsTrue(success);
        TimeSpan diff = endTime - now;
        Assert.AreEqual(25, diff.Minutes);
        Assert.AreEqual(30, diff.Seconds);
    }

    [TestMethod]
    public void TimerStartFromTimeSpanBoundaryValues()
    {
        // 0 seconds
        TimerStart zeroStart = TimerStart.FromTimeSpan(TimeSpan.Zero);
        DateTime now = DateTime.Now;
        Assert.IsTrue(zeroStart.TryGetEndTime(now, out DateTime zeroEnd));
        Assert.AreEqual(now, zeroEnd);

        // 60 minutes
        TimerStart hourStart = TimerStart.FromTimeSpan(TimeSpan.FromMinutes(60));
        Assert.IsTrue(hourStart.TryGetEndTime(now, out DateTime hourEnd));
        Assert.AreEqual(60.0, (hourEnd - now).TotalMinutes, 0.001);

        // Fractional second (e.g. 15.5 seconds)
        TimerStart fracStart = TimerStart.FromTimeSpan(TimeSpan.FromSeconds(15.5));
        Assert.IsTrue(fracStart.TryGetEndTime(now, out DateTime fracEnd));
        Assert.AreEqual(15.5, (fracEnd - now).TotalSeconds, 0.001);
    }

    [TestMethod]
    public void MathExtensionsLimitToRangeAndClamp()
    {
        // Int range
        Assert.AreEqual(0, MathExtensions.LimitToRange(-10, 0, 3600));
        Assert.AreEqual(3600, MathExtensions.LimitToRange(4000, 0, 3600));
        Assert.AreEqual(1500, MathExtensions.LimitToRange(1500, 0, 3600));
        Assert.AreEqual(0, MathExtensions.LimitToRange(0, 0, 3600));
        Assert.AreEqual(3600, MathExtensions.LimitToRange(3600, 0, 3600));

        Assert.AreEqual(0, MathExtensions.Clamp(-5, 0, 100));
        Assert.AreEqual(100, MathExtensions.Clamp(105, 0, 100));
        Assert.AreEqual(50, MathExtensions.Clamp(50, 0, 100));

        // Double range
        Assert.AreEqual(0.0, MathExtensions.LimitToRange(-0.5, 0.0, 3600.0), 0.0001);
        Assert.AreEqual(3600.0, MathExtensions.LimitToRange(3600.5, 0.0, 3600.0), 0.0001);
        Assert.AreEqual(12.34, MathExtensions.LimitToRange(12.34, 0.0, 3600.0), 0.0001);

        Assert.AreEqual(6.0, MathExtensions.Clamp(2.0, 6.0, 200.0), 0.0001);
        Assert.AreEqual(200.0, MathExtensions.Clamp(250.0, 6.0, 200.0), 0.0001);
        Assert.AreEqual(100.0, MathExtensions.Clamp(100.0, 6.0, 200.0), 0.0001);
    }

    [TestMethod]
    public void MathExtensionsAngleToSecondsAngleCalculations()
    {
        double r = 80.0;

        // 12 o'clock: 0 seconds (top: dx = 0, dy = -r)
        Assert.AreEqual(0, MathExtensions.AngleToSeconds(0, -r));

        // 9 o'clock: 15 minutes = 900 seconds (left: dx = -r, dy = 0)
        Assert.AreEqual(900, MathExtensions.AngleToSeconds(-r, 0));

        // 6 o'clock: 30 minutes = 1800 seconds (bottom: dx = 0, dy = r)
        Assert.AreEqual(1800, MathExtensions.AngleToSeconds(0, r));

        // 3 o'clock: 45 minutes = 2700 seconds (right: dx = r, dy = 0)
        Assert.AreEqual(2700, MathExtensions.AngleToSeconds(r, 0));

        // 11 o'clock: 5 minutes = 300 seconds
        double angle5Rad = 30.0 * Math.PI / 180.0;
        Assert.AreEqual(300, MathExtensions.AngleToSeconds(-r * Math.Sin(angle5Rad), -r * Math.Cos(angle5Rad)));

        // 1 o'clock: 55 minutes = 3300 seconds
        double angle55Rad = 330.0 * Math.PI / 180.0;
        Assert.AreEqual(3300, MathExtensions.AngleToSeconds(-r * Math.Sin(angle55Rad), -r * Math.Cos(angle55Rad)));
    }

    [TestMethod]
    public void MathExtensionsComputeDraggedSecondsAntiWrap()
    {
        // Normal continuous movement
        Assert.AreEqual(60, MathExtensions.ComputeDraggedSeconds(30, 60));
        Assert.AreEqual(1800, MathExtensions.ComputeDraggedSeconds(1700, 1800));

        // Dragging counter-clockwise across 12 o'clock towards 60 minutes
        // Prev was 59 min (3540s), mouse crosses 12 o'clock into [0, 120] range (e.g. 30s)
        Assert.AreEqual(3600, MathExtensions.ComputeDraggedSeconds(3540, 30));
        Assert.AreEqual(3600, MathExtensions.ComputeDraggedSeconds(3600, 10));

        // Dragging clockwise across 12 o'clock towards 0 minutes
        // Prev was 30s, mouse crosses 12 o'clock into [3480, 3600] range (e.g. 3570s)
        Assert.AreEqual(0, MathExtensions.ComputeDraggedSeconds(30, 3570));
        Assert.AreEqual(0, MathExtensions.ComputeDraggedSeconds(0, 3590));

        // Reversing out of clamp:
        // From 3600 backwards clockwise to 58 minutes (3480s)
        Assert.AreEqual(3500, MathExtensions.ComputeDraggedSeconds(3600, 3500));

        // From 0 forward counter-clockwise to 1 minute (60s)
        Assert.AreEqual(60, MathExtensions.ComputeDraggedSeconds(0, 60));

        // Initial click on the knob at 0 when candidate is slightly right of 12 o'clock
        Assert.AreEqual(0, MathExtensions.ComputeDraggedSeconds(0, 3580));

        // Direct click on numeral 45 (2700s) from 0
        Assert.AreEqual(2700, MathExtensions.ComputeDraggedSeconds(0, 2700));

        // Exact buffer boundary tests (120s buffer)
        Assert.AreEqual(0, MathExtensions.ComputeDraggedSeconds(120, 3480));
        Assert.AreEqual(3600, MathExtensions.ComputeDraggedSeconds(3480, 120));
        Assert.AreEqual(3480, MathExtensions.ComputeDraggedSeconds(121, 3480));
        Assert.AreEqual(3479, MathExtensions.ComputeDraggedSeconds(120, 3479));
        Assert.AreEqual(120, MathExtensions.ComputeDraggedSeconds(3479, 120));
        Assert.AreEqual(121, MathExtensions.ComputeDraggedSeconds(3480, 121));

        // Clamping bounds
        Assert.AreEqual(0, MathExtensions.ComputeDraggedSeconds(100, -50));
        Assert.AreEqual(3600, MathExtensions.ComputeDraggedSeconds(3500, 4000));
        Assert.AreEqual(1800, MathExtensions.ComputeDraggedSeconds(1800, 1800));
    }

    [TestMethod]
    public void CommandLineArgumentsVisualTimerSwitch()
    {
        CommandLineArguments args1 = CommandLineArguments.Parse(["--visual-timer", "on"]);
        Assert.IsFalse(args1.HasParseError);
        Assert.IsTrue(args1.GetTimerOptions().VisualTimer);

        CommandLineArguments args2 = CommandLineArguments.Parse(["-vt", "on"]);
        Assert.IsFalse(args2.HasParseError);
        Assert.IsTrue(args2.GetTimerOptions().VisualTimer);

        CommandLineArguments args3 = CommandLineArguments.Parse(["/vt", "on"]);
        Assert.IsFalse(args3.HasParseError);
        Assert.IsTrue(args3.GetTimerOptions().VisualTimer);

        CommandLineArguments args4 = CommandLineArguments.Parse(["/visual-timer", "on"]);
        Assert.IsFalse(args4.HasParseError);
        Assert.IsTrue(args4.GetTimerOptions().VisualTimer);

        CommandLineArguments args5 = CommandLineArguments.Parse(["--visual-timer", "off"]);
        Assert.IsFalse(args5.HasParseError);
        Assert.IsFalse(args5.GetTimerOptions().VisualTimer);

        CommandLineArguments args6 = CommandLineArguments.Parse(["-vt", "off"]);
        Assert.IsFalse(args6.HasParseError);
        Assert.IsFalse(args6.GetTimerOptions().VisualTimer);

        CommandLineArguments args7 = CommandLineArguments.Parse(["/visual-timer", "off"]);
        Assert.IsFalse(args7.HasParseError);
        Assert.IsFalse(args7.GetTimerOptions().VisualTimer);

        CommandLineArguments args8 = CommandLineArguments.Parse(["--visual-timer", "last"]);
        Assert.IsFalse(args8.HasParseError);
        Assert.IsNotNull(args8.GetTimerOptions());
    }

    [TestMethod]
    public void VisualTimerControlDefaultsAndProperties()
    {
        System.Threading.Thread thread = new(() =>
        {
            Hourglass.Windows.VisualTimerControl control = new();
            Assert.IsFalse(control.IsLocked);
            Assert.IsFalse(control.IsDragging);
            Assert.AreEqual(TimeSpan.Zero, control.TimeLeft);
            Assert.IsNull(control.Theme);

            control.TimeLeft = TimeSpan.FromMinutes(15);
            Assert.AreEqual(TimeSpan.FromMinutes(15), control.TimeLeft);

            control.IsLocked = true;
            Assert.IsTrue(control.IsLocked);
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}

