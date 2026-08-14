using UsmParser.Utilities;

namespace UsmParser.UsmChunks;

#pragma warning disable CA1815
public readonly struct StreamUsmChunk
    : IUsmChunk<LengthLimitedStream>, IAsyncCopyableUsmChunk
{
    public uint Signature { get; }
    public uint DataLength { get; }
    public LengthLimitedStream Data { get; }

    public StreamUsmChunk(uint signature, LengthLimitedStream data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Remaining > uint.MaxValue)
            throw new ArgumentException("Data length exceeds maximum value for a USM chunk.", nameof(data));
        Signature = signature;
        DataLength = (uint)data.Remaining;
        Data = data;
    }

    public override string ToString()
        => IUsmChunk.ToString(Signature, DataLength, "stream");
    public void CopyTo(Span<byte> destination)
    {
        IUsmChunk.ValidateCopyToArgument(in this, destination);
        Data.ReadExactly(destination);
    }
    public void CopyTo(Stream destination)
        => Data.CopyTo(destination);
    public async ValueTask CopyToAsync(Stream destination, CancellationToken cancellationToken = default)
        => await Data.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
}