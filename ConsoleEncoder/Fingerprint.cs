using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleEncoder
{
    public class Fingerprint<T> : IEquatable<Fingerprint<T>>, IEnumerable<T> where T : IEquatable<T>
    {
        public Fingerprint(IEnumerable<T> values)
        {
            _values = values.ToImmutableArray();
            _hash = GenerateHashCode(_values);
        }

        private readonly ImmutableArray<T> _values;
        public T this[int i] => _values[i];
        private readonly int _hash;

        public override string ToString() => $"[{string.Join(", ", _values)}]";

        private int GenerateHashCode(ICollection<T> values) => ((IStructuralEquatable)_values).GetHashCode(EqualityComparer<T>.Default);

        public override int GetHashCode() => _hash;

        public override bool Equals(object? other) => Equals(other as Fingerprint<T>);

        public bool Equals(Fingerprint<T>? other) => other != null && ((IStructuralEquatable)_values).Equals(other._values, EqualityComparer<T>.Default);

        public static bool operator ==(Fingerprint<T>? b1, Fingerprint<T>? b2) => b1 is null ? b2 is null : b1.Equals(b2);

        public static bool operator !=(Fingerprint<T>? b1, Fingerprint<T>? b2) => !(b1 == b2);

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();
    }
}
