using System.Security.Cryptography;
using UsmParser.UsmChunkEnumerables;
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
            Test<MemoryUsmChunkEnumerable, MemoryUsmChunkEnumerable.Enumerator, MemoryUsmChunk>(new(data));
            break;
        case Mode.Span:
            Test<SpanUsmChunkEnumerable, SpanUsmChunkEnumerable.Enumerator, SpanUsmChunk>(new(data));
            break;
        case Mode.Sequence:
            Test<SequenceUsmChunkEnumerable, SequenceUsmChunkEnumerable.Enumerator, SequenceUsmChunk>(new(new(data)));
            break;
        case Mode.Ref:
            Test<RefUsmChunkEnumerable, RefUsmChunkEnumerable.Enumerator, RefUsmChunk>(new(in data[0], (uint)data.Length));
            break;
        case Mode.Array:
            Test<ArrayUsmChunkEnumerable, ArrayUsmChunkEnumerable.Enumerator, ArrayUsmChunk>(new(new(data)));
            break;
        case Mode.Stream:
            using (MemoryStream ms = new(data))
                Test<StreamUsmChunkEnumerable, StreamUsmChunkEnumerable.Enumerator, MemoryUsmChunk>(new(ms));
            break;
        case Mode.LazyStream:
            using (MemoryStream ms = new(data))
                Test<LazyStreamUsmChunkEnumerable, LazyStreamUsmChunkEnumerable.Enumerator, StreamUsmChunk>(new(ms));
            break;
        case Mode.AsyncStream:
            using (MemoryStream ms = new(data))
                await TestAsync<StreamUsmChunkEnumerable, StreamUsmChunkEnumerable.Enumerator, MemoryUsmChunk>(new(ms));
            break;
        case Mode.AsyncLazyStream:
            using (MemoryStream ms = new(data))
                await TestAsync<LazyStreamUsmChunkEnumerable, LazyStreamUsmChunkEnumerable.Enumerator, StreamUsmChunk>(new(ms));
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
async Task PrintChunkInfoAsync<T>(T chunk) where T : IAsyncCopyableUsmChunk
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
void Test<TEnumrable, TEnumerator, TChunk>(scoped TEnumrable enumerable)
    where TEnumrable : IUsmChunkEnumerable<TChunk, TEnumerator>, allows ref struct
    where TEnumerator : IEnumerator<TChunk>, allows ref struct
    where TChunk : IUsmChunk, allows ref struct
{
    foreach (TChunk chunk in enumerable)
        PrintChunkInfo(chunk);
}
async Task TestAsync<TEnumrable, TEnumerator, TChunk>(TEnumrable enumerable)
    where TEnumrable : IAsyncUsmChunkEnumerable<TChunk, TEnumerator>
    where TEnumerator : IAsyncEnumerator<TChunk>
    where TChunk : IAsyncCopyableUsmChunk
{
    await foreach (TChunk chunk in enumerable)
        await PrintChunkInfoAsync(chunk);
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