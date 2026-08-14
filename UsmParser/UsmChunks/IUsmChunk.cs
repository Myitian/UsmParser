namespace UsmParser.UsmChunks;

public interface IUsmChunk
{
    uint Signature { get; }
    uint DataLength { get; }
    void CopyTo(scoped Span<byte> destination);
    void CopyTo(Stream destination);
    string ToString();
    public static string ToString<T>(scoped T chunk, string type)
        where T : IUsmChunk, allows ref struct
    {
        ReadOnlySpan<char> sig = [
            (char)Math.Max(' ', chunk.Signature >> 24),
            (char)Math.Max(' ', (chunk.Signature >> 16) & 0xFF),
            (char)Math.Max(' ', (chunk.Signature >> 8) & 0xFF),
            (char)Math.Max(' ', chunk.Signature & 0xFF)];
        return $"UsmChunk(0x{chunk.Signature:X8} \"{sig}\", {chunk.DataLength} bytes, {type})";
    }
    public static void ValidateCopyToArgument<T>(scoped T chunk, Span<byte> destination)
         where T : IUsmChunk, allows ref struct
    {
        if (destination.Length < chunk.DataLength)
            throw new ArgumentException($"Destination span is too small. Required: {chunk.DataLength}, Actual: {destination.Length}", nameof(destination));
    }
}
public interface IUsmChunk<T>
    : IUsmChunk
    where T : allows ref struct
{
    T Data { get; }
}