using System.Buffers;

namespace UsmParser.Utilities;

public sealed class PooledBuffer : IDisposable
{
    private readonly ArrayPool<byte> _pool;
    private byte[] _buffer;
    private bool _disposed;

    public PooledBuffer(int initialCapacity = 256, ArrayPool<byte>? pool = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCapacity);
        _pool = pool ?? ArrayPool<byte>.Shared;
        _buffer = initialCapacity > 0 ? _pool.Rent(initialCapacity) : [];
    }
    public int Capacity => _buffer.Length;
    public Memory<byte> GetBuffer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _buffer.AsMemory(0, Capacity);
    }
    public void EnsureCapacity(int required, bool clearBeforeReturn = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(required);
        if (required > Capacity)
        {
            int newCapacity = Math.Max(required, _buffer.Length * 2);
            byte[] newBuffer = _pool.Rent(newCapacity);
            if (_buffer.Length > 0)
            {
                _buffer.AsSpan().CopyTo(newBuffer);
                _pool.Return(_buffer, clearBeforeReturn);
            }
            _buffer = newBuffer;
        }
    }
    public void ReleaseBuffer(bool clearArray = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_buffer.Length > 0)
        {
            _pool.Return(_buffer, clearArray);
            _buffer = [];
        }
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            ReleaseBuffer();
            _disposed = true;
        }
    }
}