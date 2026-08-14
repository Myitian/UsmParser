using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerators;

public struct SequenceUsmChunkEnumerator(ReadOnlySequence<byte> sequence)
    : IUsmChunkEnumerator<SequenceUsmChunk>
{
    private readonly ReadOnlySequence<byte> _originalSequence = sequence;
    private ReadOnlySequence<byte> _sequence = sequence;
    public readonly uint MaxDataLength => uint.MaxValue;
    public SequenceUsmChunk Current { get; private set; }
    readonly object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (_sequence.IsEmpty)
            return false;
        if (_sequence.Length < 8)
        {
            _sequence = default;
            throw new EndOfStreamException();
        }
        Span<byte> header = stackalloc byte[8];
        SequencePosition pos = _sequence.GetPosition(8);
        _sequence.Slice(_sequence.Start, pos).CopyTo(header);
        _sequence = _sequence.Slice(pos);
        uint signature = BinaryPrimitives.ReadUInt32BigEndian(header);
        uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(header[4..]);
        if (_sequence.Length < dataSize)
        {
            _sequence = default;
            throw new EndOfStreamException();
        }
        pos = _sequence.GetPosition(dataSize);
        Current = new(signature, _sequence.Slice(0, pos));
        _sequence = _sequence.Slice(pos);
        return true;
    }
    public void Reset()
        => _sequence = _originalSequence;
    public readonly void Dispose()
    { }
    public readonly SequenceUsmChunkEnumerator GetEnumerator()
        => this;
}