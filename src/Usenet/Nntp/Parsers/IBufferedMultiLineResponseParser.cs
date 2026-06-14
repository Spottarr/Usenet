using JetBrains.Annotations;

namespace Usenet.Nntp.Parsers;

/// <summary>
/// Represents a parser for a multi-line response whose data block is materialized into a single
/// contiguous, pooled byte buffer instead of a sequence of decoded <see cref="string"/> lines.
/// The parser takes ownership of the pooled buffer on the success path; the response it produces
/// is responsible for returning the buffer to the pool when it is disposed.
/// </summary>
/// <typeparam name="TResponse">The type of the parsed response.</typeparam>
[PublicAPI]
public interface IBufferedMultiLineResponseParser<out TResponse>
{
    /// <summary>
    /// Determines if the received response code is valid.
    /// </summary>
    /// <param name="code">The response code received from the server.</param>
    /// <returns>true if <paramref name="code"/> is valid; otherwise false.</returns>
    bool IsSuccessResponse(int code);

    /// <summary>
    /// Parses a failure response that carries no data block.
    /// </summary>
    /// <param name="code">The response code received from the server.</param>
    /// <param name="message">The response message received from the server.</param>
    /// <returns>A new instance of type <typeparamref name="TResponse"/>.</returns>
    TResponse ParseError(int code, string message);

    /// <summary>
    /// Parses the multi-line data block of a successful response. The implementation takes
    /// ownership of <paramref name="buffer"/> and is responsible for returning it to the pool.
    /// </summary>
    /// <param name="code">The response code received from the server.</param>
    /// <param name="message">The response message received from the server.</param>
    /// <param name="buffer">The pooled buffer holding the materialized data block.</param>
    /// <param name="length">The number of valid bytes in <paramref name="buffer"/>.</param>
    /// <returns>A new instance of type <typeparamref name="TResponse"/>.</returns>
    TResponse Parse(int code, string message, byte[] buffer, int length);
}
