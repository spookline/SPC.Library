using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Spookline.SPC.Console.Arguments {
    public class Vector3Argument : LeafArgument<Vector3> {

        public Vector3Argument(string name, string description, Vector3 defaultValue = default, bool isRequired = false)
            : base(name, description, defaultValue, isRequired) { }

        public override string Prefix => "";
        public override bool IsFlag => false;
        public override bool IsNamed => false;
        public override bool IsList => false;

        public override object Parse(string input) {
            var parts = input.Split(',');
            if (parts.Length == 3 &&
                float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
                return new Vector3(x, y, z);

            throw new ArgumentException(
                $"Invalid Vector3 value '{input}' for argument {Name}. Expected format: 'x,y,z'"
            );
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
            return help + $"<color={style.weak}>Vec3</color>";
        }

    }
}