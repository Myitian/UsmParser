using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

public interface IAsyncUsmChunkEnumerator<out T>
    : IUsmChunkEnumeratorInfo, IAsyncEnumerator<T>
    where T : IUsmChunk, allows ref struct;