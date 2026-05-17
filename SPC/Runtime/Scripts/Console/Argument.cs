using System;
using System.Collections.Generic;
using Spookline.SPC.Console.Arguments;
using UnityEngine;

namespace Spookline.SPC.Console {
    public abstract class Argument {

        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
        public virtual bool UseFormat => false;
        public abstract string Prefix { get; }
        public abstract bool IsFlag { get; }
        public abstract bool IsNamed { get; }
        public abstract bool IsList { get; }

        public virtual object Combine(object oldValue, object newValue) {
            return newValue;
        }

        public abstract object Parse(string input);

        public virtual string Format(object value) {
            return value?.ToString() ?? "null";
        }

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
                if (IsFlag)
                    display = $"<color={valColor}>{name}</color>";
                else
                    display = $"<color={style.weak}>{Name}:</color><color={valColor}>{currentValue}</color>";
            }

            if (isActive) return $"<b><color={style.active}>{display}</color></b>";

            return display;
        }

        public virtual string GetLongHelp(CommandInfoRichTextStyle style) {
            return $"{GetShortHelp(style)}: {Description}";
        }

    }

    public abstract class Argument<T> : Argument {

        protected Argument(string name, string description, T defaultValue = default, bool isRequired = false) {
            Name = name;
            Description = description;
            DefaultValue = defaultValue;
            IsRequired = isRequired;
        }

        public T DefaultValue { get; protected set; }

        public T this[CommandContext context] => Get(context);

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

    }

    public abstract class NameableArgument<T> : Argument<T> {

        protected NameableArgument(string name, string description, T defaultValue = default, bool isRequired = false) :
            base(name, description, defaultValue, isRequired) { }

    }


    public abstract class LeafArgument<T> : NameableArgument<T> {

        protected LeafArgument(
            string name,
            string description,
            T defaultValue = default,
            bool isRequired = false
        ) : base(
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
            int defaultValue = 0,
            int? min = null,
            int? max = null
        ) {
            return new ArgumentPreset<LeafArgument<int>, int>(
                preset,
                new IntArgument(preset.name, "", defaultValue, false, min, max)
            );
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
            if (invert && addNoPrefix) {
                return new ArgumentPreset<Argument<bool>, bool>(
                    preset,
                    new FlagArgument($"no-{preset.name}", description, true)
                );
            }

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

        public static ArgumentPreset<LeafArgument<string>, string> Enum(
            this ArgumentPreset preset,
            Func<IEnumerable<string>> optionsFunc,
            string defaultValue = null,
            bool strict = true
        ) {
            return new ArgumentPreset<LeafArgument<string>, string>(
                preset,
                new EnumStringArgument(optionsFunc, preset.name, "", defaultValue, strict: strict)
            );
        }

        public static ArgumentPreset<LeafArgument<string>, string> Enum(
            this ArgumentPreset preset,
            IEnumerable<string> options,
            string defaultValue = null,
            bool strict = true
        ) {
            var list = new List<string>(options);
            return new ArgumentPreset<LeafArgument<string>, string>(
                preset,
                new EnumStringArgument(() => list, preset.name, "", defaultValue, strict: strict)
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

    public readonly struct ArgumentPreset<T, TV> where T : Argument<TV> {

        public readonly ArgumentPreset descriptor;
        public readonly T argument;

        public ArgumentPreset(ArgumentPreset descriptor, T argument) {
            this.descriptor = descriptor;
            this.argument = argument;
        }

        public static implicit operator Argument<TV>(ArgumentPreset<T, TV> preset) {
            Argument<TV> argument = preset.argument;
            if (argument is FlagArgument) {
                argument.Description = preset.descriptor.description;
                return argument;
            }

            if (preset.descriptor.isNamed) {
                if (preset.argument is NameableArgument<TV> named) argument = new NamedArgument<TV>(named);
                else throw new InvalidOperationException("Argument is not nameable");
            }

            argument.IsRequired = preset.descriptor.isRequired;
            argument.Description = preset.descriptor.description;
            return argument;
        }

    }
}