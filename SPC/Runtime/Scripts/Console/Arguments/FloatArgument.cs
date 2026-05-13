using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Spookline.SPC.Console.Arguments {
    public class FloatArgument : LeafArgument<float> {

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

        public override string Prefix => "";
        public override bool IsFlag => false;
        public override bool IsNamed => false;
        public override bool IsList => false;

        public float? Min { get; }
        public float? Max { get; }

        public override object Parse(string input) {
            var multiplier = 1f;
            if (input.EndsWith("%")) {
                input = input.Substring(0, input.Length - 1);
                multiplier = 0.01f;
            }

            if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) {
                result *= multiplier;
                if (result < Min && !Mathf.Approximately(result, Min.Value))
                    throw new ArgumentException($"Value {result} is below minimum {Min.Value} for argument {Name}");
                if (result > Max && !Mathf.Approximately(result, Max.Value))
                    throw new ArgumentException($"Value {result} is above maximum {Max.Value} for argument {Name}");
                return result;
            }

            throw new ArgumentException($"Invalid float value '{input}' for argument {Name}");
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