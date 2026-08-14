using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

public interface IAsyncUsmChunkEnumerable<out T>
    : IAsyncUsmChunkEnumerable<T, IAsyncEnumerator<T>>, IAsyncEnumerable<T>
    where T : IUsmChunk, allows ref struct;
public interface IAsyncUsmChunkEnumerable<out T, out TEnumerator>
    : IUsmChunkEnumeratorInfo
    where T : IUsmChunk, allows ref struct
    where TEnumerator : IAsyncEnumerator<T>, allows ref struct
{
    TEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default);
}