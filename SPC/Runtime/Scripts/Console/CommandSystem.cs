using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Spookline.SPC.Console.Commands;
using Spookline.SPC.Events;
using Spookline.SPC.Ext;

namespace Spookline.SPC.Console {
    public class CommandSystem : Singleton<CommandSystem> {

        private readonly List<Command> _commands = new();

        public void Refresh() {
            _commands.Clear();
            _commands.Add(new HelpCommand());
            _commands.Add(new ClearHistoryCommand());
            _commands.Add(new RefreshHistoryCommand());
            _commands.Add(new DebugCommand());
            new CollectCommandsEvt(_commands).RaiseSafe();
        }

        public async UniTask<CommandResult> Execute(string input) {
            try {
                var tokens = Tokenize(input);
                if (tokens.Count == 0) return CommandResult.Failed("Empty command");

                var current = _commands.Find(c => c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));
                if (current == null) return CommandResult.Failed($"Unknown command: {tokens[0]}");

                var tokenIdx = 1;
                while (tokenIdx < tokens.Count) {
                    var sub = current.AllChildren.Find(s => s.Name.Equals(
                            tokens[tokenIdx],
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                    if (sub == null) break;
                    current = sub;
                    tokenIdx++;
                }

                var context = new CommandContext { RawInput = input, Command = current };
                var positionalArgs = current.AllArguments
                    .Where(a => !a.IsFlag && !a.IsNamed && string.IsNullOrEmpty(a.Prefix)).ToList();
                var namedArgs = current.AllArguments
                    .Where(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix))
                    .ToList();

                var posIdx = 0;
                var usedTokens = new HashSet<int>();

                // Parse named args and flags first
                for (var i = tokenIdx; i < tokens.Count; i++) {
                    var token = tokens[i];
                    if (token.StartsWith("-") || token.StartsWith("--")) {
                        var arg = namedArgs.Find(a => (a.Prefix + a.Name).Equals(
                                token,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );
                        if (arg != null) {
                            usedTokens.Add(i);
                            if (arg.IsFlag) {
                                var val = true;
                                if (context.HasValue(arg)) val = (bool)arg.Combine(context.GetRawValue(arg), true);
                                context.SetValue(arg, val);
                            } else if (i + 1 < tokens.Count) {
                                var newValue = arg.Parse(tokens[i + 1]);
                                if (context.HasValue(arg)) newValue = arg.Combine(context.GetRawValue(arg), newValue);

                                context.SetValue(arg, newValue);
                                usedTokens.Add(++i);
                            } else
                                return CommandResult.Failed($"Missing value for argument: {token}");
                        } else
                            return CommandResult.Failed($"Unknown argument or flag: {token}");
                    }
                }

                // Parse positional args
                for (var i = tokenIdx; i < tokens.Count; i++) {
                    if (usedTokens.Contains(i)) continue;
                    if (posIdx < positionalArgs.Count) {
                        var arg = positionalArgs[posIdx++];
                        var newValue = arg.Parse(tokens[i]);
                        if (context.HasValue(arg)) newValue = arg.Combine(context.GetRawValue(arg), newValue);
                        context.SetValue(arg, newValue);
                        usedTokens.Add(i);
                    } else
                        return CommandResult.Failed($"Unexpected argument: {tokens[i]}");
                }

                // Check required
                foreach (var arg in current.AllArguments)
                    if (arg.IsRequired && !context.HasValue(arg))
                        return CommandResult.Failed($"Missing required argument: {arg.Name}");

                return await current.ExecuteAsync(context);
            } catch (Exception e) { return CommandResult.Failed(e.Message); }
        }

        public string GetHelp(string input, CommandInfoRichTextStyle style = null) {
            var tokens = Tokenize(input);
            if (tokens.Count == 0) {
                var sb = new StringBuilder("Available commands:\n");
                foreach (var cmd in _commands) sb.AppendLine($"  {cmd.Name}: {cmd.Description}");
                return sb.ToString().TrimEnd();
            }

            var current = _commands.Find(c => c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));
            if (current == null) {
                var matches = _commands
                    .Where(c => c.Name.Contains(tokens[0], StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (matches.Count == 0) return $"No command matching: '{tokens[0]}'";

                var sb = new StringBuilder("Possible commands:\n");
                foreach (var cmd in matches) sb.AppendLine($"  {cmd.Name}: {cmd.Description}");
                return sb.ToString().TrimEnd();
            }

            var tokenIdx = 1;
            while (tokenIdx < tokens.Count) {
                var sub = current.AllChildren.Find(s => s.Name.Equals(
                        tokens[tokenIdx],
                        StringComparison.OrdinalIgnoreCase
                    )
                );
                if (sub == null) break;
                current = sub;
                tokenIdx++;
            }

            return current.GetLongHelp(style ?? CommandInfoRichTextStyle.Default);
        }

        public CompletionResult Complete(string input, CommandInfoRichTextStyle style = null) {
            style ??= CommandInfoRichTextStyle.Default;

            try {
                var tokens = Tokenize(input);
                var endsWithSpace = input.EndsWith(" ") && input.Length > 0 && input[^1] != '\\';

                if (tokens.Count == 0 || (tokens.Count == 1 && !endsWithSpace)) {
                    var search = tokens.Count == 0 ? "" : tokens[0];
                    var allCommands = _commands
                        .Where(c => c.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Name)
                        .ToList();
                    return new CompletionResult(allCommands);
                }

                var current = _commands.Find(c => c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));
                if (current == null) return new CompletionResult(new List<string>());

                var pathBuilder = new StringBuilder();

                var tokenIdx = 1;
                while (tokenIdx < tokens.Count - (endsWithSpace ? 0 : 1)) {
                    var sub = current.AllChildren.Find(s => s.Name.Equals(
                            tokens[tokenIdx],
                            StringComparison.OrdinalIgnoreCase
                        )
                    );
                    if (sub == null) break;

                    if (pathBuilder.Length > 0) pathBuilder.Append(" ");
                    pathBuilder.Append(current.Name);

                    current = sub;
                    tokenIdx++;
                }

                var parentPath = pathBuilder.ToString();

                // If we are at a subcommand name
                if (!endsWithSpace && tokenIdx < tokens.Count) {
                    var search = tokens[tokenIdx];
                    var subs = current.AllChildren
                        .Where(s => s.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.Name).ToList();
                    if (subs.Count > 0) return new CompletionResult(subs);
                }

                var lastToken = endsWithSpace ? "" : tokens[^1];
                var completions = new List<string>();

                var providedValuesRaw = new Dictionary<Argument, object>();
                var providedValuesString = new Dictionary<Argument, string>();
                var malformedArgs = new HashSet<Argument>();
                string errorSuffix = null;
                Argument activeArg = null;
                string argInfo = null;

                // Argument filtering
                var namedArgs = current.AllArguments
                    .Where(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix))
                    .ToList();

                var positionalArgs = current.AllArguments
                    .Where(a => !a.IsFlag && !a.IsNamed && string.IsNullOrEmpty(a.Prefix))
                    .ToList();

                var usedTokens = new HashSet<int>();

                // Named Arguments and Flags
                for (var i = tokenIdx; i < tokens.Count - (endsWithSpace ? 0 : 1); i++) {
                    var t = tokens[i];
                    if (!t.StartsWith("-")) continue;
                    var arg = namedArgs.Find(a => (a.Prefix + a.Name).Equals(t, StringComparison.OrdinalIgnoreCase));
                    if (arg != null) {
                        usedTokens.Add(i);
                        if (arg.IsFlag)
                            providedValuesString[arg] = "true"; //
                        else if (i + 1 < tokens.Count - (endsWithSpace ? 0 : 1)) {
                            var valStr = tokens[i + 1];
                            providedValuesString[arg] = valStr;
                            usedTokens.Add(++i);
                            try {
                                var newVal = arg.Parse(valStr);
                                if (arg.UseFormat) {
                                    if (providedValuesRaw.TryGetValue(arg, out var old))
                                        newVal = arg.Combine(old, newVal);
                                    providedValuesRaw[arg] = newVal;
                                }
                            } catch {
                                malformedArgs.Add(arg);
                                if (arg.UseFormat) providedValuesRaw[arg] = valStr;
                            }
                        } else {
                            // Missing value for named arg - we are likely here now
                            activeArg = arg;
                            usedTokens.Add(i);
                        }
                    } else {
                        if (float.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) {
                            // Could be a value for a positional argument
                        } else errorSuffix = $"Unknown argument: {t}";
                    }
                }

                // Positional Arguments
                var posIdx = 0;
                for (var i = tokenIdx; i < tokens.Count - (endsWithSpace ? 0 : 1); i++) {
                    if (usedTokens.Contains(i)) continue;
                    if (posIdx < positionalArgs.Count) {
                        var arg = positionalArgs[posIdx++];
                        var valStr = tokens[i];
                        providedValuesString[arg] = valStr;
                        usedTokens.Add(i);
                        try {
                            var newVal = arg.Parse(valStr);
                            if (arg.UseFormat) {
                                if (providedValuesRaw.TryGetValue(arg, out var old)) newVal = arg.Combine(old, newVal);
                                providedValuesRaw[arg] = newVal;
                            }
                        } catch {
                            malformedArgs.Add(arg);
                            if (arg.UseFormat) providedValuesRaw[arg] = valStr;
                        }
                    } else {
                        errorSuffix = $"Unexpected argument: {tokens[i]}";
                        usedTokens.Add(i);
                    }
                }

                // Convert raw values to strings for display
                foreach (var kvp in providedValuesRaw) {
                    try { providedValuesString[kvp.Key] = kvp.Key.Format(kvp.Value); } catch {
                        // ignored
                    }
                }

                // 3. Determine active argument for completion if not already set by missing named value
                if (activeArg == null) {
                    if (tokens.Count >= (endsWithSpace ? 1 : 2)) {
                        var prevToken = endsWithSpace ? tokens[^1] : tokens[^2];
                        var namedArg = namedArgs.Find(a => (a.Prefix + a.Name).Equals(
                                prevToken,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );
                        if (namedArg != null && !namedArg.IsFlag) activeArg = namedArg;
                    }
                }

                var prioritizedCompletions = new List<string>();
                var otherCompletions = new List<string>();

                if (activeArg != null) {
                    argInfo = activeArg.Description;
                    var vals = activeArg.GetCompletions(lastToken);
                    if (activeArg.IsList && lastToken.Contains(",")) {
                        var basePart = lastToken.Substring(0, lastToken.LastIndexOf(',') + 1);
                        vals = vals.Select(v => basePart + v).ToList();
                    }

                    prioritizedCompletions.AddRange(vals);
                } else {
                    // Suggest subcommands and named arguments
                    var canSuggestSubcommands = usedTokens.Count == 0 && posIdx == 0;
                    if (canSuggestSubcommands) {
                        prioritizedCompletions.AddRange(
                            current.AllChildren
                                .Where(s => s.Name.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase))
                                .Select(s => s.Name)
                        );
                    }

                    // Active positional argument?
                    if (posIdx < positionalArgs.Count) {
                        activeArg = positionalArgs[posIdx];
                        argInfo = activeArg.Description;
                        var posCompletions = activeArg.GetCompletions(lastToken);
                        if (activeArg.IsList && lastToken.Contains(",")) {
                            var basePart = lastToken.Substring(0, lastToken.LastIndexOf(',') + 1);
                            posCompletions = posCompletions.Select(v => basePart + v).ToList();
                        }

                        prioritizedCompletions.AddRange(posCompletions);
                    }

                    foreach (var arg in namedArgs) {
                        var fullName = arg.Prefix + arg.Name;
                        if (fullName.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase))
                            otherCompletions.Add(fullName);
                    }
                }

                completions.AddRange(prioritizedCompletions.Distinct());
                completions.AddRange(otherCompletions.Distinct().Except(completions));

                var atSubcommand = usedTokens.Count == 0 && posIdx == 0;
                var richInfo = current.GetShortHelp(
                    style,
                    providedValuesString,
                    activeArg,
                    atSubcommand,
                    parentPath,
                    malformedArgs,
                    errorSuffix
                );
                if (!string.IsNullOrEmpty(argInfo)) richInfo = argInfo + "\n" + richInfo;

                return new CompletionResult(completions.ToList(), richInfo);
            } catch { return new CompletionResult(new List<string>()); }
        }

