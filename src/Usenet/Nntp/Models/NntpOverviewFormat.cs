using System.Collections.Immutable;
using JetBrains.Annotations;

namespace Usenet.Nntp.Models;

/// <summary>
/// The overview database layout returned by
/// <a href="https://tools.ietf.org/html/rfc3977#section-8.4">LIST OVERVIEW.FMT</a>: the ordered list
/// of fields present in each <c>OVER</c>/<c>XOVER</c> row. The ordering can later drive the overview
/// parser for servers whose field layout differs from the RFC 3977 default.
/// </summary>
[PublicAPI]
public sealed class NntpOverviewFormat
{
    /// <summary>
    /// The fields, in the order in which they appear in an overview row.
    /// </summary>
    public IReadOnlyList<NntpOverviewField> Fields { get; }

    internal NntpOverviewFormat(IEnumerable<NntpOverviewField> fields) =>
        Fields = fields.ToImmutableArray();

    /// <summary>
    /// Gets an empty <see cref="NntpOverviewFormat"/> object.
    /// </summary>
    public static NntpOverviewFormat Empty { get; } = new([]);
}
