using System;
using System.Collections.Generic;
using System.Linq;

namespace Spookline.SPC.Console.Arguments {
    public class EnumArgument<T> : LeafArgument<T> where T : struct, Enum {

        public EnumArgument(string name, string description, T defaultValue = default, bool isRequired = false)
            : base(name, description, defaultValue, isRequired) { }

        public override string Prefix => "";
        public override bool IsFlag => false;
        public override bool IsNamed => false;
        public override bool IsList => false;

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

    public class EnumStringArgument : LeafArgument<string> {

        public Func<IEnumerable<string>> OptionsFunc { get; set; }

        public EnumStringArgument(
            Func<IEnumerable<string>> optionsFunc,
            string name,
            string description,
            string defaultValue = default,
            bool isRequired = false
        ) : base(name, description, defaultValue, isRequired) {
            OptionsFunc = optionsFunc;
        }


        public override string Prefix => "";
        public override bool IsFlag => false;
        public override bool IsNamed => false;
        public override bool IsList => false;

        public override object Parse(string input) {
            var options = OptionsFunc();
            if (options.Contains(input, StringComparer.OrdinalIgnoreCase)) return input;
            throw new ArgumentException($"Invalid value '{input}' for argument {Name}");
        }

        public override List<string> GetCompletions(string input) {
            var options = OptionsFunc();
            return options.Where(n => n.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

    }
}