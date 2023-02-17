using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ZLibDotNet;

/// <summary>
/// An implementation of <see cref="ObjectPool{T}"/> based on the one in dotnet/aspnetcore GitHub repository.
/// </summary>
/// <typeparam name="T">The type to pool objects for.</typeparam>
/// <remarks>This implementation keeps a cache of retained objects. This means that if objects are returned when the pool has already reached "maximumRetained" objects they will be available to be garbage collected.</remarks>
internal class ObjectPool<T> where T : class, new()
{
    private readonly int _maxCapacity;
    private int _numItems;

    private protected readonly ConcurrentQueue<T> _items = new();
    private protected T _fastItem;

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
         _maxCapacity = maximumRetained - 1;  // -1 to account for _fastItem

    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns>A <typeparamref name="T"/>.</returns>
    public T Get()
    {
        T item = _fastItem;
        if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) != item)
        {
            if (_items.TryDequeue(out item))
            {
                _ = Interlocked.Decrement(ref _numItems);
                return item;
            }

            // no object available, so go get a brand new one
            return new();
        }

        return item;
    }

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    /// <param name="obj">The object to add to the pool.</param>
    public void Return(T obj)
    {
        if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
        {
            if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
                _items.Enqueue(obj);

            // no room, clean up the count and drop the object on the floor
            _ = Interlocked.Decrement(ref _numItems);
        }
    }
}