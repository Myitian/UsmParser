using System.Security.Cryptography;
using UsmParser.UsmChunkEnumerators;
using UsmParser.UsmChunks;

while (true)
{
    Console.WriteLine("Input File:");
    string file = Console.ReadLine().AsSpan().Trim().Trim('"').ToString();
    byte[] data = File.ReadAllBytes(file);
    Console.WriteLine("Mode:");
    PrintEnum<Mode>();
    if (!Enum.TryParse(Console.ReadLine(), out Mode m))
        m = Mode.Exit;
    switch (m)
    {
        case Mode.Memory:
            foreach (var chunk in new MemoryUsmChunkEnumerator(data))
                PrintChunkInfo(chunk);
            break;
        case Mode.Span:
            foreach (var chunk in new SpanUsmChunkEnumerator(data))
                PrintChunkInfo(chunk);
            break;
        case Mode.Sequence:
            foreach (var chunk in new SequenceUsmChunkEnumerator(new(data)))
                PrintChunkInfo(chunk);
            break;
        case Mode.Ref:
            foreach (var chunk in new RefUsmChunkEnumerator(in data[0], (uint)data.Length))
                PrintChunkInfo(chunk);
            break;
        case Mode.Array:
            foreach (var chunk in new ArrayUsmChunkEnumerator(new(data)))
                PrintChunkInfo(chunk);
            break;
        case Mode.Stream:
            using (MemoryStream ms = new(data))
            {
                foreach (var chunk in new StreamUsmChunkEnumerator(ms))
                    PrintChunkInfo(chunk);
            }
            break;
        case Mode.LazyStream:
            using (MemoryStream ms = new(data))
            {
                foreach (var chunk in new LazyStreamUsmChunkEnumerator(ms))
                    PrintChunkInfo(chunk);
            }
            break;
        case Mode.AsyncStream:
            using (MemoryStream ms = new(data))
            {
                await foreach (var chunk in new StreamUsmChunkEnumerator(ms))
                    await PrintChunkInfoAsync(chunk);
            }
            break;
        case Mode.AsyncLazyStream:
            using (MemoryStream ms = new(data))
            {
                await foreach (var chunk in new LazyStreamUsmChunkEnumerator(ms))
                    await PrintChunkInfoAsync(chunk);
            }
            break;
        default:
            return;
    }
}
void PrintChunkInfo<T>(T chunk) where T : IUsmChunk, allows ref struct
{
    using SHA256 sha256 = SHA256.Create();
    using (CryptoStream cs = new(Stream.Null, sha256, CryptoStreamMode.Write))
        chunk.CopyTo(cs);
    Console.WriteLine($"""
            {chunk.ToString()}
            Content SHA-256: {Convert.ToHexStringLower(sha256.Hash!)}
            """);
}
async ValueTask PrintChunkInfoAsync<T>(T chunk) where T : IAsyncCopyableUsmChunk
{
    using SHA256 sha256 = SHA256.Create();
    using (CryptoStream cs = new(Stream.Null, sha256, CryptoStreamMode.Write))
        await chunk.CopyToAsync(cs);
    Console.WriteLine($"""
            {chunk.ToString()}
            Content SHA-256: {Convert.ToHexStringLower(sha256.Hash!)}
            """);
}
void PrintEnum<T>() where T : struct, Enum
{
    T[] values = Enum.GetValues<T>();
    Array underlyingValues = Enum.GetValuesAsUnderlyingType<T>();
    for (int i = 0; i < values.Length; i++)
        Console.WriteLine($"[{underlyingValues.GetValue(i)}] {values[i]}");
}
enum Mode
{
    Exit,
    Memory,
    Span,
    Sequence,
    Ref,
    Array,
    Stream,
    LazyStream,
    AsyncStream,
    AsyncLazyStream
}