using System.Diagnostics.CodeAnalysis;
using Usenet.Nntp.Models;
using Usenet.Util;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpArticleTests
{
    [Test]
    [SuppressMessage("ReSharper", "DuplicateKeyCollectionInitialization")]
    internal async Task EqualsWithSameValuesShouldReturnTrue()
    {
        var article1 = new NntpArticle(
            0,
            "123@bla.nl",
            NntpGroups.Empty,
            new MultiValueDictionary<string, string>
            {
                { "h1", "val1" },
                { "h3", "val3" },
                { "h2", "val2" },
                { "h3", "val4" },
            },
            new List<string>(0)
        );

        var article2 = new NntpArticle(
            0,
            "123@bla.nl",
            NntpGroups.Empty,
            new MultiValueDictionary<string, string>
            {
                { "h3", "val4" },
                { "h3", "val3" },
                { "h2", "val2" },
                { "h1", "val1" },
            },
            new List<string>(0)
        );

        await Assert.That(article2).IsEqualTo(article1);
    }
}
