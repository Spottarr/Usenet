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
/// with <c>using</c>/<c>await using</c>); reading a view after disposal throws
/// <see cref="ObjectDisposedException"/> rather than returning recycled bytes, and a forgotten response
/// is recovered by a finalizer that returns the buffer and increments
/// <see cref="PooledBufferDiagnostics.LeakedBufferCount"/>.
/// </remarks>
[PublicAPI]
public sealed class NntpArticleResponse : NntpResponse, IDisposable, IAsyncDisposable
{
    private readonly PooledBuffer? _buffer;
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
    /// response is disposed; reading it afterwards throws <see cref="ObjectDisposedException"/>. Empty
    /// for a <c>HEAD</c> response or a failure response.
    /// </summary>
    public ReadOnlyMemory<byte> Body
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _buffer?.Content ?? ReadOnlyMemory<byte>.Empty;
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
        _buffer = new PooledBuffer(buffer, bodyOffset, length - bodyOffset);
    }

    /// <summary>
    /// Reads the article body as text lines on demand, using the default Usenet character encoding.
    /// Each line is decoded from the pooled buffer when enumerated; enumerating after this response is
    /// disposed throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    /// <returns>The body lines, with their CRLF terminators stripped.</returns>
    public IEnumerable<string> ReadBodyLines() => ReadBodyLines(UsenetEncoding.Default);

    /// <summary>
    /// Reads the article body as text lines on demand, using the specified character encoding.
    /// Each line is decoded from the pooled buffer when enumerated; enumerating after this response is
    /// disposed throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    /// <param name="encoding">The character encoding used to decode the body lines.</param>
    /// <returns>The body lines, with their CRLF terminators stripped.</returns>
    public IEnumerable<string> ReadBodyLines(Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _buffer is null ? [] : EnumerateLines(_buffer, encoding);
    }

    private static IEnumerable<string> EnumerateLines(PooledBuffer buffer, Encoding encoding)
    {
        var position = 0;
        // Re-acquire the span per line so a disposal between lines fails fast instead of reading a
        // recycled buffer. The span scan lives in a non-iterator helper because an iterator cannot
        // hold a ref struct local across a yield.
        while (TryReadLine(buffer, encoding, ref position, out var line))
        {
            yield return line;
        }
    }

    private static bool TryReadLine(
        PooledBuffer buffer,
        Encoding encoding,
        ref int position,
        out string line
    )
    {
        var span = buffer.GetSpan();
        if (position >= span.Length)
        {
            line = string.Empty;
            return false;
        }

        var rest = span[position..];
        var newline = rest.IndexOf((byte)'\n');
        var lineEnd = newline < 0 ? rest.Length : newline;
        var advance = newline < 0 ? rest.Length : newline + 1;

        var contentEnd = lineEnd;
        if (contentEnd > 0 && rest[contentEnd - 1] == (byte)'\r')
        {
            contentEnd--;
        }

        line = encoding.GetString(rest[..contentEnd]);
        position += advance;
        return true;
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
        ((IDisposable?)_buffer)?.Dispose();
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
    /// <see cref="PooledBufferDiagnostics.LeakedBufferCount"/> so a forgotten response cannot
    /// permanently starve the pool.
    /// </summary>
    ~NntpArticleResponse() => _buffer?.ReturnLeaked();
}
