namespace UsmParser.UsmChunkEnumerables;

public interface IUsmChunkEnumeratorInfo
{
    uint InstanceMaxDataLength { get; }
    static abstract uint MaxDataLength { get; }
}