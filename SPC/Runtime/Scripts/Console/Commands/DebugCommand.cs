using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sirenix.Utilities;
using Spookline.SPC.Common;
using Spookline.SPC.Draw;

namespace Spookline.SPC.Console.Commands {
    public class DebugCommand : Command {

        public override string Name => "debug";
        public override string Description => "Commands related to SPC's debugging features.";


        private readonly Argument<bool> _debugDraw = Flag("gizmos", "Toggles debug gizmos.");
        private readonly Argument<bool> _debugging = Flag("debug", "Toggles debugging mode.");

        public DebugCommand() {
            Arguments(_debugDraw, _debugging);
            Subcommands(new FlagsSubcommand(), new GizmosSubcommand());
            Subcommands(new ActionCommand("gc", "Triggers a manual garbage collection.", _ => {
                var stopwatch = Stopwatch.StartNew();
                GC.Collect();
                stopwatch.Stop();
                return CommandResult.Successful($"Garbage collection took {stopwatch.ElapsedMilliseconds}ms");
            }));
        }


        public override CommandResult Execute(CommandContext context) {
            var instance = Globals.Instance;
            if (!instance) return CommandResult.Failed("Globals instance not valid.");

            if (_debugging[context]) {
                instance.Debugging = !instance.Debugging;
                return CommandResult.Successful(
                    $"Debugging is now {(instance.Debugging ? "enabled" : "disabled")}"
                );
            }

            var builder = new StringBuilder();
            builder.Append($"Debugging: {instance.Debugging}");
            builder.AppendLine($"Flags: {instance.DebugFlags.JoinStringsGroup()}");
            builder.AppendLine($"Gizmos: {instance.DebugGizmos}");
            if (instance.DebugGizmos) {
                builder.AppendLine($"  Interval: {1f / instance.debugRefreshInterval:F2} fps");
                builder.AppendLine($"  Draw: {instance.DebugDraw} (Freq: {instance.drawFrequency})");
                builder.AppendLine($"  Screen Overlay: {instance.DebugScreenOverlay} (Freq: {instance.screenOverlayFrequency})");
                builder.AppendLine($"  World Overlay: {instance.DebugWorldOverlay} (Freq: {instance.worldOverlayFrequency})");
            }
            builder.AppendLine($"Active PolyDrawRenderer: {(bool)PolyDrawRenderer.InstanceOrNull}");
            return CommandResult.Successful(builder.ToString());
        }

        public class GizmosSubcommand : Command {

            public override string Name => "gizmos";
            public override string Description => "Commands for managing debug gizmos.";

            public Argument<GizmoFeature> features =
                Optional("features", "The feature to toggle or modify").Enum<GizmoFeature>();

            public Argument<int> freq =
                Optional("freq", "The refresh frequency of the feature").Int(min: 1);

            public Argument<float> refreshRate =
                Named("refreshRate", "Sets the refresh rate of the debug gizmos").Float(min: 0, max: 999);

            public GizmosSubcommand() {
                Arguments(features, freq, refreshRate);
            }

