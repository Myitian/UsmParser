using System.Buffers.Binary;
using System.Collections;
using System.Runtime.CompilerServices;
using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

public readonly ref struct RefUsmChunkEnumerable(ref readonly byte reference, nuint length)
    : IUsmChunkEnumerable<RefUsmChunk, RefUsmChunkEnumerable.Enumerator>
{
    private readonly ref readonly byte _reference = ref reference;
    private readonly nuint _length = length;
    public uint InstanceMaxDataLength => _length >= 8 ? (uint)Math.Min(uint.MaxValue, _length - 8) : 0;
    public static uint MaxDataLength => uint.MaxValue;
    public Enumerator GetEnumerator()
        => new(in _reference, _length);

    public ref struct Enumerator(ref readonly byte reference, nuint length)
        : IUsmChunkEnumerator<RefUsmChunk>
    {
        private ref readonly byte _reference = ref reference;
        private nuint _length = length;
        public readonly uint InstanceMaxDataLength => _length >= 8 ? (uint)Math.Min(uint.MaxValue, _length - 8) : 0;
        public RefUsmChunk Current { readonly get; private set; }
        readonly object IEnumerator.Current => throw new NotSupportedException();
        public static uint MaxDataLength => uint.MaxValue;

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
            => throw new NotSupportedException();
        public readonly void Dispose()
        { }
        private static uint ReadUInt32BigEndian(scoped ref readonly byte pointer)
        {
            return BitConverter.IsLittleEndian
                ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(in pointer))
                : Unsafe.ReadUnaligned<uint>(in pointer);
        }
    }
}