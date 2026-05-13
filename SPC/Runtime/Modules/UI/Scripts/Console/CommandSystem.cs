using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Spookline.SPC {
  public struct CommandResult {

    public bool success;
    public bool hasMessage;
    public object message;

    public static CommandResult Successful() => new() { success = true };

    public static CommandResult Successful(object message) =>
      new() {
        success = true,
        hasMessage = true,
        message = message
      };

    public static CommandResult Failed(object error) => new() { success = false, hasMessage = true, message = error };

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

    public string RawInput { get; internal set; }

    public Command Command { get; internal set; }
    // Values are stored by argument reference or name
    private Dictionary<Argument, object> _values = new();

    public bool HasValue(Argument arg) => _values.ContainsKey(arg);
    public object GetRawValue(Argument arg) => _values.GetValueOrDefault(arg);
    public void SetValue(Argument arg, object value) => _values[arg] = value;
    public T GetValue<T>(Argument<T> arg) => _values.TryGetValue(arg, out var val) ? (T)val : arg.DefaultValue;

  }

  public abstract class Argument {

    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public abstract string Prefix { get; }
    public abstract bool IsFlag { get; }
    public abstract bool IsNamed { get; }
    public abstract bool IsList { get; }
    public virtual object Combine(object oldValue, object newValue) => newValue;

    public abstract object Parse(string input);
    public abstract List<string> GetCompletions(string input);

    public virtual string GetShortHelp(
      CommandInfoRichTextStyle style,
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
        var valColor = isMalformed ? style.error : style.valid;
        if (IsFlag) { display = $"<color={valColor}>{name}</color>"; } else {
          display = $"<color={style.weak}>{Name}:</color><color={valColor}>{currentValue}</color>";
        }
      }

      if (isActive) { return $"<b><color={style.active}>{display}</color></b>"; }

      return display;
    }

    public virtual string GetLongHelp(CommandInfoRichTextStyle style) => $"{GetShortHelp(style)}: {Description}";

  }

  public class CommandInfoRichTextStyle {

    public static readonly CommandInfoRichTextStyle Default = new();

    public string error = "#FF0000";
    public string active = "#FFFF00";
    public string valid = "#00FF00";
    public string weak = "#808080";

  }

  public abstract class Argument<T> : Argument {

    public T DefaultValue { get; protected set; }

    protected Argument(string name, string description, T defaultValue = default, bool isRequired = false) {
      Name = name;
      Description = description;
      DefaultValue = defaultValue;
      IsRequired = isRequired;
    }

    public virtual T Get(CommandContext context) {
      return context.GetValue(this);
    }

    public virtual bool TryGet(CommandContext context, out T value) {
      if (context.HasValue(this)) {
        value = Get(context);
        return true;
      }

      value = DefaultValue;
      return false;
    }

    public T this[CommandContext context] => Get(context);

  }

  public abstract class NameableArgument<T> : Argument<T> {

    protected NameableArgument(string name, string description, T defaultValue = default, bool isRequired = false) :
      base(name, description, defaultValue, isRequired) { }

  }


  public abstract class LeafArgument<T> : NameableArgument<T> {

    protected LeafArgument(string name, string description, T defaultValue = default, bool isRequired = false) : base(
      name,
      description,
      defaultValue,
      isRequired
    ) { }

  }

  public static class ArgumentExtensions {

    public static ArgumentPreset<LeafArgument<string>, string> String(
      this ArgumentPreset preset,
      string defaultValue = null
    ) {
      return new ArgumentPreset<LeafArgument<string>, string>(
        preset,
        new StringArgument(preset.name, "", defaultValue)
      );
    }

    public static ArgumentPreset<LeafArgument<int>, int> Int(
      this ArgumentPreset preset,
      int defaultValue = 0
    ) {
      return new ArgumentPreset<LeafArgument<int>, int>(preset, new IntArgument(preset.name, "", defaultValue));
    }

    public static ArgumentPreset<LeafArgument<float>, float> Float(
      this ArgumentPreset preset,
      float defaultValue = 0f,
      float? min = null,
      float? max = null
    ) {
      return new ArgumentPreset<LeafArgument<float>, float>(
        preset,
        new FloatArgument(preset.name, "", defaultValue, false, min, max)
      );
    }

    public static ArgumentPreset<Argument<bool>, bool> Flag(
      this ArgumentPreset preset,
      string description = "",
      bool invert = false,
      bool addNoPrefix = true
    ) {
      if (invert && addNoPrefix)
        return new ArgumentPreset<Argument<bool>, bool>(
          preset,
          new FlagArgument($"no-{preset.name}", description, true)
        );
      return new ArgumentPreset<Argument<bool>, bool>(preset, new FlagArgument(preset.name, description, invert));
    }

    public static ArgumentPreset<LeafArgument<Vector2>, Vector2> Vec2(
      this ArgumentPreset preset,
      Vector2 defaultValue = default
    ) {
      return new ArgumentPreset<LeafArgument<Vector2>, Vector2>(
        preset,
        new Vector2Argument(preset.name, "", defaultValue)
      );
    }

    public static ArgumentPreset<LeafArgument<Vector3>, Vector3> Vec3(
      this ArgumentPreset preset,
      Vector3 defaultValue = default
    ) {
      return new ArgumentPreset<LeafArgument<Vector3>, Vector3>(
        preset,
        new Vector3Argument(preset.name, "", defaultValue)
      );
    }

    public static ArgumentPreset<LeafArgument<T>, T> Enum<T>(
      this ArgumentPreset preset,
      T defaultValue = default
    ) where T : struct, Enum {
      return new ArgumentPreset<LeafArgument<T>, T>(
        preset,
        new EnumArgument<T>(preset.name, "", defaultValue)
      );
    }

    public static ArgumentPreset<NameableArgument<List<T>>, List<T>> List<T>(
      this ArgumentPreset<LeafArgument<T>, T> preset,
      List<T> defaultValue = null
    ) {
      return new ArgumentPreset<NameableArgument<List<T>>, List<T>>(
        preset.descriptor,
        new ListArgument<T>(preset.argument, "", defaultValue: defaultValue)
      );
    }

  }

  public class StringArgument : LeafArgument<string> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => false;

    public StringArgument(string name, string description, string defaultValue = "", bool isRequired = false)
      : base(name, description, defaultValue, isRequired) { }

    public override object Parse(string input) => input;
    public override List<string> GetCompletions(string input) => new();

  }

  public class IntArgument : LeafArgument<int> {

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

    public override List<string> GetCompletions(string input) => new();

  }

  public class FloatArgument : LeafArgument<float> {

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

    public override List<string> GetCompletions(string input) => new();

    public override string GetShortHelp(
      CommandInfoRichTextStyle style,
      string currentValue = null,
      bool isActive = false,
      bool isMalformed = false
    ) {
      var help = base.GetShortHelp(style, currentValue, isActive, isMalformed);
      if (!string.IsNullOrEmpty(currentValue)) return help;

      if (Min.HasValue || Max.HasValue) {
        var range = "(";
        if (Min.HasValue) range += Min.Value.ToString(CultureInfo.InvariantCulture);
        range += "..";
        if (Max.HasValue) range += Max.Value.ToString(CultureInfo.InvariantCulture);
        range += ")";
        return help + $"<color={style.weak}>" + range + "</color>";
      }

      return help;
    }

  }

  public class Vector2Argument : LeafArgument<Vector2> {

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

    public override List<string> GetCompletions(string input) => new();


    public override string GetShortHelp(
      CommandInfoRichTextStyle style,
      string currentValue = null,
      bool isActive = false,
      bool isMalformed = false
    ) {
      var help = base.GetShortHelp(style, currentValue, isActive, isMalformed);
      if (!string.IsNullOrEmpty(currentValue)) return help;
      return help + $"<color={style.weak}>Vec2</color>";
    }

  }

  public class Vector3Argument : LeafArgument<Vector3> {

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

    public override List<string> GetCompletions(string input) => new();

    public override string GetShortHelp(
      CommandInfoRichTextStyle style,
      string currentValue = null,
      bool isActive = false,
      bool isMalformed = false
    ) {
      var help = base.GetShortHelp(style, currentValue, isActive, isMalformed);
      if (!string.IsNullOrEmpty(currentValue)) return help;
      return help + $"<color={style.weak}>Vec3</color>";
    }

  }

  public class EnumArgument<T> : LeafArgument<T> where T : struct, Enum {

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
    public bool Invert { get; set; } = false;

    public FlagArgument(string name, string description, bool invert = false) :
      base(name, description, false, false) {
      Invert = invert;
    }

    public override object Parse(string input) => true;
    public override List<string> GetCompletions(string input) => new();

    public override bool Get(CommandContext context) {
      return context.HasValue(this) != Invert;
    }

    public override bool TryGet(CommandContext context, out bool value) {
      value = context.HasValue(this) != Invert;
      return true;
    }

  }

  public class NamedArgument<T> : Argument<T> {

    public override string Prefix => "--";
    public override bool IsFlag => false;
    public override bool IsNamed => true;
    public override bool IsList => false;

    private readonly NameableArgument<T> _inner;

    public NamedArgument(NameableArgument<T> inner) : base(
      inner.Name,
      inner.Description,
      inner.DefaultValue,
      inner.IsRequired
    ) {
      _inner = inner;
    }

    public override object Combine(object oldValue, object newValue) => _inner.Combine(oldValue, newValue);
    public override object Parse(string input) => _inner.Parse(input);
    public override List<string> GetCompletions(string input) => _inner.GetCompletions(input);

  }

  public class ListArgument<T> : NameableArgument<List<T>> {

    public override string Prefix => "";
    public override bool IsFlag => false;
    public override bool IsNamed => false;
    public override bool IsList => true;

    private readonly LeafArgument<T> _elementArg;

    public ListArgument(
      LeafArgument<T> elementArg,
      string description,
      bool isRequired = false,
      List<T> defaultValue = null
    )
      : base(elementArg.Name, description, defaultValue ?? new List<T>(), isRequired) {
      _elementArg = elementArg;
    }

    public override object Parse(string input) {
      var parts = CommandSystem.SplitList(input);
      var result = new List<T>();
      foreach (var part in parts) { result.Add((T)_elementArg.Parse(part)); }

      return result;
    }

    public override object Combine(object oldValue, object newValue) {
      if (oldValue is List<T> oldList && newValue is List<T> newList) {
        var combined = new List<T>(oldList);
        combined.AddRange(newList);
        return combined;
      }

      return newValue;
    }

    public override List<string> GetCompletions(string input) {
      var parts = CommandSystem.SplitList(input);
      var lastPart = parts.Count > 0 ? parts[^1] : "";
      return _elementArg.GetCompletions(lastPart);
    }

  }

  public readonly struct ArgumentPreset {

    public readonly string name;
    public readonly string description;
    public readonly bool isRequired;
    public readonly bool isNamed;

    public ArgumentPreset(string name, string description, bool isRequired = false, bool isNamed = false) {
      this.name = name;
      this.description = description;
      this.isRequired = isRequired;
      this.isNamed = isNamed;
    }

  }

  public readonly struct ArgumentPreset<T, V> where T : Argument<V> {

    public readonly ArgumentPreset descriptor;
    public readonly T argument;

    public ArgumentPreset(ArgumentPreset descriptor, T argument) {
      this.descriptor = descriptor;
      this.argument = argument;
    }

    public static implicit operator Argument<V>(ArgumentPreset<T, V> preset) {
      Argument<V> argument = preset.argument;
      if (argument is FlagArgument) {
        argument.Description = preset.descriptor.description;
        return argument;
      }

      if (preset.descriptor.isNamed) {
        if (preset.argument is NameableArgument<V> named) argument = new NamedArgument<V>(named);
        else throw new InvalidOperationException("Argument is not nameable");
      }

      argument.IsRequired = preset.descriptor.isRequired;
      argument.Description = preset.descriptor.description;
      return argument;
    }

  }

  public abstract class Command {

    protected static ArgumentPreset Argument(string name, string description = "") =>
      new ArgumentPreset(
        name,
        description,
        isRequired: true
      );

    protected static ArgumentPreset Optional(string name, string description = "") =>
      new ArgumentPreset(
        name,
        description,
        isRequired: false
      );

    public static ArgumentPreset Named(
      string name,
      string description = "",
      bool isRequired = false
    ) =>
      new ArgumentPreset(
        name,
        description,
        isRequired: isRequired,
        isNamed: true
      );

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

    public abstract string Name { get; }
    public abstract string Description { get; }
    public List<Argument> AllArguments { get; } = new();
    public List<Command> AllChildren { get; } = new();

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


    public abstract CommandResult Execute(CommandContext context);

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
        if (activeArg == null) { subText = $"<b><color={style.weak}>{subText}</color></b>"; }

        sb.Append(" ").Append(subText);
      }

      foreach (var arg in AllArguments) {
        string val = null;
        values?.TryGetValue(arg, out val);
        var isMalformed = malformedArgs != null && malformedArgs.Contains(arg);
        sb.Append(" ").Append(arg.GetShortHelp(style, val, arg == activeArg, isMalformed));
      }

      if (!string.IsNullOrEmpty(errorSuffix)) { sb.Append($" <color={style.active}>!").Append(errorSuffix).Append("</color>"); }

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

  public class CommandSystem {

    private readonly List<Command> _commands = new();

    public void Register(Command cmd) => _commands.Add(cmd);

    public CommandResult Execute(string input) {
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
        var namedArgs = current.AllArguments.Where(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix))
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
              } else { return CommandResult.Failed($"Missing value for argument: {token}"); }
            } else { return CommandResult.Failed($"Unknown argument or flag: {token}"); }
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
          } else { return CommandResult.Failed($"Unexpected argument: {tokens[i]}"); }
        }

        // Check required
        foreach (var arg in current.AllArguments) {
          if (arg.IsRequired && !context.HasValue(arg)) {
            return CommandResult.Failed($"Missing required argument: {arg.Name}");
          }
        }

        return current.Execute(context);
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

        var sb = new StringBuilder($"Possible commands:\n");
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
          var cmds = _commands.Where(c => c.Name.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Name).ToList();
          return new CompletionResult(cmds);
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
        var namedArgs = current.AllArguments.Where(a => a.IsFlag || a.IsNamed || !string.IsNullOrEmpty(a.Prefix))
          .ToList();
        var positionalArgs = current.AllArguments
          .Where(a => !a.IsFlag && !a.IsNamed && string.IsNullOrEmpty(a.Prefix)).ToList();
        var usedTokens = new HashSet<int>();

        // 1. Identify named arguments/flags and their values
        for (var i = tokenIdx; i < tokens.Count - (endsWithSpace ? 0 : 1); i++) {
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
        var posIdx = 0;
        for (var i = tokenIdx; i < tokens.Count - (endsWithSpace ? 0 : 1); i++) {
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
            if (fullName.StartsWith(lastToken, StringComparison.OrdinalIgnoreCase)) { otherCompletions.Add(fullName); }
          }
        }

        completions.AddRange(prioritizedCompletions.Distinct());
        completions.AddRange(otherCompletions.Distinct().Except(completions));

        var atSubcommand = usedTokens.Count == 0 && posIdx == 0;
        var richInfo = current.GetShortHelp(
          style,
          providedValues,
          activeArg,
          atSubcommand,
          parentPath,
          malformedArgs,
          errorSuffix
        );
        if (!string.IsNullOrEmpty(argInfo)) { richInfo = argInfo + "\n" + richInfo; }

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
      var escaped = false;
      var inQuotes = false;
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
}