using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ImGui;

using ImPoolIdx = Int32;

/// <summary>
/// Dynamic array mirroring ImVector from imgui.h.
/// </summary>
public unsafe struct ImVector<T> : IDisposable where T : unmanaged
{
    public int Size;
    public int Capacity;
    public T* Data;

    public Span<T> Span => new(Data, Size);
    public ReadOnlySpan<T> ReadOnlySpan => new(Data, Size);

    public bool IsEmpty => Size == 0;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)Size)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref Data[index];
        }
    }

    public void Clear()
    {
        Size = 0;
    }

    public void Reserve(int newCapacity)
    {
        if (newCapacity <= Capacity)
            return;
        nuint bytes = (nuint)(newCapacity * sizeof(T));
        Data = (T*)NativeMemory.Realloc(Data, bytes);
        Capacity = newCapacity;
    }

    public void Resize(int newSize)
    {
        Reserve(newSize);
        if (newSize > Size)
        {
            var span = new Span<T>(Data + Size, newSize - Size);
            span.Clear();
        }
        Size = newSize;
    }

    public void PushBack(T value)
    {
        if (Size == Capacity)
        {
            int target = Capacity == 0 ? 4 : Capacity * 2;
            Reserve(target);
        }
        Data[Size++] = value;
    }

    public T PopBack()
    {
        if (Size == 0)
            throw new InvalidOperationException("Vector is empty.");
        T value = Data[Size - 1];
        Size--;
        return value;
    }

    public void Dispose()
    {
        if (Data != null)
        {
            NativeMemory.Free(Data);
            Data = null;
            Size = 0;
            Capacity = 0;
        }
    }
}

/// <summary>
/// Chunked stream mirroring ImChunkStream from imgui.h.
/// Stores elements in a contiguous byte buffer to preserve layout.
/// </summary>
public unsafe struct ImChunkStream<T> : IDisposable where T : unmanaged
{
    private ImVector<byte> _buffer;

    public int Size => _buffer.Size / sizeof(T);
    public bool IsEmpty => Size == 0;

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)Size)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref ((T*)_buffer.Data)[index];
        }
    }

    public Span<T> Span => new(_buffer.Data, Size);

    public void Clear()
    {
        _buffer.Clear();
    }

    public void Reserve(int capacity)
    {
        _buffer.Reserve(capacity * sizeof(T));
    }

    public void Resize(int newSize)
    {
        _buffer.Resize(newSize * sizeof(T));
    }

    public void PushBack(in T value)
    {
        int offsetBytes = _buffer.Size;
        _buffer.Resize(offsetBytes + sizeof(T));
        ((T*)(_buffer.Data + offsetBytes))[0] = value;
    }

    public void Dispose()
    {
        _buffer.Dispose();
    }
}

/// <summary>
/// Pool of items with recycled indices (ImPool in imgui.h).
/// </summary>
public sealed class ImPool<T>
{
    private readonly List<T> _data = new();
    private readonly Stack<int> _free = new();

    public int Count => _data.Count - _free.Count;
    public int Capacity => _data.Count;

    public void Clear()
    {
        _data.Clear();
        _free.Clear();
    }

    public ImPoolIdx GetOrAdd()
    {
        if (_free.Count > 0)
        {
            return _free.Pop();
        }

        _data.Add(default!);
        return _data.Count - 1;
    }

    public ref T Get(ImPoolIdx idx)
    {
        var span = CollectionsMarshal.AsSpan(_data);
        if ((uint)idx >= (uint)span.Length)
            throw new ArgumentOutOfRangeException(nameof(idx));
        return ref span[idx];
    }

    public bool TryGet(ImPoolIdx idx, out T value)
    {
        if ((uint)idx >= (uint)_data.Count)
        {
            value = default!;
            return false;
        }

        value = _data[idx];
        return true;
    }

    public void Remove(ImPoolIdx idx)
    {
        if ((uint)idx >= (uint)_data.Count)
            return;
        _data[idx] = default!;
        _free.Push(idx);
    }
}
