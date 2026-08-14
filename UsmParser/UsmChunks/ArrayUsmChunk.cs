using System.Runtime.InteropServices;

namespace UsmParser.UsmChunks;

#pragma warning disable CA1815
public readonly struct ArrayUsmChunk(uint signature, ArraySegment<byte> data)
    : IUsmChunk<ArraySegment<byte>>, IContinuousMemoryUsmChunk, IAsyncCopyableUsmChunk
{
    public uint Signature { get; } = signature;
    public uint DataLength => (uint)Data.Count;
    public ArraySegment<byte> Data { get; } = data;
    public ref readonly byte Reference => ref MemoryMarshal.GetReference(Data.AsSpan());
    public override string ToString()
        => IUsmChunk.ToString(Signature, DataLength, "array");
    public MemoryUsmChunk AsMemoryUsmChunk()
        => new(Signature, Data);
    public SpanUsmChunk AsSpanUsmChunk()
        => new(Signature, Data);
    public RefUsmChunk AsRefUsmChunk()
        => new(Signature, DataLength, in Reference);
    public SequenceUsmChunk AsSequenceUsmChunk()
        => new(Signature, new(Data));
    public void CopyTo(Span<byte> destination)
    {
        IUsmChunk.ValidateCopyToArgument(in this, destination);
        Data.AsSpan().CopyTo(destination);
    }
    public void CopyTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (Data.Array is not null)
            destination.Write(Data.Array, Data.Offset, Data.Count);
    }
    public async ValueTask CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (Data.Array is not null)
            await destination.WriteAsync(Data, cancellationToken).ConfigureAwait(false);
    }
}