using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Spookline.SPC.Console.Arguments;
using Spookline.SPC.Debugging;

namespace Spookline.SPC.Console {
    public abstract class Command {

        public abstract string Name { get; }
        public abstract string Description { get; }
        public List<Argument> AllArguments { get; } = new();
        public List<Command> AllChildren { get; } = new();

        public static Command Action(string name, Func<CommandContext, CommandResult> action, string description = "") {
            return new ActionCommand(name, description, action);
        }

        public static Command Action(
            string name,
            Func<CommandContext, UniTask<CommandResult>> innerAction,
            string description = ""
        ) {
            return new ActionCommand(name, description, innerAction);
        }

        public static Command SingleArgumentAction<T>(
            string name,
            Func<ArgumentPreset, Argument<T>> argBuilder,
            Func<CommandContext, T, CommandResult> action,
            string description = "",
            string argDescription = "",
            bool argRequired = true
        ) {
            var preset = argRequired ? Argument(name, argDescription) : Optional(name, argDescription);
            var arg = argBuilder(preset);
            var command = new ActionCommand(
                name,
                description,
                context => action(context, arg[context])
            );
            command.Arguments(arg);
            return command;
        }

        public static Command SingleArgumentAction<T>(
            string name,
            Func<ArgumentPreset, Argument<T>> argBuilder,
            Func<CommandContext, T, UniTask<CommandResult>> action,
            string description = "",
            string argDescription = "",
            bool argRequired = true
        ) {
            var preset = argRequired ? Argument(name, argDescription) : Optional(name, argDescription);
            var arg = argBuilder(preset);
            var command = new ActionCommand(
                name,
                description,
                context => action(context, arg[context])
            );
            command.Arguments(arg);
            return command;
        }


        protected static ArgumentPreset Argument(string name, string description = "") {
            return new ArgumentPreset(
                name,
                description,
                true
            );
        }

        protected static ArgumentPreset Optional(string name, string description = "") {
            return new ArgumentPreset(
                name,
                description,
                false
            );
        }

        public static ArgumentPreset Named(
            string name,
            string description = "",
            bool isRequired = false
        ) {
            return new ArgumentPreset(
                name,
                description,
                isRequired,
                true
            );
        }

        public static ArgumentPreset<Argument<bool>, bool> Flag(
            string name,
            string description = "",
            bool invert = false,
            bool addNoPrefix = true
        ) {
            FlagArgument argument;
            if (invert && addNoPrefix) argument = new FlagArgument($"no-{name}", description, true);
            else argument = new FlagArgument(name, description, invert);

            return new ArgumentPreset<Argument<bool>, bool>(
                new ArgumentPreset(name, description),
                argument
            );
        }

        protected void Arguments(Argument arg) {
            // Positional arguments must be at the start
            if (!arg.IsFlag && !arg.IsNamed && string.IsNullOrEmpty(arg.Prefix)) {
                var firstNonPos = AllArguments.FindIndex(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix));
                if (firstNonPos != -1) {
                    throw new InvalidOperationException(
                        $"Positional argument '{arg.Name}' cannot be registered after named arguments or flags in command '{Name}'."
                    );
                }
            }

            AllArguments.Add(arg);
        }

        protected void Arguments(params Argument[] args) {
            foreach (var arg in args) Arguments(arg);
        }


        protected void Subcommands(params Command[] cmd) {
            AllChildren.AddRange(cmd);
        }


        public virtual UniTask<CommandResult> ExecuteAsync(CommandContext context) {
            return UniTask.FromResult(Execute(context));
        }

        public virtual CommandResult Execute(CommandContext context) {
            return CommandResult.Failed("Command not implemented");
        }

        public virtual string GetShortHelp(
            CommandInfoRichTextStyle style = null,
            Dictionary<Argument, string> values = null,
            Argument activeArg = null,
            bool atSubcommand = false,
            string parentPath = "",
            HashSet<Argument> malformedArgs = null,
            string errorSuffix = null
        ) {
            style ??= CommandInfoRichTextStyle.Default;
            var fullName = string.IsNullOrEmpty(parentPath) ? Name : $"{parentPath} {Name}";
            var sb = new StringBuilder(fullName);

            if (AllChildren.Count > 0 && atSubcommand) {
                var subText = $"[{string.Join("|", AllChildren.Select(s => s.Name))}]";
                if (activeArg == null) subText = $"<b><color={style.weak}>{subText}</color></b>";

                sb.Append(" ").Append(subText);
            }

            foreach (var arg in AllArguments) {
                string val = null;
                values?.TryGetValue(arg, out val);
                var isMalformed = malformedArgs != null && malformedArgs.Contains(arg);
                sb.Append(" ").Append(arg.GetShortHelp(style, val, arg == activeArg, isMalformed));
            }

            if (!string.IsNullOrEmpty(errorSuffix))
                sb.Append($" <color={style.error}>").Append(errorSuffix).Append("</color>");

            return sb.ToString();
        }

