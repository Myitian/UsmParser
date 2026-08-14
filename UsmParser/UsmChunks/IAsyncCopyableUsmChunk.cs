namespace UsmParser.UsmChunks;

public interface IAsyncCopyableUsmChunk
    : IUsmChunk
{
    ValueTask CopyToAsync(Stream destination, CancellationToken cancellationToken = default);
}