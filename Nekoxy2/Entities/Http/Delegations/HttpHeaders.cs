using System.Collections;
using System.Collections.Generic;

namespace Nekoxy2.Entities.Http.Delegations
{
    internal sealed class ReadOnlyHttpHeaders : IReadOnlyHttpHeaders
    {
        private readonly Spi.Entities.Http.IReadOnlyHttpHeaders source;

        public int Count => this.source.Count;

        public IEnumerator<(string Name, string Value)> GetEnumerator()
            => this.source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.source.GetEnumerator();

        private ReadOnlyHttpHeaders(Spi.Entities.Http.IReadOnlyHttpHeaders source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyHttpHeaders)
                return this.source.Equals((obj as ReadOnlyHttpHeaders).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static ReadOnlyHttpHeaders Convert(Spi.Entities.Http.IReadOnlyHttpHeaders source)
            => new ReadOnlyHttpHeaders(source);
    }

    internal sealed class HttpHeaders : IHttpHeaders
    {
        private readonly Spi.Entities.Http.IHttpHeaders source;

        public bool IsReadOnly => this.source.IsReadOnly;

        public int Count => this.source.Count;

        public (string Name, string Value) this[int index]
        {
            get => this.source[index];
            set => this.source[index] = value;
        }

        public IEnumerator<(string Name, string Value)> GetEnumerator()
            => this.source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.source.GetEnumerator();

        public int IndexOf((string Name, string Value) item) => this.source.IndexOf(item);
        public void Insert(int index, (string Name, string Value) item) => this.source.Insert(index, item);
        public void RemoveAt(int index) => this.source.RemoveAt(index);

        public void Add((string Name, string Value) item) => this.source.Add(item);
        public void Clear() => this.source.Clear();
        public bool Contains((string Name, string Value) item) => this.source.Contains(item);
        public void CopyTo((string Name, string Value)[] array, int arrayIndex) => this.source.CopyTo(array, arrayIndex);
        public bool Remove((string Name, string Value) item) => this.source.Remove(item);

        private HttpHeaders(Spi.Entities.Http.IHttpHeaders source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is HttpHeaders)
                return this.source.Equals((obj as HttpHeaders).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static HttpHeaders Convert(Spi.Entities.Http.IHttpHeaders source)
            => new HttpHeaders(source);
    }
}
