using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Spookline.SPC.UI.Profiling {
  public abstract class FrameSamplerBufferBase {

    public int bufferSize;
    public float window;


    protected FrameSamplerBufferBase(int bufferSize, float window) {
      this.bufferSize = bufferSize;
      this.window = window;
    }

    protected FrameSamplerBufferBase(int rate, int length) {
      bufferSize = rate * length;
      window = 1f / rate;
    }

    protected FrameSamplerBufferBase(int count) {
      bufferSize = count;
      window = -1f;
    }

    public abstract void Update();

    public float BufferSeconds => bufferSize * window;

    public float BufferHz => 1f / window;

  }

  public class FrameCountSource : FrameSamplerBufferBase, IFpsGraphSampler {

    public readonly List<Vector3> frameCounts = new();
    public int targetFps;

    private float _accumulator = 0f;
    private float _deltaLow = float.MaxValue;
    private float _deltaHigh = 0f;
    private int _count = 0;

    private float _highest = 0f;
    private float _lowest = 0f;
    private float _average = 0f;
    private int _missed = 0;


    public FrameCountSource(int rate, int length) : base(rate, length) {
      targetFps = 60;
    }

    public FrameCountSource(int count) : base(count) {
      targetFps = 60;
    }

    public void Add(float delta) {
      _accumulator += delta;
      if (delta < _deltaLow) _deltaLow = delta;
      if (delta > _deltaHigh) _deltaHigh = delta;
      _count++;
      if (_accumulator >= window) {
        var fps = _count / _accumulator;
        frameCounts.Add(new Vector3(1f / _deltaHigh, fps, 1f / _deltaLow));
        _accumulator -= window;
        _deltaLow = float.MaxValue;
        _deltaHigh = 0f;
        _count = 0;
        RefreshStats();
      }
    }

    public void Clear() {
      frameCounts.Clear();
      _accumulator = 0f;
      _deltaLow = float.MaxValue;
      _deltaHigh = 0f;
      _count = 0;
      RefreshStats();
    }

    public void RefreshStats() {
      _highest = 0f;
      _lowest = float.MaxValue;
      _average = 0f;
      _missed = 0;

      for (var i = 0; i < frameCounts.Count; i++) {
        var v = frameCounts[i];
        var fps = v.y;
        if (v.z > _highest) _highest = v.z;
        if (v.x < _lowest) _lowest = v.x;
        if (fps < targetFps) _missed++;
        _average += fps;
      }

      _lowest = Math.Max(0f, _lowest);
      _average /= frameCounts.Count;

      while (_accumulator > window) {
        _accumulator = Math.Max(0f, _accumulator - window);
        frameCounts.Add(new Vector3(0, 0, 0));
      }

      while (frameCounts.Count > bufferSize) { frameCounts.RemoveAt(0); }
    }

    public int Length => bufferSize;

    public float3 SampleFrameDataAt(int index) {
      index -= bufferSize - frameCounts.Count;
      if (index < 0 || index >= frameCounts.Count) return float3.zero;
      return math.saturate(frameCounts[index] / targetFps);
    }

    public int LineCount => 3;

    public float TargetFps => targetFps;
    public float TargetFrameTime => 1f / targetFps;

    public float CurrentFps {
      get {
        if (frameCounts.Count == 0) return 0f;
        return frameCounts.Last().y;
      }
    }

    public float CurrentFrameTime => 1f / CurrentFps;

    public float HighestFps => _highest;
    public float HighestFrameTime => 1f / HighestFps;
    public float LowestFps => _lowest;
    public float LowestFrameTime => 1f / LowestFps;
    public float AverageFps => _average;
    public float AverageFrameTime => 1f / AverageFps;

    public int MissedFrames => _missed;
    public float MissedPercent => math.saturate(_missed / (float)frameCounts.Count);

    public override void Update() {
      Add(Time.deltaTime);
    }

  }

  public class MemorySource : FrameSamplerBufferBase, IMultiLineGraphSampler {

    public readonly List<Entry> entries = new();
    public float lastUpdated = float.MinValue;

    private const double _toGiB = 1f / (1024 * 1024 * 1024);
    public float peakMemory = 0f;


    public MemorySource() : base(2, 120) {
      bufferSize = 120;
    }

    public override void Update() {
      if (Time.time - lastUpdated < window) return;
      lastUpdated = Time.time;

      entries.Add(
        new Entry {
          totalAllocated = (float)(Profiler.GetTotalAllocatedMemoryLong() * _toGiB),
          totalReserved = (float)(Profiler.GetTotalReservedMemoryLong() * _toGiB),
          monoUsed = (float)(Profiler.GetMonoUsedSizeLong() * _toGiB),
        }
      );
      if (entries.Count > bufferSize) entries.RemoveAt(0);

      peakMemory = 0f;
      for (var i = 0; i < entries.Count; i++) {
        var entry = entries[i];
        if (entry.totalReserved > peakMemory) peakMemory = entry.totalReserved;
      }
    }

    public void Clear() {
      entries.Clear();
      peakMemory = 0f;
    }

    public struct Entry {

      public float totalReserved;
      public float totalAllocated;
      public float monoUsed;

    }

    public int Length => bufferSize;
    public int LineCount => 3;

    public float SampleLineAt(int index, int lineIndex) {
      index -= bufferSize - entries.Count;
      if (index < 0 || index >= entries.Count) return 0f;
      return lineIndex switch {
        0 => entries[index].totalReserved,
        1 => entries[index].totalAllocated,
        2 => entries[index].monoUsed,
        _ => 0f
      } / peakMemory;
    }

    public float LastTotalReserved => entries.Count == 0 ? 0f : entries.Last().totalReserved;

    public float LastTotalUsed => entries.Count == 0 ? 0f : entries.Last().totalAllocated;

    public float LastMonoUsed => entries.Count == 0 ? 0f : entries.Last().monoUsed;

    public float PeakMemory => peakMemory;

  }
}