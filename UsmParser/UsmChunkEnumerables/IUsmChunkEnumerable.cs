using UsmParser.UsmChunks;

namespace UsmParser.UsmChunkEnumerables;

public interface IUsmChunkEnumerable<out T>
    : IUsmChunkEnumerable<T, IEnumerator<T>>, IEnumerable<T>
    where T : IUsmChunk, allows ref struct;
public interface IUsmChunkEnumerable<out T, out TEnumerator>
    : IUsmChunkEnumeratorInfo
    where T : IUsmChunk, allows ref struct
    where TEnumerator : IEnumerator<T>, allows ref struct
{
    TEnumerator GetEnumerator();
}