using System.Buffers;

namespace CircularBufferStream;

public class CircularBufferStream : Stream
{
    public CircularBufferStream(int capacity) : this(capacity, true) { }
    public CircularBufferStream(int capacity, bool canWrite)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Invalid negative or empty capacity");
        }

        Buffer = ArrayPool<byte>.Shared.Rent(capacity);
        Size = capacity;
        CanWrite = canWrite;
        ReadPosition = WritePosition = 0;
        _length = 0;
    }

    internal readonly byte[] Buffer;
    private readonly object _bufferLock = new object();
    internal long ReadPosition;
    internal long WritePosition;
    private long _length;

    public override bool CanRead => true;
    public override bool CanWrite { get; }
    public override bool CanSeek => false;
    public override long Position { get; set; }
    public override long Length => _length;
    public long Size { get; }
    public bool IsFull => Size == Length;
    public long Available => GetAvailableRead();
    private bool IsEmpty => Size == 0;

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        long read;
        lock (_bufferLock)
        {
            read = Math.Min(count, Available);
            for (var i = offset; i < offset + read; i++)
            {
                var item = PopFront();
                buffer[i] = item;
            }
        }

        return (int)read;
    }

    private long GetAvailableRead()
    {
        if (ReadPosition == WritePosition)
        {
            return 0;
        }

        var available = WritePosition - ReadPosition;
        if (ReadPosition > WritePosition)
        {
            available = Length - ReadPosition;
            available += WritePosition;
        }

        return available;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException("Cannot seek");
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("Cannot change length of a fixed size ring buffer");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!CanWrite)
        {
            throw new InvalidOperationException("Cannot write on a non writable stream");
        }

        lock (_bufferLock)
        {
            for (var i = offset; i < offset + count; i++)
            {
                PushBack(buffer[i]);
            }
        }
    }

    private void PushBack(byte item)
    {
        Buffer[WritePosition] = item;
        Increment(ref WritePosition);

        if (!IsFull)
        {
            _length++;
        }
    }

    private byte PopFront()
    {
        ThrowIfEmpty("Cannot take elements from an empty buffer.");
        var b = Buffer[ReadPosition];
        Increment(ref ReadPosition);

        return b;
    }

    private void Increment(ref long index)
    {
        if (++index == Size)
        {
            index = 0;
        }
    }

    private void ThrowIfEmpty(string message = "Cannot access an empty buffer.")
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException(message);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_bufferLock)
            {
                ArrayPool<byte>.Shared.Return(Buffer);
            }
        }

        base.Dispose(disposing);
    }
}