        public virtual string GetLongHelp(CommandInfoRichTextStyle style) {
            var sb = new StringBuilder();
            sb.AppendLine($"{Name}: {Description}");
            if (AllArguments.Count > 0) {
                sb.AppendLine("Arguments:");
                foreach (var arg in AllArguments) sb.AppendLine($"  {arg.GetLongHelp(style)}");
            }

            if (AllChildren.Count > 0) {
                sb.AppendLine("Subcommands:");
                foreach (var sub in AllChildren) sb.AppendLine($"  {sub.Name}: {sub.Description}");
            }

            return sb.ToString().TrimEnd();
        }

    }

    public struct CommandResult {

        public bool success;
        public bool hasMessage;
        public object message;

        public static CommandResult Successful() {
            return new CommandResult { success = true };
        }

        public static CommandResult Successful(object message) {
            return new CommandResult {
                success = true,
                hasMessage = true,
                message = message
            };
        }

        public static CommandResult Successful(string summary, string message) {
            return new CommandResult {
                success = true,
                hasMessage = true,
                message = new ExtendedLogEntry {
                    type = ExtLogType.Log,
                    summary = summary,
                    message = message
                }
            };
        }

        public static CommandResult Failed(object error) {
            return new CommandResult { success = false, hasMessage = true, message = error };
        }

        public static implicit operator CommandResult(bool success) {
            return new CommandResult { success = success };
        }

        public static implicit operator bool(CommandResult result) {
            return result.success;
        }

        public static implicit operator CommandResult(string message) {
            return Successful(message);
        }

    }

    public struct CompletionResult {

        public readonly List<string> completionItems;
        public readonly string richInfoText;

        public CompletionResult(List<string> items, string richInfo = null) {
            completionItems = items ?? new List<string>();
            richInfoText = richInfo;
        }

    }

    public class CommandContext {

        // Values are stored by argument reference or name
        private readonly Dictionary<Argument, object> _values = new();

        public string RawInput { get; internal set; }

        public Command Command { get; internal set; }

        public bool HasValue(Argument arg) {
            return _values.ContainsKey(arg);
        }

        public object GetRawValue(Argument arg) {
            return _values.GetValueOrDefault(arg);
        }

        public void SetValue(Argument arg, object value) {
            _values[arg] = value;
        }

        public T GetValue<T>(Argument<T> arg) {
            return _values.TryGetValue(arg, out var val) ? (T)val : arg.DefaultValue;
        }

    }

    public class CommandInfoRichTextStyle {

        public static readonly CommandInfoRichTextStyle Default = new();
        public string active = "#FFFF00";

        public string error = "#FF0000";
        public string valid = "#00FF00";
        public string weak = "#808080";

    }

    public class ActionCommand : Command {

        public override string Name { get; }
        public override string Description { get; }

        public Func<CommandContext, UniTask<CommandResult>> InnerAction { get; }

        public ActionCommand(string name, string description, Action action) {
            Name = name;
            Description = description;
            InnerAction = _ => {
                action();
                return UniTask.FromResult(CommandResult.Successful());
            };
        }

        public ActionCommand(string name, string description, Func<CommandContext, CommandResult> action) {
            Name = name;
            Description = description;
            InnerAction = context => UniTask.FromResult(action(context));
        }

        public ActionCommand(
            string name,
            string description,
            Func<CommandContext, UniTask<CommandResult>> innerAction
        ) {
            Name = name;
            Description = description;
            InnerAction = innerAction;
        }

        public ActionCommand(string name, Action action) : this(name, "", action) { }

        public ActionCommand(
            string name,
            Func<CommandContext, CommandResult> action
        ) : this(name, "", action) { }

        public ActionCommand(
            string name,
            Func<CommandContext, UniTask<CommandResult>> innerAction
        ) : this(name, "", innerAction) { }

        public override UniTask<CommandResult> ExecuteAsync(CommandContext context) {
            return InnerAction(context);
        }

    }
}