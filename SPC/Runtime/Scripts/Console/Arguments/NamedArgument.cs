using System.Collections.Generic;

namespace Spookline.SPC.Console.Arguments {
    public class NamedArgument<T> : Argument<T> {

        private readonly NameableArgument<T> _inner;

        public NamedArgument(NameableArgument<T> inner) : base(
            inner.Name,
            inner.Description,
            inner.DefaultValue,
            inner.IsRequired
        ) {
            _inner = inner;
        }

        public override string Prefix => "--";
        public override bool IsFlag => false;
        public override bool IsNamed => true;
        public override bool IsList => false;

        public override bool UseFormat => _inner.UseFormat;

        public override object Combine(object oldValue, object newValue) {
            return _inner.Combine(oldValue, newValue);
        }

        public override object Parse(string input) {
            return _inner.Parse(input);
        }

        public override List<string> GetCompletions(string input) {
            return _inner.GetCompletions(input);
        }

        public override string Format(object value) {
            return _inner.Format(value);
        }

    }
}