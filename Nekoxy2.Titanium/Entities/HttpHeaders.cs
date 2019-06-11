using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;

namespace Nekoxy2.Titanium.Entities
{
    internal sealed class HttpHeaders : IHttpHeaders
    {
        private IList<(string Name, string Value)> Source { get; }

        public (string Name, string Value) this[int index]
        {
            get => this.Source[index];
            set => this.Source[index] = value;
        }

        public int Count => this.Source.Count;

        public bool IsReadOnly => this.Source.IsReadOnly;

        public IEnumerator<(string Name, string Value)> GetEnumerator()
            => this.Source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.Source.GetEnumerator();

        public int IndexOf((string Name, string Value) item)
            => this.Source.IndexOf(item);

        public void Insert(int index, (string Name, string Value) item)
            => this.Source.Insert(index, item);

        public void RemoveAt(int index)
            => this.Source.RemoveAt(index);

        public void Add((string Name, string Value) item)
            => this.Source.Add(item);

        public void Clear()
            => this.Source.Clear();

        public bool Contains((string Name, string Value) item)
            => this.Source.Contains(item);

        public void CopyTo((string Name, string Value)[] array, int arrayIndex)
            => this.Source.CopyTo(array, arrayIndex);

        public bool Remove((string Name, string Value) item)
            => this.Source.Remove(item);

        public HttpHeaders(HeaderCollection source)
        {
            this.Source = source.Select(x => x.ToTuple()).ToList();
        }
    }

    static partial class Extensions
    {
        public static (string Name, string Value) ToTuple(this HttpHeader header)
            => (header.Name, header.Value);

        public static HttpHeader ToHeader(this (string Name, string Value) tuple)
            => new HttpHeader(tuple.Name, tuple.Value);
    }
}
