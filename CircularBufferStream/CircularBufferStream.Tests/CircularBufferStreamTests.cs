using FluentAssertions;

namespace CircularBufferStream.Tests;

public class StreamCircularBufferTests
{
    // Should build
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void Constructor_ShouldBuild(int capacity, bool canWrite)
    {
        var stream = new CircularBufferStream(capacity: capacity, canWrite: canWrite);
        try
        {
            stream.Size.Should().Be(capacity);
            stream.CanWrite.Should().Be(canWrite);
            stream.Length.Should().Be(0);
            stream.IsFull.Should().BeFalse();
            stream.Available.Should().Be(0);
        }
        finally
        {
            stream.Dispose();
        }
    }

    // Should build with defaults
    [Fact]
    public void Constructor_When_DefaultParametersUsed_ShouldBuild()
    {
        const int capacity = 1;
        var stream = new CircularBufferStream(capacity: capacity);
        try
        {
            stream.Size.Should().Be(capacity);
            stream.CanWrite.Should().BeTrue();
            stream.Length.Should().Be(0);
            stream.IsFull.Should().BeFalse();
            stream.Available.Should().Be(0);
        }
        finally
        {
            stream.Dispose();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_When_CapacityIsInvalid_Then_ThrowsArgumentOutOfRangeException(int capacity)
    {
        var constructor = () => new CircularBufferStream(capacity: capacity);

        constructor.Should().ThrowExactly<ArgumentOutOfRangeException>();
    }

    // Should write
    [Theory]
    [InlineData(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, 0, 4, false, 4, 4, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF })]
    [InlineData(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, 1, 2, false, 2, 2, new byte[] { 0xAD, 0xBE })]
    [InlineData(new byte[] { 0xDE, 0xAD, 0xD0, 0x0D, 0xC0, 0xD3, 0x00 }, 0, 7, true, 5, 2, new byte[] { 0xD3, 0x00, 0xD0, 0x0D, 0xC0 })]
    public void Write_When_GivenBytes_Then_Write(byte[] data, int offset, int count, bool isFull, int expectedLength, int expectedWritePosition, byte[] expected)
    {
        // Arrange
        const int capacity = 5;
        var stream = new CircularBufferStream(capacity: capacity);

        // Act
        stream.Write(data, offset, count);

        try
        {
            stream.Length.Should().Be(expectedLength);
            stream.IsFull.Should().Be(isFull);

            stream.Buffer[..expectedLength].Should().BeEquivalentTo(expected);
            stream.ReadPosition.Should().Be(0); // We didn't 'legally' read from buffer
            stream.WritePosition.Should().Be(expectedWritePosition);
        }
        finally
        {
            stream.Dispose();
        }
    }

    // Should read
    [Theory]
    [InlineData(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, 0, 4, 4, new byte[] { 0xDE, 0xAD, 0xBE, 0xEF })]
    [InlineData(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, 1, 2, 2, new byte[] { 0xDE, 0xAD })]
    [InlineData(new byte[] { 0xDE, 0xAD }, 0, 5, 2, new byte[] { 0xDE, 0xAD })]
    public void Read_When_GivenBuffer_Then_Read(byte[] data, int offset, int count, int expectedReadPosition, byte[] expected)
    {
        // Arrange
        const int capacity = 5;
        var stream = new CircularBufferStream(capacity: capacity);
        stream.Write(data, 0, data.Length);

        // Act
        var buffer = new byte[capacity];
        try
        {
            var read = stream.Read(buffer, offset, count);
            
            // Assert
            read.Should().Be(expected.Length);
            stream.ReadPosition.Should().Be(expectedReadPosition);

            buffer[new Range(offset, offset + expected.Length)].Should().BeEquivalentTo(expected);
        }
        finally
        {
            stream.Dispose();
        }
    }
}