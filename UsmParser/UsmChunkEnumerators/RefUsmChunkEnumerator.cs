using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerators;

public ref struct RefUsmChunkEnumerator(ref readonly byte reference, nuint length)
    : IUsmChunkEnumerator<RefUsmChunk>
{
    private readonly ref readonly byte _originalReference = ref reference;
    private readonly nuint _originalLength = length;
    private ref readonly byte _reference = ref reference;
    private nuint _length = length;
    public readonly uint MaxDataLength => uint.MaxValue;
    public RefUsmChunk Current { readonly get; private set; }
    readonly object IEnumerator.Current => throw new NotSupportedException();

    public bool MoveNext()
    {
        if (_length == 0)
            return false;
        if (_length < 8)
        {
            _reference = ref Unsafe.NullRef<byte>();
            _length = 0;
            throw new EndOfStreamException();
        }
        ref readonly byte reference = ref _reference;
        uint signature = ReadUInt32BigEndian(in reference);
        reference = ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in reference), 4);
        uint dataSize = ReadUInt32BigEndian(in reference);
        _length -= 8;
        if (dataSize > _length)
        {
            _reference = ref Unsafe.NullRef<byte>();
            _length = 0;
            throw new EndOfStreamException();
        }
        reference = ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in reference), 4);
        Current = new(signature, dataSize, in reference);
        _length -= dataSize;
        _reference = ref reference;
        return true;
    }
    public void Reset()
    {
        _reference = ref _originalReference;
        _length = _originalLength;
    }
    public readonly void Dispose()
    { }
    private static uint ReadUInt32BigEndian(scoped ref readonly byte pointer)
    {
        return BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(in pointer))
            : Unsafe.ReadUnaligned<uint>(in pointer);
    }
    public readonly RefUsmChunkEnumerator GetEnumerator()
        => this;
}