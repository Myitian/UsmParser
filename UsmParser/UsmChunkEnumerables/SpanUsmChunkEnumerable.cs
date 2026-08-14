using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

public readonly ref struct SpanUsmChunkEnumerable(ReadOnlySpan<byte> span)
    : IUsmChunkEnumerable<SpanUsmChunk, SpanUsmChunkEnumerable.Enumerator>
{
    private readonly ReadOnlySpan<byte> _span = span;
    public uint InstanceMaxDataLength => (uint)Math.Max(0, _span.Length - 8);
    public static uint MaxDataLength => int.MaxValue;
    public Enumerator GetEnumerator()
        => new(_span);

    public ref struct Enumerator(ReadOnlySpan<byte> span)
        : IUsmChunkEnumerator<SpanUsmChunk>
    {
        private ReadOnlySpan<byte> _span = span;
        public readonly uint InstanceMaxDataLength => (uint)Math.Max(0, _span.Length - 8);
        public SpanUsmChunk Current { readonly get; private set; }
        readonly object IEnumerator.Current => throw new NotSupportedException();
        public static uint MaxDataLength => int.MaxValue;

        public bool MoveNext()
        {
            if (_span.IsEmpty)
                return false;
            if (_span.Length < 8)
            {
                _span = default;
                throw new EndOfStreamException();
            }
            uint signature = BinaryPrimitives.ReadUInt32BigEndian(_span);
            uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(_span[4..]);
            if (dataSize > int.MaxValue)
            {
                _span = default;
                throw new NotSupportedException($"Data size {dataSize} is too large to be processed.");
            }
            _span = _span[8..];
            if ((uint)_span.Length < dataSize)
            {
                _span = default;
                throw new EndOfStreamException();
            }
            Current = new(signature, _span[..(int)dataSize]);
            _span = _span[(int)dataSize..];
            return true;
        }
        public void Reset()
            => throw new NotSupportedException();
        public readonly void Dispose()
        { }
    }
}