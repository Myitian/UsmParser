using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

#pragma warning disable CA1815
public readonly struct MemoryUsmChunkEnumerable(ReadOnlyMemory<byte> memory)
    : IUsmChunkEnumerable<MemoryUsmChunk, MemoryUsmChunkEnumerable.Enumerator>, IUsmChunkEnumerable<MemoryUsmChunk>
{
    private readonly ReadOnlyMemory<byte> _memory = memory;
    public uint InstanceMaxDataLength => (uint)Math.Max(0, _memory.Length - 8);
    public static uint MaxDataLength => int.MaxValue;
    public Enumerator GetEnumerator()
        => new(_memory);
    IEnumerator<MemoryUsmChunk> IUsmChunkEnumerable<MemoryUsmChunk, IEnumerator<MemoryUsmChunk>>.GetEnumerator()
        => GetEnumerator();
    IEnumerator<MemoryUsmChunk> IEnumerable<MemoryUsmChunk>.GetEnumerator()
        => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public struct Enumerator(ReadOnlyMemory<byte> memory)
        : IUsmChunkEnumerator<MemoryUsmChunk>
    {
        private ReadOnlyMemory<byte> _memory = memory;
        public readonly uint InstanceMaxDataLength => (uint)Math.Max(0, _memory.Length - 8);
        public MemoryUsmChunk Current { readonly get; private set; }
        readonly object IEnumerator.Current => Current;
        public static uint MaxDataLength => int.MaxValue;

        public bool MoveNext()
        {
            ReadOnlyMemory<byte> memory = _memory;
            if (memory.IsEmpty)
                return false;
            if (memory.Length < 8)
            {
                _memory = default;
                throw new EndOfStreamException();
            }
            uint signature = BinaryPrimitives.ReadUInt32BigEndian(memory.Span);
            uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(memory.Span[4..]);
            if (dataSize > int.MaxValue)
            {
                memory = default;
                throw new NotSupportedException($"Data size {dataSize} is too large to be processed.");
            }
            memory = memory[8..];
            if ((uint)memory.Length < dataSize)
            {
                _memory = default;
                throw new EndOfStreamException();
            }
            Current = new(signature, memory[..(int)dataSize]);
            _memory = memory[(int)dataSize..];
            return true;
        }
        public void Reset()
            => throw new NotSupportedException();
        public readonly void Dispose()
        { }
    }
}