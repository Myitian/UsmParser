using System.Buffers.Binary;
using System.Collections;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerators;

public struct MemoryUsmChunkEnumerator(ReadOnlyMemory<byte> memory)
    : IUsmChunkEnumerator<MemoryUsmChunk>
{
    private readonly ReadOnlyMemory<byte> _originalMemory = memory;
    private ReadOnlyMemory<byte> _memory = memory;
    public readonly uint InstanceMaxDataLength => (uint)_memory.Length;
    public MemoryUsmChunk Current { readonly get; private set; }
    readonly object IEnumerator.Current => Current;
    public static uint MaxDataLength => int.MaxValue;

    public bool MoveNext()
    {
        if (_memory.IsEmpty)
            return false;
        if (_memory.Length < 8)
        {
            _memory = default;
            throw new EndOfStreamException();
        }
        uint signature = BinaryPrimitives.ReadUInt32BigEndian(_memory.Span);
        uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(_memory.Span[4..]);
        if (dataSize > int.MaxValue)
        {
            _memory = default;
            throw new NotSupportedException($"Data size {dataSize} is too large to be processed.");
        }
        _memory = _memory[8..];
        if ((uint)_memory.Length < dataSize)
        {
            _memory = default;
            throw new EndOfStreamException();
        }
        Current = new(signature, _memory[..(int)dataSize]);
        _memory = _memory[(int)dataSize..];
        return true;
    }
    public void Reset()
        => _memory = _originalMemory;
    public readonly void Dispose()
    { }
    public readonly MemoryUsmChunkEnumerator GetEnumerator()
        => this;
}