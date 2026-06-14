using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpArticleTests
{
    [Test]
    internal async Task EqualsWithSameValuesShouldReturnTrue()
    {
        var article1 = new NntpArticle(
            0,
            "123@bla.nl",
            NntpGroups.Empty,
            new NntpHeaderCollection([
                new("h1", "val1"),
                new("h3", "val3"),
                new("h2", "val2"),
                new("h3", "val4"),
            ]),
            new List<string>(0)
        );

        var article2 = new NntpArticle(
            0,
            "123@bla.nl",
            NntpGroups.Empty,
            new NntpHeaderCollection([
                new("h3", "val4"),
                new("h3", "val3"),
                new("h2", "val2"),
                new("h1", "val1"),
            ]),
            new List<string>(0)
        );

        await Assert.That(article2).IsEqualTo(article1);
    }
}
