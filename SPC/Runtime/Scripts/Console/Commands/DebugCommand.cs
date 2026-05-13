using System;
using System.Collections.Generic;
using System.Text;
using Spookline.SPC.Common;

namespace Spookline.SPC.Console.Commands {
    public class DebugCommand : Command {

        public override string Name => "debug";
        public override string Description => "Commands related to SPC's debugging features.";


        private readonly Argument<bool> _debugDraw = Flag("draw", "Toggles debug drawing.");
        private readonly Argument<bool> _debugging = Flag("debug", "Toggles debugging mode.");

        public DebugCommand() {
            Arguments(_debugDraw, _debugging);
            Subcommands(new FlagsSubcommand());
        }


        public override CommandResult Execute(CommandContext context) {
            var instance = Globals.Instance;
            if (!instance) return CommandResult.Failed("Globals instance not valid.");

            if (_debugDraw[context]) {
                instance.DebugDraw = !instance.DebugDraw;
                return CommandResult.Successful(
                    $"Debug drawing is now {(instance.DebugDraw ? "enabled" : "disabled")}"
                );
            }

            if (_debugging[context]) {
                instance.Debugging = !instance.Debugging;
                return CommandResult.Successful(
                    $"Debugging is now {(instance.Debugging ? "enabled" : "disabled")}"
                );
            }

            var builder = new StringBuilder();
            builder.Append($"Debugging: {instance.Debugging}");
            builder.Append($", Debug Draw: {instance.DebugDraw}");
            builder.Append($", Flags: {instance.DebugFlags.JoinStringsGroup()}");
            return CommandResult.Successful(builder.ToString());
        }

        public class FlagsSubcommand : Command {

            public override string Name => "flags";
            public override string Description => "Commands for managing debug flags.";

            private readonly Argument<List<string>> _flags =
                Optional("flags", "Sets debug flags by name.").Enum(() => {
                        if (!Globals.Instance) return Array.Empty<string>();
                        return Globals.Instance.AvailableDebugFlags;
                    }
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
                    foreach (var flag in _flags[context]) {
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