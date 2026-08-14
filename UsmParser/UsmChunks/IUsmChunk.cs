namespace UsmParser.UsmChunks;

public interface IUsmChunk
{
    uint Signature { get; }
    uint DataLength { get; }
    void CopyTo(scoped Span<byte> destination);
    void CopyTo(Stream destination);
    string ToString();
    public static void ValidateCopyToArgument<T>(scoped T chunk, scoped Span<byte> destination) // for class and small struct types
         where T : IUsmChunk, allows ref struct
    {
        if (destination.Length < chunk.DataLength)
            throw new ArgumentException($"Destination span is too small. Required: {chunk.DataLength}, Actual: {destination.Length}", nameof(destination));
    }
    public static void ValidateCopyToArgument<T>(scoped ref readonly T chunk, scoped Span<byte> destination) // for large struct types
         where T : IUsmChunk, allows ref struct
    {
        if (destination.Length < chunk.DataLength)
            throw new ArgumentException($"Destination span is too small. Required: {chunk.DataLength}, Actual: {destination.Length}", nameof(destination));
    }
    public static string ToString(uint signature, uint dataLength, string type)
    {
        ReadOnlySpan<char> sig = [
            (char)Math.Max(' ', signature >> 24),
            (char)Math.Max(' ', (signature >> 16) & 0xFF),
            (char)Math.Max(' ', (signature >> 8) & 0xFF),
            (char)Math.Max(' ', signature & 0xFF)];
        return $"UsmChunk(0x{signature:X8} \"{sig}\", {dataLength} bytes, {type})";
    }
}
public interface IUsmChunk<T>
    : IUsmChunk
    where T : allows ref struct
{
    T Data { get; }
}