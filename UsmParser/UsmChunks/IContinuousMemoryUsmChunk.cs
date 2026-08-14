namespace UsmParser.UsmChunks;

public interface IContinuousMemoryUsmChunk
    : IUsmChunk
{
    ref readonly byte Reference { get; }
}