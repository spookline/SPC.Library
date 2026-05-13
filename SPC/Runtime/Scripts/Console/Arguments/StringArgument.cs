using System.Collections.Generic;

namespace Spookline.SPC.Console.Arguments {
    public class StringArgument : LeafArgument<string> {

        public StringArgument(string name, string description, string defaultValue = "", bool isRequired = false)
            : base(name, description, defaultValue, isRequired) { }

        public override string Prefix => "";
        public override bool IsFlag => false;
        public override bool IsNamed => false;
        public override bool IsList => false;

        public override object Parse(string input) {
            return input;
        }

        public override List<string> GetCompletions(string input) {
            return new List<string>();
        }

    }
}