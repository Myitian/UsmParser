using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

#pragma warning disable CA1815
public readonly struct ArrayUsmChunkEnumerable(ArraySegment<byte> segment)
    : IUsmChunkEnumerable<ArrayUsmChunk, ArrayUsmChunkEnumerable.Enumerator>, IUsmChunkEnumerable<ArrayUsmChunk>
{
    private readonly ArraySegment<byte> _segment = segment;
    public readonly uint InstanceMaxDataLength => (uint)Math.Max(0, _segment.Count - 8);
    public static uint MaxDataLength => (uint)Array.MaxLength;
    public Enumerator GetEnumerator()
        => new(_segment);
    IEnumerator<ArrayUsmChunk> IUsmChunkEnumerable<ArrayUsmChunk, IEnumerator<ArrayUsmChunk>>.GetEnumerator()
        => GetEnumerator();
    IEnumerator<ArrayUsmChunk> IEnumerable<ArrayUsmChunk>.GetEnumerator()
        => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct Enumerator(ArraySegment<byte> segment)
        : IUsmChunkEnumerator<ArrayUsmChunk>
    {
        private ArraySegment<byte> _segment = segment;
        public readonly uint InstanceMaxDataLength => (uint)Math.Max(0, _segment.Count - 8);
        public ArrayUsmChunk Current { readonly get; private set; }
        readonly object IEnumerator.Current => Current;
        public static uint MaxDataLength => (uint)Array.MaxLength;

        public bool MoveNext()
        {
            if (_segment.Count == 0)
                return false;
            if (_segment.Count < 8)
            {
                _segment = default;
                throw new EndOfStreamException();
            }
            uint signature = BinaryPrimitives.ReadUInt32BigEndian(_segment);
            uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(_segment[4..]);
            if (dataSize > int.MaxValue)
            {
                _segment = default;
                throw new NotSupportedException($"Data size {dataSize} is too large to be processed.");
            }
            _segment = _segment[8..];
            if ((uint)_segment.Count < dataSize)
            {
                _segment = default;
                throw new EndOfStreamException();
            }
            Current = new(signature, _segment[..(int)dataSize]);
            _segment = _segment[(int)dataSize..];
            return true;
        }
        public void Reset()
            => throw new NotSupportedException();
        public readonly void Dispose()
        { }
    }
}