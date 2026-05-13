using System;
using System.Collections.Generic;
using System.Globalization;

namespace Spookline.SPC.Console.Arguments {
    public class IntArgument : LeafArgument<int> {

        public IntArgument(
            string name,
            string description,
            int defaultValue = 0,
            bool isRequired = false,
            int? min = null,
            int? max = null
        ) : base(name, description, defaultValue, isRequired) {
            Min = min;
            Max = max;
        }

        public override string Prefix => "";
        public override bool IsFlag => false;
        public override bool IsNamed => false;
        public override bool IsList => false;

        public int? Min { get; }
        public int? Max { get; }

        public override object Parse(string input) {
            if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)) {
                if (result < Min)
                    throw new ArgumentException($"Value {result} is below minimum {Min.Value} for argument {Name}");

                if (result > Max)
                    throw new ArgumentException($"Value {result} is above maximum {Max.Value} for argument {Name}");

                return result;
            }

            throw new ArgumentException($"Invalid integer value '{input}' for argument {Name}");
        }

        public override List<string> GetCompletions(string input) {
            return new List<string>();
        }

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
}