using System.Runtime.InteropServices;

namespace UsmParser.UsmChunks;

public readonly ref struct SpanUsmChunk(uint signature, ReadOnlySpan<byte> data)
    : IUsmChunk<ReadOnlySpan<byte>>, IContinuousMemoryUsmChunk
{
    public uint Signature { get; } = signature;
    public uint DataLength => (uint)Data.Length;
    public ReadOnlySpan<byte> Data { get; } = data;
    public ref readonly byte Reference => ref MemoryMarshal.GetReference(Data);
    public override string ToString()
        => IUsmChunk.ToString(this, "span");
    public void CopyTo(Span<byte> destination)
    {
        IUsmChunk.ValidateCopyToArgument(this, destination);
        Data.CopyTo(destination);
    }
    public void CopyTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Write(Data);
    }
}