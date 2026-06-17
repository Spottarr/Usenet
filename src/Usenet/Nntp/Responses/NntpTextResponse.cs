using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Usenet.Nntp.Responses;

/// <summary>
/// Represents a genuinely free-form multi-line text response, such as the output of the
/// <a href="https://tools.ietf.org/html/rfc3977#section-7.2">HELP</a>,
/// <a href="https://tools.ietf.org/html/rfc6048#section-2.5">LIST MOTD</a> and
/// <a href="https://tools.ietf.org/html/rfc3977#section-8.6">LIST HEADERS</a> commands, whose
/// data blocks carry no further structure worth modelling.
/// </summary>
[PublicAPI]
public class NntpTextResponse : NntpResponse
{
    /// <summary>
    /// The lines received from the server.
    /// </summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>
    /// The <see cref="Lines"/> joined with a line feed, for convenient display.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Creates a new instance of the <see cref="NntpTextResponse"/> class.
    /// </summary>
    /// <param name="code">The response code received from the server.</param>
    /// <param name="message">The response message received from the server.</param>
    /// <param name="success">A value indicating whether the command succeeded or failed.</param>
    /// <param name="lines">The lines received from the server.</param>
    internal NntpTextResponse(int code, string message, bool success, IEnumerable<string> lines)
        : base(code, message, success)
    {
        Lines = lines.ToImmutableArray();
        Text = string.Join('\n', Lines);
    }
}