            public override CommandResult Execute(CommandContext context) {
                var instance = Globals.Instance;
                if (!instance) return CommandResult.Failed("Globals instance not valid.");

                if (context.HasValue(refreshRate)) {
                    instance.debugRefreshInterval = 1 / refreshRate[context];
                    return CommandResult.Successful(
                        $"Debug gizmos refresh rate set to {refreshRate[context]}\n(Interval: {instance.debugRefreshInterval} seconds)"
                    );
                }

                if (context.HasValue(features)) {
                    var feature = features[context];
                    switch (feature) {
                        case GizmoFeature.Draw:
                            if (context.HasValue(freq)) {
                                instance.drawFrequency = freq[context];
                                return CommandResult.Successful(
                                    $"Debug drawing frequency set to {instance.drawFrequency}"
                                );
                            }
                            instance.SetDebugDraw(!instance.DebugDraw);
                            return CommandResult.Successful(
                                $"Debug drawing is now {(instance.DebugDraw ? "enabled" : "disabled")}"
                            );
                        case GizmoFeature.ScreenOverlay:
                            if (context.HasValue(freq)) {
                                instance.screenOverlayFrequency = freq[context];
                                return CommandResult.Successful(
                                    $"Debug screen overlay frequency set to {instance.screenOverlayFrequency}"
                                );
                            }
                            instance.SetDebugScreenOverlay(!instance.DebugScreenOverlay);
                            return CommandResult.Successful(
                                $"Debug screen overlay is now {(instance.DebugScreenOverlay ? "enabled" : "disabled")}"
                            );
                        case GizmoFeature.WorldOverlay:
                            if (context.HasValue(freq)) {
                                instance.worldOverlayFrequency = freq[context];
                                return CommandResult.Successful(
                                    $"Debug world overlay frequency set to {instance.worldOverlayFrequency}"
                                );
                            }

                            instance.SetDebugWorldOverlay(!instance.DebugWorldOverlay);
                            return CommandResult.Successful(
                                $"Debug world overlay is now {(instance.DebugWorldOverlay ? "enabled" : "disabled")}"
                            );

                        default: throw new ArgumentOutOfRangeException();
                    }
                }

                instance.SetDebugGizmos(!instance.DebugGizmos);
                return CommandResult.Successful(
                    $"Debug gizmos are now {(instance.DebugGizmos ? "enabled" : "disabled")}"
                );
            }

            public enum GizmoFeature {

                Draw,
                ScreenOverlay,
                WorldOverlay

            }

        }

        public class FlagsSubcommand : Command {

            public override string Name => "flags";
            public override string Description => "Commands for managing debug flags.";

            private readonly Argument<List<string>> _flags =
                Optional("flags", "Sets debug flags by name.").Enum(
                    () => {
                        if (!Globals.Instance) return Array.Empty<string>();
                        Globals.Instance.RefreshDebugFlags();
                        return Globals.Instance.AvailableDebugFlags;
                    },
                    strict: false
                ).List();

            private readonly Argument<bool> _enable = Flag("enable", "Enables all given flags.");
            private readonly Argument<bool> _disable = Flag("disable", "Disables all given flags.");
            private readonly Argument<bool> _clear = Flag("clear", "Clears all debug flags.");

            public FlagsSubcommand() {
                Arguments(_flags, _enable, _disable);
            }

            public override CommandResult Execute(CommandContext context) {
                var globals = Globals.Instance;
                if (!globals) return CommandResult.Failed("Globals instance not valid.");
                if (_clear[context]) {
                    globals.SetDebugFlags(Array.Empty<string>());
                    return CommandResult.Successful("All debug flags cleared.");
                }

                if (_flags[context].Count > 0) {
                    var actual = new HashSet<string>();
                    foreach (var se in _flags[context]) {
                        if (se.EndsWith("*")) {
                            var prefix = se[..^1];
                            actual.AddRange(globals.AvailableDebugFlags.Where(f => f.StartsWith(prefix)));
                        } else { actual.Add(se); }
                    }

                    foreach (var flag in actual) {
                        if (_enable[context]) {
                            globals.SetDebugFlag(flag); //
                        } else if (_disable[context]) {
                            globals.RemoveDebugFlag(flag); //
                        } else {
                            globals.ToggleDebugFlag(flag); //
                        }
                    }

                    return CommandResult.Successful($"Now active: {globals.DebugFlags.JoinStringsGroup()}");
                }

                globals.RefreshDebugFlags();
                var builder = new StringBuilder();
                builder.Append("Active: ");
                builder.AppendLine(globals.DebugFlags.JoinStringsGroup());
                builder.Append("Available: ");
                builder.AppendLine(globals.AvailableDebugFlags.JoinStringsGroup());
                return CommandResult.Successful(builder.ToString());
            }

        }

    }
}