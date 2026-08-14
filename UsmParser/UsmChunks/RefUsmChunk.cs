using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UsmParser.UsmChunks;

public readonly ref struct RefUsmChunk(uint signature, uint dataLength, ref readonly byte reference)
    : IUsmChunk<ReadOnlyRef<byte>>, IContinuousMemoryUsmChunk
{
    private readonly ref readonly byte _data = ref reference;
    public uint Signature { get; } = signature;
    public uint DataLength { get; } = dataLength;
    public ReadOnlyRef<byte> Data => new(in _data);
    public ref readonly byte Reference => ref _data;
    public override string ToString()
        => IUsmChunk.ToString(Signature, DataLength, "ref");
    public void CopyTo(Span<byte> destination)
    {
        IUsmChunk.ValidateCopyToArgument(in this, destination);
        MemoryMarshal.CreateReadOnlySpan(in _data, (int)DataLength).CopyTo(destination);
    }
    public void CopyTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        uint length = DataLength;
        ref readonly byte data = ref _data;
        while (length > 0)
        {
            uint chunkSize = Math.Min(length, int.MaxValue);
            destination.Write(MemoryMarshal.CreateReadOnlySpan(in data, (int)chunkSize));
            length -= chunkSize;
            data = ref Unsafe.AddByteOffset(ref Unsafe.AsRef(in data), chunkSize);
        }
    }
}
public readonly ref struct ReadOnlyRef<T>(ref readonly T value)
{
    private readonly ref readonly T _value = ref value;
    public ref readonly T Value => ref _value;
}