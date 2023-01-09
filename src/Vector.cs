using System;
using System.Buffers;

namespace TinyEcs;

struct Vector<T>
{
    private T[] _array;
    private int _size;
    private const int _defaultCapacity = 4;

    public Vector(int capacity)
    {
        _array = ArrayPool<T>.Shared.Rent(capacity);
        _size = 0;
    }


    public int Count => _size;

    public ref T this[int index] => ref _array[index];


    public void Add(T item)
    {
        if (_size == _array.Length)
        {
            int newCapacity = _array.Length == 0 ? _defaultCapacity : _array.Length * 2;
            T[] newArray = ArrayPool<T>.Shared.Rent(newCapacity);
            Array.Copy(_array, newArray, _size);
            ArrayPool<T>.Shared.Return(_array);
            _array = newArray;
        }

        _array[_size++] = item;
    }

    public bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    public int IndexOf(T item)
    {
        for (int i = 0; i < _size; i++)
        {
            if (_array[i].Equals(item))
            {
                return i;
            }
        }

        return -1;
    }

    public void Clear()
    {
        ArrayPool<T>.Shared.Return(_array);
        _array = ArrayPool<T>.Shared.Rent(_defaultCapacity);
        _size = 0;
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_array);
        _array = null;
        _size = 0;
    }

    private void RemoveAt(int index)
    {
        _size--;
        for (int i = index; i < _size; i++)
        {
            _array[i] = _array[i + 1];
        }
    }


    public void Sort() => Array.Sort(_array);

    public Enumerator GetEnumerator() => new Enumerator(this);


    public ref struct Enumerator
    {
        private readonly Vector<T> _list;
        private int _index;

        public Enumerator(Vector<T> list)
        {
            _list = list;
            _index = -1;
        }

        public bool MoveNext() => ++_index < _list._size;
        public ref T Current => ref _list._array[_index];
    }
}