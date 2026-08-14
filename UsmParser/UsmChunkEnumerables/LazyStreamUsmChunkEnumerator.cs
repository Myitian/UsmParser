using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerators;

public sealed class LazyStreamUsmChunkEnumerator
    : IUsmChunkEnumerator<StreamUsmChunk>, IAsyncUsmChunkEnumerable<StreamUsmChunk>
{
    private readonly CancellationToken _cancellationToken;
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private bool _first = true;
    private bool _completed;
    private bool _disposed;

    public LazyStreamUsmChunkEnumerator(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _cancellationToken = cancellationToken;
        _stream = stream;
        _leaveOpen = leaveOpen;
    }

    public uint InstanceMaxDataLength => (uint)Array.MaxLength;
    public StreamUsmChunk Current { get; private set; }
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
                if (!_first)
                    Current.Data.CopyTo(Stream.Null);
                _first = false;
#pragma warning disable CA2000
                Current = new(signature, new(_stream, dataSize, true));
#pragma warning restore CA2000
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
            if (!_leaveOpen)
                return _stream.DisposeAsync();
        }
        return ValueTask.CompletedTask;
    }
    public LazyStreamUsmChunkEnumerator GetEnumerator()
        => this;
    public IAsyncEnumerator<StreamUsmChunk> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new AsyncEnumerator(this, cancellationToken);
    sealed class AsyncEnumerator(LazyStreamUsmChunkEnumerator @this, CancellationToken cancellationToken)
        : IAsyncEnumerator<StreamUsmChunk>
    {
        private readonly LazyStreamUsmChunkEnumerator _this = @this;
        private readonly CancellationToken _cancellationToken = cancellationToken;
        private byte[]? _buffer;
        public StreamUsmChunk Current => _this.Current;
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
            switch (await _this._stream.ReadAtLeastAsync(_buffer ??= new byte[8], 8, false, _cancellationToken).ConfigureAwait(false))
            {
                case 0:
                    _this._completed = true;
                    return false;
                case 8:
                    uint signature = BinaryPrimitives.ReadUInt32BigEndian(_buffer);
                    uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(4));
                    if (!_this._first)
                    {
                        await Current.Data.CopyToAsync(Stream.Null, _cancellationToken).ConfigureAwait(false);
                    }
                    _this._first = false;
#pragma warning disable CA2000
                    _this.Current = new(signature, new(_this._stream, dataSize, true));
#pragma warning restore CA2000
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