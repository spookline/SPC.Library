using System.Collections.Generic;
using System.Linq;

namespace Spookline.SPC.Console.Arguments {
    public class ListArgument<T> : NameableArgument<List<T>> {

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

        public override string Prefix => "";
        public override bool IsFlag => false;
        public override bool IsNamed => false;
        public override bool IsList => true;
        public override bool UseFormat => true;

        public override object Parse(string input) {
            var parts = CommandSystem.SplitList(input);
            var result = new List<T>();
            foreach (var part in parts) result.Add((T)_elementArg.Parse(part));

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


        public override string Format(object value) {
            if (value is List<T> list) return string.Join(",", list.Select(e => _elementArg.Format(e)));
            return base.Format(value);
        }

    }
}