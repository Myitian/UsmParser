using System.Buffers;

namespace UsmParser.UsmChunks;

#pragma warning disable CA1815
public readonly struct SequenceUsmChunk : IUsmChunk<ReadOnlySequence<byte>>, IAsyncCopyableUsmChunk
{
    public uint Signature { get; }
    public uint DataLength { get; }
    public ReadOnlySequence<byte> Data { get; }

    public SequenceUsmChunk(uint signature, ReadOnlySequence<byte> data)
    {
        if (data.Length > uint.MaxValue)
            throw new ArgumentException("Data length exceeds maximum value for a USM chunk.", nameof(data));
        Signature = signature;
        DataLength = (uint)data.Length;
        Data = data;
    }

    public override string ToString()
        => IUsmChunk.ToString(this, "sequence");
    public void CopyTo(Span<byte> destination)
    {
        IUsmChunk.ValidateCopyToArgument(this, destination);
        Data.CopyTo(destination);
    }
    public void CopyTo(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        foreach (ReadOnlyMemory<byte> segment in Data)
            destination.Write(segment.Span);
    }
    public async ValueTask CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        foreach (ReadOnlyMemory<byte> segment in Data)
            await destination.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
    }
}