        public static List<string> Tokenize(string input) {
            var result = new List<string>();
            var current = new StringBuilder();
            var escaped = false;
            var inQuotes = false;
            foreach (var c in input) {
                if (escaped) {
                    current.Append(c);
                    escaped = false;
                } else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inQuotes = !inQuotes;
                else if
                    (!inQuotes && char.IsWhiteSpace(c)) {
                    if (current.Length > 0) {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                } else
                    current.Append(c);
            }

            if (current.Length > 0) result.Add(current.ToString());
            return result;
        }

        public static List<string> SplitList(string input) {
            var result = new List<string>();
            var current = new StringBuilder();
            var escaped = false;
            var inQuotes = false;
            foreach (var c in input) {
                if (escaped) {
                    current.Append(c);
                    escaped = false;
                } else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inQuotes = !inQuotes;
                else if
                    (!inQuotes && c == ',') {
                    result.Add(current.ToString());
                    current.Clear();
                } else
                    current.Append(c);
            }

            result.Add(current.ToString());
            return result;
        }

    }

    public readonly struct CollectCommandsEvt : Evt<CollectCommandsEvt> {

        public readonly List<Command> commands;

        public CollectCommandsEvt(List<Command> commands) {
            this.commands = commands;
        }

        public void Register(params Command[] cmd) {
            commands.AddRange(cmd);
        }
    }
}