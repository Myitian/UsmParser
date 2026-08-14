using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerators;

public interface IUsmChunkEnumerator
{
    uint MaxDataLength { get; }
}
public interface IUsmChunkEnumerator<out T> : IUsmChunkEnumerator, IEnumerator<T>
    where T : IUsmChunk, allows ref struct;
public interface IAsyncUsmChunkEnumerable<out T> : IUsmChunkEnumerator, IAsyncEnumerable<T>
    where T : IUsmChunk, allows ref struct;