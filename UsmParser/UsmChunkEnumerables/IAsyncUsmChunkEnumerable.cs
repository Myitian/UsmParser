using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerators;

public interface IAsyncUsmChunkEnumerable<out T> : IUsmChunkEnumerator, IAsyncEnumerable<T>
    where T : IUsmChunk, allows ref struct;