using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;
using UsmParser.Utilities;

namespace UsmParser.UsmChunkEnumerables;

public sealed class StreamUsmChunkEnumerable
    : IUsmChunkEnumerable<MemoryUsmChunk, StreamUsmChunkEnumerable.Enumerator>, IUsmChunkEnumerable<MemoryUsmChunk>,
    IAsyncUsmChunkEnumerable<MemoryUsmChunk, StreamUsmChunkEnumerable.Enumerator>, IAsyncUsmChunkEnumerable<MemoryUsmChunk>,
    IDisposable
{
    private readonly Stream _stream;
    private readonly ArrayPool<byte>? _bufferPool;
    private readonly bool _leaveOpen;
    private bool _isConsumed;
    private bool _disposed;
    public StreamUsmChunkEnumerable(Stream stream, bool leaveOpen = false, ArrayPool<byte>? bufferPool = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
        _bufferPool = bufferPool;
        _leaveOpen = leaveOpen;
    }
    public uint InstanceMaxDataLength => (uint)Array.MaxLength;
    public static uint MaxDataLength => (uint)Array.MaxLength;
    public Enumerator GetEnumerator()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_isConsumed)
            throw new InvalidOperationException("The enumerable has already been consumed.");
        _isConsumed = true;
        return new(_stream, _leaveOpen, _bufferPool);
    }
    IEnumerator<MemoryUsmChunk> IUsmChunkEnumerable<MemoryUsmChunk, IEnumerator<MemoryUsmChunk>>.GetEnumerator()
        => GetEnumerator();
    IEnumerator<MemoryUsmChunk> IEnumerable<MemoryUsmChunk>.GetEnumerator()
        => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_isConsumed)
            throw new InvalidOperationException("The enumerable has already been consumed.");
        _isConsumed = true;
        return new(_stream, _leaveOpen, _bufferPool, cancellationToken);
    }
    IAsyncEnumerator<MemoryUsmChunk> IAsyncUsmChunkEnumerable<MemoryUsmChunk, IAsyncEnumerator<MemoryUsmChunk>>.GetAsyncEnumerator(CancellationToken cancellationToken)
       => GetAsyncEnumerator(cancellationToken);
    IAsyncEnumerator<MemoryUsmChunk> IAsyncEnumerable<MemoryUsmChunk>.GetAsyncEnumerator(CancellationToken cancellationToken)
       => GetAsyncEnumerator(cancellationToken);
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (!_leaveOpen)
                _stream.Dispose();
        }
    }

    public sealed class Enumerator
        : IUsmChunkEnumerator<MemoryUsmChunk>, IAsyncUsmChunkEnumerator<MemoryUsmChunk>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly Stream _stream;
        private readonly PooledBuffer _buffer;
        private readonly bool _leaveOpen;
        private bool _completed;
        private bool _disposed;

        internal Enumerator(Stream stream, bool leaveOpen = false, ArrayPool<byte>? bufferPool = null, CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
            _stream = stream;
            _buffer = new(0, bufferPool);
            _leaveOpen = leaveOpen;
        }

        public uint InstanceMaxDataLength => (uint)Array.MaxLength;
        public MemoryUsmChunk Current { get; private set; }
        object IEnumerator.Current => Current;
        public static uint MaxDataLength => (uint)Array.MaxLength;

        public bool MoveNext()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_completed)
                return false;
            Span<byte> header = stackalloc byte[8];
            switch (_stream.ReadAtLeast(header, 8, false))
            {
                case 0:
                    _completed = true;
                    return false;
                case 8:
                    uint signature = BinaryPrimitives.ReadUInt32BigEndian(header);
                    uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(header[4..]);
                    if (dataSize > Array.MaxLength)
                        throw new NotSupportedException($"Data size {dataSize} is too large to be processed.");
                    _buffer.EnsureCapacity((int)dataSize, discardOldData: true);
                    Memory<byte> memory = _buffer.GetBuffer()[..(int)dataSize];
                    if (_stream.ReadAtLeast(memory.Span, (int)dataSize, false) < dataSize)
                        goto default;
                    Current = new(signature, memory);
                    return true;
                default:
                    _completed = true;
                    throw new EndOfStreamException();
            }
        }
        public ValueTask<bool> MoveNextAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<bool>(_cancellationToken);
            if (_completed)
                return ValueTask.FromResult(false);
            return CoreMoveNextAsync();
        }
        private async ValueTask<bool> CoreMoveNextAsync()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _buffer.EnsureCapacity(8, discardOldData: true);
            Memory<byte> header = _buffer.GetBuffer()[..8];
            switch (await _stream.ReadAtLeastAsync(header, 8, false, _cancellationToken).ConfigureAwait(false))
            {
                case 0:
                    _completed = true;
                    return false;
                case 8:
                    uint signature = BinaryPrimitives.ReadUInt32BigEndian(header.Span);
                    uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(header.Span[4..]);
                    if (dataSize > Array.MaxLength)
                        throw new NotSupportedException($"Data size {dataSize} is too large to be processed.");
                    _buffer.EnsureCapacity((int)dataSize, discardOldData: true);
                    Memory<byte> memory = _buffer.GetBuffer()[..(int)dataSize];
                    if (await _stream.ReadAtLeastAsync(memory, (int)dataSize, false).ConfigureAwait(false) < dataSize)
                        goto default;
                    Current = new(signature, memory);
                    return true;
                default:
                    _completed = true;
                    throw new EndOfStreamException();
            }
        }
        public void Reset()
            => throw new NotSupportedException();
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _buffer.Dispose();
                if (!_leaveOpen)
                    _stream.Dispose();
            }
        }
        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                _buffer.Dispose();
                if (!_leaveOpen)
                    return _stream.DisposeAsync();
            }
            return ValueTask.CompletedTask;
        }
    }
}