using System.Buffers;
using JetBrains.Annotations;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;

namespace Usenet.Nntp.Contracts;

/// <summary>
/// Represents an NNTP connection.
/// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
/// </summary>
[PublicAPI]
public interface INntpConnection : IDisposable
{
    /// <summary>
    /// Attempts to establish a connection with a usenet server.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="hostname">The hostname of the usenet server.</param>
    /// <param name="port">The port to use.</param>
    /// <param name="useSsl">A value to indicate whether to use SSL encryption.</param>
    /// <param name="parser">The response parser to use.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> ConnectAsync<TResponse>(
        string hostname,
        int port,
        bool useSsl,
        IResponseParser<TResponse> parser
    );

    /// <summary>
    /// Attempts to establish a connection with a usenet server.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="hostname">The hostname of the usenet server.</param>
    /// <param name="port">The port to use.</param>
    /// <param name="useSsl">A value to indicate whether to use SSL encryption.</param>
    /// <param name="parser">The response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> ConnectAsync<TResponse>(
        string hostname,
        int port,
        bool useSsl,
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Sends a command to the usenet server asynchronously. The response is expected to be a single line.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="parser">The response parser to use.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> CommandAsync<TResponse>(string command, IResponseParser<TResponse> parser);

    /// <summary>
    /// Sends a command to the usenet server asynchronously. The response is expected to be a single line.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="parser">The response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> CommandAsync<TResponse>(
        string command,
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Sends a command to the usenet server asynchronously. The response is expected to be multiple lines.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="parser">The multi-line response parser to use.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> MultiLineCommandAsync<TResponse>(
        string command,
        IMultiLineResponseParser<TResponse> parser
    );

    /// <summary>
    /// Sends a command to the usenet server asynchronously. The response is expected to be multiple lines.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="parser">The multi-line response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> MultiLineCommandAsync<TResponse>(
        string command,
        IMultiLineResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Sends a command to the usenet server asynchronously and streams the multi-line data block,
    /// parsing each line as it arrives instead of materializing the whole range.
    /// </summary>
    /// <typeparam name="T">The type each data-block line is parsed into.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="successCode">The response code that indicates a data block follows.</param>
    /// <param name="lineParser">The per-line parser to use.</param>
    /// <returns>A streamed response that must be fully enumerated or disposed before the next command.</returns>
    Task<NntpStreamResponse<T>> MultiLineStreamCommandAsync<T>(
        string command,
        int successCode,
        NntpStreamLineParser<T> lineParser
    );

    /// <summary>
    /// Sends a command to the usenet server asynchronously and streams the multi-line data block,
    /// parsing each line as it arrives instead of materializing the whole range.
    /// </summary>
    /// <typeparam name="T">The type each data-block line is parsed into.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="successCode">The response code that indicates a data block follows.</param>
    /// <param name="lineParser">The per-line parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A streamed response that must be fully enumerated or disposed before the next command.</returns>
    Task<NntpStreamResponse<T>> MultiLineStreamCommandAsync<T>(
        string command,
        int successCode,
        NntpStreamLineParser<T> lineParser,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Sends a command to the usenet server asynchronously and materializes the multi-line data block
    /// into a single contiguous, pooled byte buffer instead of decoding it into <see cref="string"/> lines.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="command">The command to send to the server.</param>
    /// <param name="parser">The buffered multi-line response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> BufferedMultiLineCommandAsync<TResponse>(
        string command,
        IBufferedMultiLineResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets a single-line response from the usenet server asynchronously.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="parser">The response parser to use.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> GetResponseAsync<TResponse>(IResponseParser<TResponse> parser);

    /// <summary>
    /// Gets a single-line response from the usenet server asynchronously.
    /// </summary>
    /// <typeparam name="TResponse">The type of the parsed response.</typeparam>
    /// <param name="parser">The response parser to use.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A response object of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> GetResponseAsync<TResponse>(
        IResponseParser<TResponse> parser,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Sends a line to the usenet server asynchronously.
    /// </summary>
    /// <param name="line">The line to send to the usenet server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteLineAsync(string line);

    /// <summary>
    /// Sends a line to the usenet server asynchronously.
    /// </summary>
    /// <param name="line">The line to send to the usenet server.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task WriteLineAsync(string line, CancellationToken cancellationToken);

    /// <summary>
    /// Buffers a single CRLF-terminated line into the connection's write buffer without flushing it
    /// to the underlying transport. Call <see cref="FlushAsync"/> to send the buffered bytes. This
    /// allows a whole command (for example an article) to be batched into a single flush.
    /// </summary>
    /// <param name="line">The line to buffer.</param>
    void BufferLine(string line);

    /// <summary>
    /// Flushes any bytes buffered by <see cref="BufferLine"/> or written to <see cref="Output"/> to
    /// the underlying transport.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The byte sink backing the connection's write buffer. Bytes written here are batched until
    /// <see cref="FlushAsync"/> is called and are included in <see cref="BytesWritten"/>. This is the
    /// seam the streaming yEnc encoder writes into so encoded bytes flow straight to the transport
    /// without an intermediate list of lines.
    /// </summary>
    IBufferWriter<byte> Output { get; }

    /// <summary>
    /// The number of bytes read from the connection.
    /// </summary>
    long BytesRead { get; }

    /// <summary>
    /// The number of bytes written to the connection.
    /// </summary>
    long BytesWritten { get; }

    /// <summary>
    /// Resets the <see cref="BytesRead"/> and <see cref="BytesWritten"/> counters.
    /// </summary>
    void ResetCounters();
}
