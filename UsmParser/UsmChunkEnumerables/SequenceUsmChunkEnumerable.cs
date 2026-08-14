using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

#pragma warning disable CA1815
public readonly struct SequenceUsmChunkEnumerable(ReadOnlySequence<byte> sequence)
    : IUsmChunkEnumerable<SequenceUsmChunk, SequenceUsmChunkEnumerable.Enumerator>, IUsmChunkEnumerable<SequenceUsmChunk>
{
    private readonly ReadOnlySequence<byte> _sequence = sequence;
    public uint InstanceMaxDataLength => (uint)Math.Min(uint.MaxValue, _sequence.Length);
    public static uint MaxDataLength => uint.MaxValue;
    public Enumerator GetEnumerator()
        => new(_sequence);
    IEnumerator<SequenceUsmChunk> IUsmChunkEnumerable<SequenceUsmChunk, IEnumerator<SequenceUsmChunk>>.GetEnumerator()
        => GetEnumerator();
    IEnumerator<SequenceUsmChunk> IEnumerable<SequenceUsmChunk>.GetEnumerator()
        => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct Enumerator(ReadOnlySequence<byte> sequence)
        : IUsmChunkEnumerator<SequenceUsmChunk>
    {
        private ReadOnlySequence<byte> _sequence = sequence;
        public readonly uint InstanceMaxDataLength => (uint)Math.Min(uint.MaxValue, _sequence.Length);
        public SequenceUsmChunk Current { get; private set; }
        readonly object IEnumerator.Current => Current;
        public static uint MaxDataLength => uint.MaxValue;

        public bool MoveNext()
        {
            ReadOnlySequence<byte> sequence = _sequence;
            if (sequence.IsEmpty)
                return false;
            if (sequence.Length < 8)
            {
                _sequence = default;
                throw new EndOfStreamException();
            }
            Span<byte> header = stackalloc byte[8];
            SequencePosition pos = sequence.GetPosition(8);
            sequence.Slice(sequence.Start, pos).CopyTo(header);
            sequence = sequence.Slice(pos);
            uint signature = BinaryPrimitives.ReadUInt32BigEndian(header);
            uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(header[4..]);
            if (sequence.Length < dataSize)
            {
                _sequence = default;
                throw new EndOfStreamException();
            }
            pos = sequence.GetPosition(dataSize);
            Current = new(signature, sequence.Slice(0, pos));
            _sequence = sequence.Slice(pos);
            return true;
        }
        public void Reset()
            => throw new NotSupportedException();
        public readonly void Dispose()
        { }
    }
}