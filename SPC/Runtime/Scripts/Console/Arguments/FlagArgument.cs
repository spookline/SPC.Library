using System.Collections.Generic;

namespace Spookline.SPC.Console.Arguments {
    public class FlagArgument : Argument<bool> {

        public FlagArgument(string name, string description, bool invert = false) :
            base(name, description) {
            Invert = invert;
        }

        public override string Prefix => "-";
        public override bool IsFlag => true;
        public override bool IsNamed => false;
        public override bool IsList => false;
        public bool Invert { get; set; }

        public override object Parse(string input) {
            return true;
        }

        public override List<string> GetCompletions(string input) {
            return new List<string>();
        }

        public override bool Get(CommandContext context) {
            return context.HasValue(this) != Invert;
        }

        public override bool TryGet(CommandContext context, out bool value) {
            value = context.HasValue(this) != Invert;
            return true;
        }

    }
}