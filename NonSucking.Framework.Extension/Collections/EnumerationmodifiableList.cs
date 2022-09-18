using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NonSucking.Framework.Extension.Collections
{
    /// <summary>
    /// List that can be modified during enumeration, but is not threadsafe
    /// </summary>
    /// <typeparam name="T">Type to hold in the list</typeparam>
    public class EnumerationModifiableList<T> : IList<T>, IReadOnlyCollection<T>
    {
        T IList<T>.this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                list[index] = value;
            }
        }

        /// <inheritdoc/>
        public int Count => list.Count;
        /// <inheritdoc/>
        public bool IsReadOnly => ((ICollection<T>)list).IsReadOnly;


        private readonly List<T> list;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerationModifiableList{T}"/> class that is empty and has the default initial capacity.
        /// </summary>
        public EnumerationModifiableList()
        {
            list = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerationModifiableList{T}"/> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection" /> is <see langword="null" />.</exception>
        public EnumerationModifiableList(IEnumerable<T> collection)
        {
            list = new List<T>(collection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerationModifiableList{T}"/> class that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="capacity" /> is less than 0.</exception>
        public EnumerationModifiableList(int capacity)
        {
            list = new List<T>(capacity);
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            list.Add(item);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            list.Clear();
        }
        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return list.Contains(item);
        }
        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }
        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }
        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            list.Insert(index, item);
        }
        /// <inheritdoc/>
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index == -1)
                return false;

            RemoveAt(index);
            return true;
        }
        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the elements of a <see cref="EnumerationModifiableList{T}"/>.
        /// </summary>
        public sealed class Enumerator : IEnumerator<T>
        {
            /// <inheritdoc/>
            public T Current { get; private set; }
            object IEnumerator.Current => Current;

            private int currentIndex = -1;
            private readonly EnumerationModifiableList<T> parent;

            internal Enumerator(EnumerationModifiableList<T> list)
            {
                parent = list;
            }

            internal void RemoveAt(int index)
            {
                if (index > currentIndex)
                    return;

                if (index == currentIndex)
                    Current = default;
                currentIndex--;
            }

            internal void InsertAt(int index)
            {
                if (currentIndex < index)
                    return;


                currentIndex++;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                currentIndex++;
                if (currentIndex >= parent.Count)
                    return false;

                Current = parent.list[currentIndex];
                return true;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                currentIndex = -1;
                Current = default;
            }

        }

    }
}