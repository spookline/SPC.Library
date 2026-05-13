using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Spookline.SPC {
  public struct CommandResult {

    public bool success;
    public string error;

    public static CommandResult Successful() => new CommandResult { success = true };
    public static CommandResult Failed(string error) => new CommandResult { success = false, error = error };

  }

  public struct CompletionResult {

    public readonly List<string> completionItems;
    public readonly string infoText;
    public readonly string richInfoText;

    public CompletionResult(List<string> items, string info = null, string richInfo = null) {
      completionItems = items ?? new List<string>();
      infoText = info;
      richInfoText = richInfo;
    }

  }

  public class CommandContext {

    public string RawInput { get; internal set; }

    public Command Command { get; internal set; }
    // Values are stored by argument reference or name
    private Dictionary<Argument, object> _values = new();

    public bool HasValue(Argument arg) => _values.ContainsKey(arg);
    public void SetValue(Argument arg, object value) => _values[arg] = value;
    public T GetValue<T>(Argument<T> arg) => _values.TryGetValue(arg, out var val) ? (T)val : arg.DefaultValue;

    public List<T> GetListValue<T>(ListArgument<T> arg) =>
      _values.TryGetValue(arg, out var val) ? (List<T>)val : new List<T>();

  }

  public abstract class Argument {

    public string Name { get; protected set; }
    public string Description { get; protected set; }
    public bool IsRequired { get; protected set; }
    public abstract string Prefix { get; }
    public abstract bool IsFlag { get; }
    public abstract bool IsNamed { get; }
    public abstract bool IsList { get; }

    public abstract object Parse(string input);
    public abstract List<string> GetCompletions(string input);

    public virtual string GetShortHelp(
      string currentValue = null,
      bool isActive = false,
      bool isMalformed = false
    ) {
      var prefix = Prefix;
      var name = string.IsNullOrEmpty(prefix) ? Name : prefix + Name;
      string display;

      if (IsFlag) display = $"[{name}]";
      else if (IsRequired) display = $"<{name}>";
      else display = $"[{name}]";

      if (!string.IsNullOrEmpty(currentValue)) {
        var valColor = isMalformed ? "#FF0000" : "#00FF00";
        if (IsFlag) { display = $"<color={valColor}>{name}</color>"; } else {
          display = $"<color=#808080>[{Name}]</color><color={valColor}>{currentValue}</color>";
        }
      }

      if (isActive) { return $"<b><color=#FFFF00>{display}</color></b>"; }

      return display;
    }

    public virtual string GetLongHelp() => $"{GetShortHelp()}: {Description}";

  }

  public abstract class Argument<T> : Argument {

    public T DefaultValue { get; protected set; }

    protected Argument(string name, string description, T defaultValue = default, bool isRequired = false) {
      Name = name;
      Description = description;
      DefaultValue = defaultValue;
      IsRequired = isRequired;
    }

  }

  public class StringArgument : Argument<string> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public StringArgument(string name, string description, string defaultValue = "", bool isRequired = false)
      : base(name, description, defaultValue, isRequired) { }

    public override object Parse(string input) => input;
    public override List<string> GetCompletions(string input) => new List<string>();

  }

  public class IntArgument : Argument<int> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public IntArgument(string name, string description, int defaultValue = 0, bool isRequired = false)
      : base(name, description, defaultValue, isRequired) { }

    public override object Parse(string input) {
      if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) return result;
      throw new ArgumentException($"Invalid integer value '{input}' for argument {Name}");
    }

    public override List<string> GetCompletions(string input) => new List<string>();

  }

  public class FloatArgument : Argument<float> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public float? Min { get; }
    public float? Max { get; }

    public FloatArgument(
      string name,
      string description,
      float defaultValue = 0f,
      bool isRequired = false,
      float? min = null,
      float? max = null
    )
      : base(name, description, defaultValue, isRequired) {
      Min = min;
      Max = max;
    }

    public override object Parse(string input) {
      var multiplier = 1f;
      if (input.EndsWith("%")) {
        input = input.Substring(0, input.Length - 1);
        multiplier = 0.01f;
      }

      if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) {
        result *= multiplier;
        if (Min.HasValue && result < Min.Value)
          throw new ArgumentException($"Value {result} is below minimum {Min.Value} for argument {Name}");
        if (Max.HasValue && result > Max.Value)
          throw new ArgumentException($"Value {result} is above maximum {Max.Value} for argument {Name}");
        return result;
      }

      throw new ArgumentException($"Invalid float value '{input}' for argument {Name}");
    }

    public override List<string> GetCompletions(string input) => new List<string>();

    public override string GetShortHelp(
      string currentValue = null,
      bool isActive = false,
      bool isMalformed = false
    ) {
      var help = base.GetShortHelp(currentValue, isActive, isMalformed);
      if (!string.IsNullOrEmpty(currentValue)) return help;

      if (Min.HasValue || Max.HasValue) {
        string range = "(";
        if (Min.HasValue) range += Min.Value.ToString(CultureInfo.InvariantCulture);
        range += "..";
        if (Max.HasValue) range += Max.Value.ToString(CultureInfo.InvariantCulture);
        range += ")";
        return help + "<color=#808080>" + range + "</color>";
      }

      return help;
    }

  }

  public class Vector2Argument : Argument<Vector2> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public Vector2Argument(string name, string description, Vector2 defaultValue = default, bool isRequired = false)
      : base(name, description, defaultValue, isRequired) { }

    public override object Parse(string input) {
      var parts = input.Split(',');
      if (parts.Length == 2 &&
          float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
          float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y)) {
        return new Vector2(x, y);
      }

      throw new ArgumentException($"Invalid Vector2 value '{input}' for argument {Name}. Expected format: 'x,y'");
    }

    public override List<string> GetCompletions(string input) => new List<string>();


    public override string GetShortHelp(
      string currentValue = null,
      bool isActive = false,
      bool isMalformed = false
    ) {
      var help = base.GetShortHelp(currentValue, isActive, isMalformed);
      if (!string.IsNullOrEmpty(currentValue)) return help;
      return help + "<color=#808080>Vec2</color>";
    }

  }

  public class Vector3Argument : Argument<Vector3> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public Vector3Argument(string name, string description, Vector3 defaultValue = default, bool isRequired = false)
      : base(name, description, defaultValue, isRequired) { }

    public override object Parse(string input) {
      var parts = input.Split(',');
      if (parts.Length == 3 &&
          float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
          float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
          float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z)) {
        return new Vector3(x, y, z);
      }

      throw new ArgumentException(
        $"Invalid Vector3 value '{input}' for argument {Name}. Expected format: 'x,y,z'"
      );
    }

    public override List<string> GetCompletions(string input) => new List<string>();

    public override string GetShortHelp(
      string currentValue = null,
      bool isActive = false,
      bool isMalformed = false
    ) {
      var help = base.GetShortHelp(currentValue, isActive, isMalformed);
      if (!string.IsNullOrEmpty(currentValue)) return help;
      return help + "<color=#808080>Vec3</color>";
    }

  }

  public class EnumArgument<T> : Argument<T> where T : struct, Enum {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public EnumArgument(string name, string description, T defaultValue = default, bool isRequired = false)
      : base(name, description, defaultValue, isRequired) { }

    public override object Parse(string input) {
      if (Enum.TryParse<T>(input, true, out var result)) return result;
      throw new ArgumentException($"Invalid value '{input}' for enum {typeof(T).Name}");
    }

    public override List<string> GetCompletions(string input) {
      return Enum.GetNames(typeof(T))
        .Where(n => n.StartsWith(input, StringComparison.OrdinalIgnoreCase))
        .ToList();
    }

  }

  public class FlagArgument : Argument<bool> {

    public override string Prefix => "-";
    public override bool IsFlag => true;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public FlagArgument(string name, string description) : base(name, description, false, false) { }

    public override object Parse(string input) => true;
    public override List<string> GetCompletions(string input) => new List<string>();

  }

  public class NamedArgument<T> : Argument<T> {

    public override string Prefix => "--";
    public override bool IsFlag => false;
    public override bool IsNamed => true;
    public override bool IsList => false;

    private readonly Argument<T> _inner;

    public NamedArgument(Argument<T> inner) : base(
      inner.Name,
      inner.Description,
      inner.DefaultValue,
      inner.IsRequired
    ) {
      _inner = inner;
    }

    public override object Parse(string input) => _inner.Parse(input);
    public override List<string> GetCompletions(string input) => _inner.GetCompletions(input);

  }

  public class ListArgument<T> : Argument<List<T>> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => true;

    private readonly Argument<T> _elementArg;

    public ListArgument(Argument<T> elementArg, string description, bool isRequired = false)
      : base(elementArg.Name, description, new List<T>(), isRequired) {
      _elementArg = elementArg;
    }

    public override object Parse(string input) {
      var parts = CommandSystem.SplitList(input);
      var result = new List<T>();
      foreach (var part in parts) { result.Add((T)_elementArg.Parse(part)); }

      return result;
    }

    public override List<string> GetCompletions(string input) {
      var parts = CommandSystem.SplitList(input);
      var lastPart = parts.Count > 0 ? parts[^1] : "";
      return _elementArg.GetCompletions(lastPart);
    }

  }

  public abstract class Command {

    public abstract string Name { get; }
    public abstract string Description { get; }
    public List<Argument> Arguments { get; } = new();
    public List<Command> Subcommands { get; } = new();

    protected void Register(Argument arg) {
      // Positional arguments must be at the start
      if (!arg.IsFlag && !arg.IsNamed && string.IsNullOrEmpty(arg.Prefix)) {
        int firstNonPos = Arguments.FindIndex(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix));
        if (firstNonPos != -1) {
          throw new InvalidOperationException(
            $"Positional argument '{arg.Name}' cannot be registered after named arguments or flags in command '{Name}'."
          );
        }
      }

      Arguments.Add(arg);
    }

    protected void Register(params Argument[] args) {
      foreach (var arg in args) Register(arg);
    }


    protected void RegisterSubcommand(Command cmd) => Subcommands.Add(cmd);

    public abstract CommandResult Execute(CommandContext context);

    public virtual string GetShortHelp(
      Dictionary<Argument, string> values = null,
      Argument activeArg = null,
      bool atSubcommand = false,
      string parentPath = "",
      HashSet<Argument> malformedArgs = null,
      string errorSuffix = null
    ) {
      var fullName = string.IsNullOrEmpty(parentPath) ? Name : $"{parentPath} {Name}";
      var sb = new StringBuilder(fullName);

      if (Subcommands.Count > 0 && atSubcommand) {
        string subText = $"[{string.Join("|", Subcommands.Select(s => s.Name))}]";
        if (activeArg == null) { subText = $"<b><color=#FFFF00>{subText}</color></b>"; }

        sb.Append(" ").Append(subText);
      }

      foreach (var arg in Arguments) {
        string val = null;
        values?.TryGetValue(arg, out val);
        bool isMalformed = malformedArgs != null && malformedArgs.Contains(arg);
        sb.Append(" ").Append(arg.GetShortHelp(val, arg == activeArg, isMalformed));
      }

      if (!string.IsNullOrEmpty(errorSuffix)) { sb.Append(" <color=#FF0000>!").Append(errorSuffix).Append("</color>"); }

      return sb.ToString();
    }

    public virtual string GetLongHelp() {
      var sb = new StringBuilder();
      sb.AppendLine($"{Name}: {Description}");
      if (Arguments.Count > 0) {
        sb.AppendLine("Arguments:");
        foreach (var arg in Arguments) sb.AppendLine($"  {arg.GetLongHelp()}");
      }

      if (Subcommands.Count > 0) {
        sb.AppendLine("Subcommands:");
        foreach (var sub in Subcommands) sb.AppendLine($"  {sub.Name}: {sub.Description}");
      }

      return sb.ToString().TrimEnd();
    }

  }

  public class CommandSystem {

    private readonly List<Command> _commands = new();

    public void Register(Command cmd) => _commands.Add(cmd);

    public CommandResult Execute(string input) {
      try {
        var tokens = Tokenize(input);
        if (tokens.Count == 0) return CommandResult.Failed("Empty command");

        Command current = _commands.Find(c => c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));
        if (current == null) return CommandResult.Failed($"Unknown command: {tokens[0]}");

        int tokenIdx = 1;
        while (tokenIdx < tokens.Count) {
          var sub = current.Subcommands.Find(s => s.Name.Equals(
              tokens[tokenIdx],
              StringComparison.OrdinalIgnoreCase
            )
          );
          if (sub == null) break;
          current = sub;
          tokenIdx++;
        }

        var context = new CommandContext { RawInput = input, Command = current };
        var positionalArgs = current.Arguments
          .Where(a => !a.IsFlag && !a.IsNamed && string.IsNullOrEmpty(a.Prefix)).ToList();
        var namedArgs = current.Arguments.Where(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix))
          .ToList();

        int posIdx = 0;
        var usedTokens = new HashSet<int>();

        // Parse named args and flags first
        for (int i = tokenIdx; i < tokens.Count; i++) {
          var token = tokens[i];
          if (token.StartsWith("-") || token.StartsWith("--")) {
            var arg = namedArgs.Find(a => (a.Prefix + a.Name).Equals(
                token,
                StringComparison.OrdinalIgnoreCase
              )
            );
            if (arg != null) {
              usedTokens.Add(i);
              if (arg.IsFlag) { context.SetValue(arg, true); } else if (i + 1 < tokens.Count) {
                context.SetValue(arg, arg.Parse(tokens[i + 1]));
                usedTokens.Add(++i);
              } else { return CommandResult.Failed($"Missing value for argument: {token}"); }
            } else { return CommandResult.Failed($"Unknown argument or flag: {token}"); }
          }
        }

        // Parse positional args
        for (int i = tokenIdx; i < tokens.Count; i++) {
          if (usedTokens.Contains(i)) continue;
          if (posIdx < positionalArgs.Count) {
            var arg = positionalArgs[posIdx++];
            context.SetValue(arg, arg.Parse(tokens[i]));
            usedTokens.Add(i);
          } else { return CommandResult.Failed($"Unexpected argument: {tokens[i]}"); }
        }

        // Check required
        foreach (var arg in current.Arguments) {
          if (arg.IsRequired && !context.HasValue(arg)) {
            return CommandResult.Failed($"Missing required argument: {arg.Name}");
          }
        }

        return current.Execute(context);
      } catch (Exception e) { return CommandResult.Failed(e.Message); }
    }

    public string GetHelp(string input) {
      var tokens = Tokenize(input);
      if (tokens.Count == 0) {
        var sb = new StringBuilder("Available commands:\n");
        foreach (var cmd in _commands) sb.AppendLine($"  {cmd.Name}: {cmd.Description}");
        return sb.ToString().TrimEnd();
      }

      Command current = _commands.Find(c => c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));
      if (current == null) return $"Unknown command: {tokens[0]}";

      int tokenIdx = 1;
      while (tokenIdx < tokens.Count) {
        var sub = current.Subcommands.Find(s => s.Name.Equals(
            tokens[tokenIdx],
            StringComparison.OrdinalIgnoreCase
          )
        );
        if (sub == null) break;
        current = sub;
        tokenIdx++;
      }

      return current.GetLongHelp();
    }

    private class DummyArg : Argument<object> {

      public override string Prefix => "";
      public override bool IsFlag => false;
      public override bool IsNamed => false;
      public override bool IsList => false;
      public DummyArg(Argument a) : base(a.Name, a.Description) { }
      public override object Parse(string input) => input;
      public override List<string> GetCompletions(string input) => new List<string>();

    }

    public CompletionResult Complete(string input) {
      try {
        var tokens = Tokenize(input);
        bool endsWithSpace = input.EndsWith(" ") && input.Length > 0 && input[^1] != '\\';

        if (tokens.Count == 0 || (tokens.Count == 1 && !endsWithSpace)) {
          var search = tokens.Count == 0 ? "" : tokens[0];
          var cmds = _commands.Where(c => c.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Name).ToList();
          return new CompletionResult(cmds);
        }

        Command current = _commands.Find(c => c.Name.Equals(tokens[0], StringComparison.OrdinalIgnoreCase));
        if (current == null) return new CompletionResult(new List<string>());

        var pathBuilder = new StringBuilder();

        int tokenIdx = 1;
        while (tokenIdx < tokens.Count - (endsWithSpace ? 0 : 1)) {
          var sub = current.Subcommands.Find(s => s.Name.Equals(
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

        string parentPath = pathBuilder.ToString();

        // If we are at a subcommand name
        if (!endsWithSpace && tokenIdx < tokens.Count) {
          var search = tokens[tokenIdx];
          var subs = current.Subcommands
            .Where(s => s.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Name).ToList();
          if (subs.Count > 0) return new CompletionResult(subs);
        }

        // Arguments completion
        var lastToken = endsWithSpace ? "" : tokens[^1];
        var completions = new List<string>();

        // Track current values and active argument for rich info text
        var providedValues = new Dictionary<Argument, string>();
        var malformedArgs = new HashSet<Argument>();
        string errorSuffix = null;
        Argument activeArg = null;
        string argInfo = null;

        // Simple parsing to find provided values
        var namedArgs = current.Arguments.Where(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix))
          .ToList();
        var positionalArgs = current.Arguments
          .Where(a => !a.IsFlag && !a.IsNamed && string.IsNullOrEmpty(a.Prefix)).ToList();
        var usedTokens = new HashSet<int>();

        // 1. Identify named arguments/flags and their values
        for (int i = tokenIdx; i < tokens.Count - (endsWithSpace ? 0 : 1); i++) {
          var t = tokens[i];
          if (t.StartsWith("-") || t.StartsWith("--")) {
            var arg = namedArgs.Find(a => (a.Prefix + a.Name).Equals(t, StringComparison.OrdinalIgnoreCase)
            );
            if (arg != null) {
              usedTokens.Add(i);
              if (arg.IsFlag) { providedValues[arg] = "true"; } else if (i + 1 < tokens.Count -
                                                                         (endsWithSpace ? 0 : 1)) {
                var val = tokens[i + 1];
                providedValues[arg] = val;
                usedTokens.Add(++i);
                try { arg.Parse(val); } catch { malformedArgs.Add(arg); }
              } else {
                // Missing value for named arg - we are likely here now
                activeArg = arg;
                usedTokens.Add(i);
              }
            } else { errorSuffix = $"Unknown argument: {t}"; }
          }
        }

        // 2. Identify positional arguments
        int posIdx = 0;
        for (int i = tokenIdx; i < tokens.Count - (endsWithSpace ? 0 : 1); i++) {
          if (usedTokens.Contains(i)) continue;
          if (posIdx < positionalArgs.Count) {
            var arg = positionalArgs[posIdx++];
            var val = tokens[i];
            providedValues[arg] = val;
            usedTokens.Add(i);
            try { arg.Parse(val); } catch { malformedArgs.Add(arg); }
          } else {
            errorSuffix = $"Unexpected argument: {tokens[i]}";
            usedTokens.Add(i);
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
            if (namedArg != null && !namedArg.IsFlag) { activeArg = namedArg; }
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
          bool canSuggestSubcommands = usedTokens.Count == 0 && posIdx == 0;
          if (canSuggestSubcommands) {
            prioritizedCompletions.AddRange(
              current.Subcommands
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
            if (fullName.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase)) { otherCompletions.Add(fullName); }
          }
        }

        completions.AddRange(prioritizedCompletions.Distinct());
        completions.AddRange(otherCompletions.Distinct().Except(completions));

        bool atSubcommand = usedTokens.Count == 0 && posIdx == 0;
        string richInfo = current.GetShortHelp(
          providedValues,
          activeArg,
          atSubcommand,
          parentPath,
          malformedArgs,
          errorSuffix
        );
        if (!string.IsNullOrEmpty(argInfo)) { richInfo = argInfo + "\n" + richInfo; }

        return new CompletionResult(completions.ToList(), richInfo, richInfo);
      } catch { return new CompletionResult(new List<string>()); }
    }

    public static List<string> Tokenize(string input) {
      var result = new List<string>();
      var current = new StringBuilder();
      bool escaped = false;
      bool inQuotes = false;
      foreach (var c in input) {
        if (escaped) {
          current.Append(c);
          escaped = false;
        } else if (c == '\\') { escaped = true; } else if (c == '"') { inQuotes = !inQuotes; } else if
          (!inQuotes && char.IsWhiteSpace(c)) {
          if (current.Length > 0) {
            result.Add(current.ToString());
            current.Clear();
          }
        } else { current.Append(c); }
      }

      if (current.Length > 0) result.Add(current.ToString());
      return result;
    }

    public static List<string> SplitList(string input) {
      var result = new List<string>();
      var current = new StringBuilder();
      bool escaped = false;
      bool inQuotes = false;
      foreach (var c in input) {
        if (escaped) {
          current.Append(c);
          escaped = false;
        } else if (c == '\\') { escaped = true; } else if (c == '"') { inQuotes = !inQuotes; } else if
          (!inQuotes && c == ',') {
          result.Add(current.ToString());
          current.Clear();
        } else { current.Append(c); }
      }

      result.Add(current.ToString());
      return result;
    }

  }

  // --- EXAMPLE IMPLEMENTATION ---

  public enum EntityType {

    Orc,
    Goblin,
    Dragon

  }

  public class SpawnCommand : Command {

    public override string Name => "spawn";
    public override string Description => "Spawns an entity at a location.";

    private readonly EnumArgument<EntityType> _type;
    private readonly StringArgument _name;
    private readonly IntArgument _amount;
    private readonly FloatArgument _scale;
    private readonly Vector3Argument _position;
    private readonly FlagArgument _silent;
    private readonly NamedArgument<string> _tags;
    private readonly ListArgument<string> _modifiers;

    public SpawnCommand() {
      _type = new EnumArgument<EntityType>("type", "The type of entity to spawn", EntityType.Orc, true);
      _name = new StringArgument("name", "The name of the entity", "Unnamed");
      _amount = new IntArgument("amount", "Amount to spawn", 1);
      _scale = new FloatArgument("scale", "Scale of the entity", 1f, min: 0.1f, max: 10f);
      _position = new Vector3Argument("pos", "Position to spawn at", Vector3.zero);
      _silent = new FlagArgument("silent", "If true, no message will be shown");
      _tags = new NamedArgument<string>(new StringArgument("tags", "Comma separated tags"));
      _modifiers = new ListArgument<string>(
        new StringArgument("mod", "List of modifiers"),
        "Modifiers for the entity"
      );

      Register(_type);
      Register(_name);
      Register(_amount);
      Register(_scale);
      Register(_position);
      Register(_modifiers);
      Register(_silent);
      Register(_tags);

      RegisterSubcommand(new InfoSubcommand());
      RegisterSubcommand(new ConfigSubcommand());
      RegisterSubcommand(new TeleportSubcommand());
    }

    public override CommandResult Execute(CommandContext context) {
      var type = context.GetValue(_type);
      var name = context.GetValue(_name);
      var amount = context.GetValue(_amount);
      var scale = context.GetValue(_scale);
      var pos = context.GetValue(_position);
      var silent = context.GetValue(_silent);
      var tags = context.GetValue(_tags);
      var mods = context.GetListValue(_modifiers);

      var sb = new StringBuilder();
      sb.Append($"Spawning {amount}x {type} named '{name}' at {pos} with scale {scale}");
      if (mods.Count > 0) sb.Append($" with modifiers: {string.Join(", ", mods)}");
      if (!string.IsNullOrEmpty(tags)) sb.Append($" and tags: {tags}");
      if (silent) sb.Append(" (silently)");

      UnityEngine.Debug.Log(sb.ToString());
      return CommandResult.Successful();
    }

    public static void RunExample() {
      var system = new CommandSystem();
      system.Register(new SpawnCommand());

      // Normal usage
      DebugLogResult(
        system.Execute("spawn Orc \"Green Orc\" 1 1.5 0,10,0 -silent --tags \"tag1,tag2\" mod1,mod2")
      );

      // Literal with escaped quotes
      DebugLogResult(system.Execute("spawn Goblin \"\\\"Fast\\\" Goblin\""));

      // Subcommand with arguments
      DebugLogResult(system.Execute("spawn config debug --speed 1.5 -verbose"));

      // New Teleport Subcommand
      DebugLogResult(system.Execute("spawn tp 10,0,10 45,90"));

      // Error: unknown flag
      DebugLogResult(system.Execute("spawn Dragon -unknown"));

      // Error: too many positional
      DebugLogResult(system.Execute("spawn Orc Name 1 1.0 0,0,0 Extra"));
    }

    private static void DebugLogResult(CommandResult result) {
      if (result.success) UnityEngine.Debug.Log("Command executed successfully.");
      else UnityEngine.Debug.LogError($"Command failed: {result.error}");
    }

    private class InfoSubcommand : Command {

      public override string Name => "info";
      public override string Description => "Shows info about spawning.";

      public override CommandResult Execute(CommandContext context) {
        UnityEngine.Debug.Log("Spawn command allows you to create entities in the world.");
        return CommandResult.Successful();
      }

    }

    private class ConfigSubcommand : Command {

      public override string Name => "config";
      public override string Description => "Configures spawn settings.";

      private readonly StringArgument _key;
      private readonly NamedArgument<float> _speed;
      private readonly FlagArgument _verbose;

      public ConfigSubcommand() {
        _key = new StringArgument("key", "The config key", isRequired: true);
        _speed = new NamedArgument<float>(new FloatArgument("speed", "The speed multiplier", 1.0f));
        _verbose = new FlagArgument("verbose", "Enable verbose logging");

        Register(_key);
        Register(_speed);
        Register(_verbose);
      }

      public override CommandResult Execute(CommandContext context) {
        var key = context.GetValue(_key);
        var speed = context.GetValue(_speed);
        var verbose = context.GetValue(_verbose);

        UnityEngine.Debug.Log(
          $"Config updated: {key} = {speed.ToString(CultureInfo.InvariantCulture)} (Verbose: {verbose})"
        );
        return CommandResult.Successful();
      }

    }

    private class TeleportSubcommand : Command {

      public override string Name => "tp";
      public override string Description => "Teleports to a location.";

      private readonly Vector3Argument _destination = new(
        "dest",
        "Destination position",
        isRequired: true
      );

      private readonly Vector2Argument _rotation = new(
        "rot",
        "Rotation (pitch, yaw)",
        Vector2.zero
      );

      public TeleportSubcommand() {
        Register(_destination, _rotation);
      }

      public override CommandResult Execute(CommandContext context) {
        var dest = context.GetValue(_destination);
        var rot = context.GetValue(_rotation);
        UnityEngine.Debug.Log($"Teleporting to {dest} with rotation {rot}");
        return CommandResult.Successful();
      }

    }

  }
}