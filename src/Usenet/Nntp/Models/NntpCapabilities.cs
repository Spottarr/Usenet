using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// Represents the capabilities advertised by a server in response to the
/// <a href="https://tools.ietf.org/html/rfc3977#section-5.2">CAPABILITIES</a> command. Each
/// capability is a keyword (for example <c>READER</c>, <c>OVER</c> or <c>LIST</c>) optionally
/// followed by arguments describing the supported variants.
/// </summary>
[PublicAPI]
public sealed class NntpCapabilities
{
    private readonly ImmutableDictionary<string, ImmutableArray<string>> _capabilities;

    internal NntpCapabilities(IDictionary<string, ImmutableArray<string>> capabilities) =>
        _capabilities = capabilities.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets an empty <see cref="NntpCapabilities"/> object.
    /// </summary>
    public static NntpCapabilities Empty { get; } =
        new(new Dictionary<string, ImmutableArray<string>>());

    /// <summary>
    /// The capability keywords advertised by the server.
    /// </summary>
    public IReadOnlyCollection<string> Keywords => _capabilities.Keys.ToImmutableArray();

    /// <summary>
    /// Determines whether the server advertises the specified capability keyword.
    /// </summary>
    /// <param name="capability">The capability keyword to look for (case-insensitive).</param>
    /// <returns>true if the capability is advertised; otherwise false.</returns>
    public bool Supports(string capability) => _capabilities.ContainsKey(capability);

    /// <summary>
    /// Determines whether the server advertises the specified capability keyword with the
    /// specified argument, for example <c>OVER</c> with <c>MSGID</c>.
    /// </summary>
    /// <param name="capability">The capability keyword to look for (case-insensitive).</param>
    /// <param name="argument">The argument to look for (case-insensitive).</param>
    /// <returns>true if the capability is advertised with the argument; otherwise false.</returns>
    public bool Supports(string capability, string argument) =>
        _capabilities.TryGetValue(capability, out var arguments)
        && arguments.Contains(argument, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the arguments advertised for the specified capability keyword, or an empty list when
    /// the capability is not advertised or carries no arguments.
    /// </summary>
    /// <param name="capability">The capability keyword to look for (case-insensitive).</param>
    /// <returns>The arguments advertised for the capability.</returns>
    public IReadOnlyList<string> GetArguments(string capability) =>
        _capabilities.TryGetValue(capability, out var arguments) ? arguments : [];

    /// <summary>
    /// The protocol version advertised by the <c>VERSION</c> capability, or <see langword="null"/>
    /// when it is absent.
    /// </summary>
    public string? Version
    {
        get
        {
            var arguments = GetArguments("VERSION");
            return arguments.Count > 0 ? arguments[0] : null;
        }
    }

    /// <summary>
    /// Whether the server advertises the <c>READER</c> capability (reader mode).
    /// </summary>
    public bool IsReader => Supports("READER");

    /// <summary>
    /// The variants advertised for the <c>OVER</c> capability, for example <c>MSGID</c>.
    /// </summary>
    public IReadOnlyList<string> OverVariants => GetArguments("OVER");

    /// <summary>
    /// The variants advertised for the <c>LIST</c> capability, for example <c>ACTIVE</c>,
    /// <c>NEWSGROUPS</c> or <c>OVERVIEW.FMT</c>.
    /// </summary>
    public IReadOnlyList<string> ListVariants => GetArguments("LIST");
}
