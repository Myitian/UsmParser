using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;
using UsmParser.Utilities;

namespace UsmParser.UsmChunkEnumerators;

public sealed class StreamUsmChunkEnumerator
    : IUsmChunkEnumerator<MemoryUsmChunk>, IAsyncUsmChunkEnumerable<MemoryUsmChunk>
{
    private readonly PooledBuffer _buffer;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private bool _completed;
    private bool _disposed;

    public StreamUsmChunkEnumerator(Stream stream, bool leaveOpen = false, ArrayPool<byte>? pool = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _buffer = new(0, pool);
        _stream = stream;
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
    public void Reset()
        => throw new NotSupportedException();
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
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
    public StreamUsmChunkEnumerator GetEnumerator()
        => this;
    public IAsyncEnumerator<MemoryUsmChunk> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumerator(this, cancellationToken);
    sealed class AsyncEnumerator(StreamUsmChunkEnumerator @this, CancellationToken cancellationToken)
        : IAsyncEnumerator<MemoryUsmChunk>
    {
        private readonly StreamUsmChunkEnumerator _this = @this;
        private readonly CancellationToken _cancellationToken = cancellationToken;
        public MemoryUsmChunk Current => _this.Current;

        public ValueTask<bool> MoveNextAsync()
        {
            if (_cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<bool>(_cancellationToken);
            if (_this._completed)
                return ValueTask.FromResult(false);
            return CoreMoveNextAsync();
        }
        private async ValueTask<bool> CoreMoveNextAsync()
        {
            ObjectDisposedException.ThrowIf(_this._disposed, this);
            _this._buffer.EnsureCapacity(8, discardOldData: true);
            Memory<byte> header = _this._buffer.GetBuffer()[..8];
            switch (await _this._stream.ReadAtLeastAsync(header, 8, false, _cancellationToken).ConfigureAwait(false))
            {
                case 0:
                    _this._completed = true;
                    return false;
                case 8:
                    uint signature = BinaryPrimitives.ReadUInt32BigEndian(header.Span);
                    uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(header.Span[4..]);
                    if (dataSize > Array.MaxLength)
                        throw new NotSupportedException($"Data size {dataSize} is too large to be processed.");
                    _this._buffer.EnsureCapacity((int)dataSize, discardOldData: true);
                    Memory<byte> memory = _this._buffer.GetBuffer()[..(int)dataSize];
                    if (await _this._stream.ReadAtLeastAsync(memory, (int)dataSize, false).ConfigureAwait(false) < dataSize)
                        goto default;
                    _this.Current = new(signature, memory);
                    return true;
                default:
                    _this._completed = true;
                    throw new EndOfStreamException();
            }
        }
        public void Reset()
            => throw new NotSupportedException();
        public void Dispose()
            => _this.Dispose();
        public ValueTask DisposeAsync()
            => _this.DisposeAsync();
    }
}