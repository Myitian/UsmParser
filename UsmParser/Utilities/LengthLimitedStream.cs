namespace UsmParser.Utilities;

public sealed class LengthLimitedStream(Stream stream, long length, bool leaveOpen = true)
    : Stream
{
    private readonly bool _leaveOpen = leaveOpen;
    public Stream BaseStream { get; } = stream;
    public long Remaining { get; private set; } = length;
    public override bool CanRead => BaseStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Close()
    {
        if (!_leaveOpen)
            BaseStream.Close();
    }
    public override void Flush()
    {
        BaseStream.Flush();
    }
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return BaseStream.FlushAsync(cancellationToken);
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }
    public override int Read(Span<byte> buffer)
    {
        long c = Math.Min(buffer.Length, Remaining);
        if (c <= 0)
            return 0;
        int result = BaseStream.Read(buffer[..(int)c]);
        Remaining -= result;
        return result;
    }
    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        long c = Math.Min(buffer.Length, Remaining);
        if (c <= 0)
            return 0;
        int result = await BaseStream.ReadAsync(buffer[..(int)c], cancellationToken).ConfigureAwait(false);
        Remaining -= result;
        return result;
    }
    public override int ReadByte()
    {
        if (Remaining <= 0)
            return -1;
        int result = BaseStream.ReadByte();
        if (result >= 0)
            Remaining--;
        return result;
    }
    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();
    public override void SetLength(long value)
        => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();
    public override void WriteByte(byte value)
        => throw new NotSupportedException();
}