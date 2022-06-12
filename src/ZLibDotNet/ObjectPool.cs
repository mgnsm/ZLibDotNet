using System;
using System.Diagnostics;
using System.Threading;

namespace ZLibDotNet;

/// <summary>
/// An implementation of <see cref="ObjectPool{T}"/> based on the one in dotnet/aspnetcore GitHub repository.
/// </summary>
/// <typeparam name="T">The type to pool objects for.</typeparam>
/// <remarks>This implementation keeps a cache of retained objects. This means that if objects are returned when the pool has already reached "maximumRetained" objects they will be available to be garbage collected.</remarks>
internal class ObjectPool<T> where T : class, new()
{
    private readonly ObjectWrapper[] _items;
    private protected T _firstItem;

    /// <summary>
    /// Creates an instance of <see cref="ObjectPool{T}"/>.
    /// </summary>
    public ObjectPool() : this(Environment.ProcessorCount * 2)
    { }

    /// <summary>
    /// Creates an instance of <see cref="ObjectPool{T}"/>.
    /// </summary>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public ObjectPool(int maximumRetained) =>
        _items = new ObjectWrapper[maximumRetained - 1]; // -1 due to _firstItem

    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns>A <typeparamref name="T"/>.</returns>
    public T Get()
    {
        T item = _firstItem;
        if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
        {
            ObjectWrapper[] items = _items;
            for (int i = 0; i < items.Length; i++)
            {
                item = items[i].Element;
                if (item != null && Interlocked.CompareExchange(ref items[i].Element, null, item) == item)
                    return item;
            }
            item = new();
        }
        return item;
    }

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    /// <param name="obj">The object to add to the pool.</param>
    public void Return(T obj)
    {
        if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
        {
            ObjectWrapper[] items = _items;
            for (int i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i].Element, obj, null) != null; ++i)
            { }
        }
    }

    // PERF: the struct wrapper avoids array-covariance-checks from the runtime when assigning to elements of the array.
    [DebuggerDisplay("{Element}")]
    private protected struct ObjectWrapper
    {
        public T Element;
    }
}