using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Usenet.Nntp.Contracts;

namespace Usenet.Nntp.Responses;

/// <summary>
/// Parses a single line of a streamed multi-line data block into a value of type
/// <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type each data-block line is parsed into.</typeparam>
/// <param name="line">The data-block line, with dot-stuffing already undone.</param>
/// <param name="value">The parsed value when the line is valid.</param>
/// <returns><see langword="true"/> when <paramref name="line"/> was parsed into
/// <paramref name="value"/>; <see langword="false"/> to skip the line.</returns>
[PublicAPI]
public delegate bool NntpStreamLineParser<T>(string line, [MaybeNullWhen(false)] out T value);

/// <summary>
/// Represents a streamed multi-line response. The data block is parsed per line as it arrives off
/// the connection rather than being materialized up front, so memory stays flat over arbitrarily
/// large ranges.
/// </summary>
/// <remarks>A streamed response ties up its connection until the data block is fully enumerated or
/// the response is disposed. Enumerate it fully, or <see langword="await using"/> it, before issuing
/// the next command on the same connection or returning its lease to the pool.</remarks>
/// <typeparam name="T">The type each data-block line is parsed into.</typeparam>
[PublicAPI]
public sealed class NntpStreamResponse<T> : NntpResponse, IAsyncEnumerable<T>, IAsyncDisposable
{
    private readonly INntpStreamSource? _source;
    private readonly NntpStreamLineParser<T> _lineParser;
    private bool _finished;

    /// <summary>
    /// Creates a successful streamed response backed by a live connection.
    /// </summary>
    internal NntpStreamResponse(
        int code,
        string message,
        INntpStreamSource source,
        NntpStreamLineParser<T> lineParser
    )
        : base(code, message, true)
    {
        _source = source;
        _lineParser = lineParser;
    }

    /// <summary>
    /// Creates an unsuccessful streamed response with no data block.
    /// </summary>
    internal NntpStreamResponse(int code, string message, NntpStreamLineParser<T> lineParser)
        : base(code, message, false)
    {
        _lineParser = lineParser;
        _finished = true;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default
    )
    {
        if (_source is null || _finished)
        {
            yield break;
        }

        try
        {
            while (true)
            {
                var line = await _source
                    .ReadStreamLineAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (line is null)
                {
                    break;
                }

                if (_lineParser(line, out var value))
                {
                    yield return value;
                }
            }
        }
        finally
        {
            // If enumeration stopped early (e.g. break), drain the rest so the connection is clean.
            await _source.DrainStreamAsync(cancellationToken).ConfigureAwait(false);
            _finished = true;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_source is null || _finished)
        {
            return;
        }

        await _source.DrainStreamAsync(CancellationToken.None).ConfigureAwait(false);
        _finished = true;
    }
}
