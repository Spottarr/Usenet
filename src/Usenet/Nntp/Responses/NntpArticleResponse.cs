using System.Buffers;
using System.Text;
using JetBrains.Annotations;
using Usenet.Nntp.Models;
using Usenet.Util;

namespace Usenet.Nntp.Responses;

/// <summary>
/// Represents a response to the
/// <a href="https://tools.ietf.org/html/rfc3977#section-6.2.1">ARTICLE</a>,
/// <a href="https://tools.ietf.org/html/rfc3977#section-6.2.2">HEAD</a> and
/// <a href="https://tools.ietf.org/html/rfc3977#section-6.2.3">BODY</a> commands.
/// </summary>
/// <remarks>
/// The article is buffered whole (one article, bounded around 1 MB) into a single contiguous
/// buffer rented from <see cref="ArrayPool{T}"/>. The response owns that buffer: the <see cref="Body"/>
/// view and the lines returned by <see cref="ReadBodyLines()"/> are only valid until the response is
/// disposed, after which the buffer is returned to the pool. Always dispose the response (preferably
/// with <c>using</c>/<c>await using</c>); a forgotten response is recovered by a finalizer that returns
/// the buffer and increments <see cref="LeakedBufferCount"/>.
/// </remarks>
[PublicAPI]
public sealed class NntpArticleResponse : NntpResponse, IDisposable, IAsyncDisposable
{
    private static long _leakedBufferCount;

    /// <summary>
    /// The number of pooled buffers reclaimed by the finalizer because a response was not disposed.
    /// A non-zero value is a diagnostic signal that a caller forgot to dispose an article response.
    /// </summary>
    public static long LeakedBufferCount => Interlocked.Read(ref _leakedBufferCount);

    private byte[]? _buffer;
    private readonly int _bodyOffset;
    private readonly int _length;
    private bool _disposed;

    /// <summary>
    /// The number of the article in the currently selected newsgroup.
    /// </summary>
    public long Number { get; }

    /// <summary>
    /// The message-id of the article.
    /// </summary>
    public NntpMessageId MessageId { get; }

    /// <summary>
    /// The NNTP newsgroups the article is posted in.
    /// </summary>
    public NntpGroups Groups { get; }

    /// <summary>
    /// The headers of the article. Empty for a <c>BODY</c> response or a failure response.
    /// </summary>
    public NntpHeaderCollection Headers { get; }

    /// <summary>
    /// The article body as the raw bytes transmitted by the server, with dot-stuffing undone and the
    /// terminating dot removed. The memory is backed by a pooled buffer and is only valid until this
    /// response is disposed. Empty for a <c>HEAD</c> response or a failure response.
    /// </summary>
    public ReadOnlyMemory<byte> Body
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _buffer is null
                ? ReadOnlyMemory<byte>.Empty
                : _buffer.AsMemory(_bodyOffset, _length - _bodyOffset);
        }
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NntpArticleResponse"/> class for a failure response.
    /// </summary>
    internal NntpArticleResponse(int code, string message, bool success)
        : base(code, message, success)
    {
        Headers = NntpHeaderCollection.Empty;
        MessageId = NntpMessageId.Empty;
        Groups = NntpGroups.Empty;

        // A failure response owns no pooled buffer, so it never needs finalization.
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NntpArticleResponse"/> class for a successful response,
    /// taking ownership of the pooled <paramref name="buffer"/>.
    /// </summary>
    internal NntpArticleResponse(
        int code,
        string message,
        long number,
        NntpMessageId messageId,
        NntpGroups groups,
        NntpHeaderCollection headers,
        byte[] buffer,
        int bodyOffset,
        int length
    )
        : base(code, message, true)
    {
        Number = number;
        MessageId = messageId;
        Groups = groups;
        Headers = headers;
        _buffer = buffer;
        _bodyOffset = bodyOffset;
        _length = length;
    }

    /// <summary>
    /// Reads the article body as text lines on demand, using the default Usenet character encoding.
    /// Each line is decoded from the pooled buffer when enumerated; the result is only valid until
    /// this response is disposed.
    /// </summary>
    /// <returns>The body lines, with their CRLF terminators stripped.</returns>
    public IEnumerable<string> ReadBodyLines() => ReadBodyLines(UsenetEncoding.Default);

    /// <summary>
    /// Reads the article body as text lines on demand, using the specified character encoding.
    /// Each line is decoded from the pooled buffer when enumerated; the result is only valid until
    /// this response is disposed.
    /// </summary>
    /// <param name="encoding">The character encoding used to decode the body lines.</param>
    /// <returns>The body lines, with their CRLF terminators stripped.</returns>
    public IEnumerable<string> ReadBodyLines(Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        ObjectDisposedException.ThrowIf(_disposed, this);

        var buffer = _buffer;
        return buffer is null ? [] : EnumerateLines(buffer, _bodyOffset, _length, encoding);
    }

    private static IEnumerable<string> EnumerateLines(
        byte[] buffer,
        int offset,
        int length,
        Encoding encoding
    )
    {
        var position = offset;
        while (position < length)
        {
            var newline = Array.IndexOf(buffer, (byte)'\n', position, length - position);
            var lineEnd = newline < 0 ? length : newline;
            var next = newline < 0 ? length : newline + 1;

            var contentEnd = lineEnd;
            if (contentEnd > position && buffer[contentEnd - 1] == (byte)'\r')
            {
                contentEnd--;
            }

            yield return encoding.GetString(buffer, position, contentEnd - position);
            position = next;
        }
    }

    /// <summary>
    /// Returns the pooled buffer to the <see cref="ArrayPool{T}"/> it was rented from. After disposal
    /// the <see cref="Body"/> view and any in-flight <see cref="ReadBodyLines()"/> enumeration are no
    /// longer valid.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var buffer = _buffer;
        _buffer = null;
        if (buffer is not null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="Dispose"/>
    public ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Safety net for a response that was never disposed: returns the pooled buffer and increments
    /// <see cref="LeakedBufferCount"/> so a forgotten response cannot permanently starve the pool.
    /// </summary>
    ~NntpArticleResponse()
    {
        var buffer = _buffer;
        if (buffer is null)
        {
            return;
        }

        _buffer = null;
        ArrayPool<byte>.Shared.Return(buffer);
        Interlocked.Increment(ref _leakedBufferCount);
    }
}
