using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

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
            => throw new NotSupportedException();
        public readonly void Dispose()
        { }
    }
}