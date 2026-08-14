using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerators;

public struct ArrayUsmChunkEnumerator(ArraySegment<byte> segment)
    : IUsmChunkEnumerator<ArrayUsmChunk>
{
    private readonly ArraySegment<byte> _originalSegment = segment;
    private ArraySegment<byte> _segment = segment;
    public readonly uint InstanceMaxDataLength => (uint)_segment.Count;
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
        => _segment = _originalSegment;
    public readonly void Dispose()
    { }
    public readonly ArrayUsmChunkEnumerator GetEnumerator()
        => this;
}