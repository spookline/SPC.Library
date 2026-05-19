using System;
using Sirenix.OdinInspector;
using Spookline.SPC.Console;
using Spookline.SPC.Debugging;
using Spookline.SPC.Examples;
using Spookline.SPC.Ext;
using UnityEngine;

namespace Spookline.SPC.UI.Profiling {
  [HideMonoScript]
  public class HudProfilingManager : SpookManagerBehaviour<HudProfilingManager> {

    [NonSerialized]
    public FrameCountSource frameCounterShort;
    [NonSerialized]
    public FrameCountSource frameCount;
    [NonSerialized]
    public MemorySource memory;

    [NonSerialized]
    public bool profiling = false;

    protected override void Awake() {
      base.Awake();
      On<GizmoEvt>().Do(OnGizmos);
      On<CollectDebugFlagsEvt>().Do(OnCollectFlags);
      On<CollectCommandsEvt>().Do(OnCollectCommands);
      On<DebugFlagsChangedEvt>().Do(OnFlagsChanged);

      memory = new MemorySource();
      frameCount = new FrameCountSource(20, 10);
      frameCounterShort = new FrameCountSource(60, 1);
    }

    public void SetFpsTarget(int fps) {
      frameCount.targetFps = fps;
      frameCounterShort.targetFps = fps;
    }

    private void OnCollectFlags(ref CollectDebugFlagsEvt args) {
      args.flags.Add("profiling");
    }

    private void OnCollectCommands(ref CollectCommandsEvt args) {
      args.commands.Add(new ProfilerCommand());
    }

    private void Update() {
      if (!profiling) return;
      frameCount.Update();
      frameCounterShort.Update();
      memory.Update();
    }

    private void OnFlagsChanged(ref DebugFlagsChangedEvt args) {
      if (args.flags.Contains("profiling") && !profiling) { Begin(); } else if (profiling) { End(); }
    }

    private void Begin() {
      profiling = true;
    }

    private void End() {
      profiling = false;
      frameCount.Clear();
      frameCounterShort.Clear();
      memory.Clear();
    }

    private void OnGizmos(ref GizmoEvt args) {
      if (args.HasFlag("profiling")) {
        if (!profiling) Begin();
      } else {
        if (profiling) End();
        return;
      }

      if (args.ScreenOverlayPass(out var screen)) {
        screen.Global()
          .Field("Short FPS Graph", frameCounterShort, FpsGraphFieldFactory.Instance)
          .Field("FPS Graph", frameCount, FpsGraphFieldFactory.Instance)
          .Field("Memory Graph", memory, MemoryGraphFieldFactory.Instance);
      }
    }

  }

  public class ProfilerCommand : Command {

    public override string Name => "profiler";
    public override string Description => "Options for the profiler";

    public ProfilerCommand() {
      Subcommands(
        SingleArgumentAction<int>(
          "target-fps",
          preset => preset.Int(60, 1),
          (_, value) => {
            HudProfilingManager.Instance.SetFpsTarget(value);
            return $"Target frame rate set to {value}";
          },
          description: "Set the target frame rate for the profiler"
        )
      );
    }

    public override CommandResult Execute(CommandContext context) {
      return CommandResult.Successful();
    }

  }
}