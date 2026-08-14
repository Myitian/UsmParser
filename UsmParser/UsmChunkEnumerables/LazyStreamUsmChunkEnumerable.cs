using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

public sealed class LazyStreamUsmChunkEnumerable
    : IUsmChunkEnumerable<StreamUsmChunk, LazyStreamUsmChunkEnumerable.Enumerator>, IUsmChunkEnumerable<StreamUsmChunk>,
    IAsyncUsmChunkEnumerable<StreamUsmChunk, LazyStreamUsmChunkEnumerable.Enumerator>, IAsyncUsmChunkEnumerable<StreamUsmChunk>,
    IDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private bool _isConsumed;
    private bool _disposed;
    public LazyStreamUsmChunkEnumerable(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _stream = stream;
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
        return new(_stream, _leaveOpen);
    }
    IEnumerator<StreamUsmChunk> IUsmChunkEnumerable<StreamUsmChunk, IEnumerator<StreamUsmChunk>>.GetEnumerator()
        => GetEnumerator();
    IEnumerator<StreamUsmChunk> IEnumerable<StreamUsmChunk>.GetEnumerator()
        => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
    public Enumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_isConsumed)
            throw new InvalidOperationException("The enumerable has already been consumed.");
        _isConsumed = true;
        return new(_stream, _leaveOpen, cancellationToken);
    }
    IAsyncEnumerator<StreamUsmChunk> IAsyncUsmChunkEnumerable<StreamUsmChunk, IAsyncEnumerator<StreamUsmChunk>>.GetAsyncEnumerator(CancellationToken cancellationToken)
       => GetAsyncEnumerator(cancellationToken);
    IAsyncEnumerator<StreamUsmChunk> IAsyncEnumerable<StreamUsmChunk>.GetAsyncEnumerator(CancellationToken cancellationToken)
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
        : IUsmChunkEnumerator<StreamUsmChunk>, IAsyncUsmChunkEnumerator<StreamUsmChunk>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly Stream _stream;
        private byte[]? _buffer;
        private readonly bool _leaveOpen;
        private bool _first = true;
        private bool _completed;
        private bool _disposed;

        internal Enumerator(Stream stream, bool leaveOpen = false, CancellationToken cancellationToken = default)
        {
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
            switch (await _stream.ReadAtLeastAsync(_buffer ??= new byte[8], 8, false, _cancellationToken).ConfigureAwait(false))
            {
                case 0:
                    _completed = true;
                    return false;
                case 8:
                    uint signature = BinaryPrimitives.ReadUInt32BigEndian(_buffer);
                    uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(4));
                    if (!_first)
                    {
                        await Current.Data.CopyToAsync(Stream.Null, _cancellationToken).ConfigureAwait(false);
                    }
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
    }
}