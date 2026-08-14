using System.Runtime.InteropServices;

namespace UsmParser.UsmChunks;

#pragma warning disable CA1815
public readonly struct MemoryUsmChunk(uint signature, ReadOnlyMemory<byte> data)
    : IUsmChunk<ReadOnlyMemory<byte>>, IContinuousMemoryUsmChunk, IAsyncCopyableUsmChunk
{
    public uint Signature { get; } = signature;
    public uint DataLength => (uint)Data.Length;
    public ReadOnlyMemory<byte> Data { get; } = data;
    public ref readonly byte Reference => ref MemoryMarshal.GetReference(Data.Span);
    public override string ToString()
        => IUsmChunk.ToString(Signature, DataLength, "memory");
    public SpanUsmChunk AsSpanUsmChunk()
        => new(Signature, Data.Span);
    public SequenceUsmChunk AsSequenceUsmChunk()
        => new(Signature, new(Data));
    public void CopyTo(Span<byte> destination)
    {
        IUsmChunk.ValidateCopyToArgument(in this, destination);
        Data.Span.CopyTo(destination);
    }
    public void CopyTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Write(Data.Span);
    }

    public async ValueTask CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        await destination.WriteAsync(Data, cancellationToken).ConfigureAwait(false);
    }
}