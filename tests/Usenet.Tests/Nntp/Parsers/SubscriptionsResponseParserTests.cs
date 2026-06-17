using Usenet.Nntp.Parsers;

namespace Usenet.Tests.Nntp.Parsers;

internal sealed class SubscriptionsResponseParserTests
{
    private static readonly string[] Subscriptions =
    [
        "misc.test",
        "news.announce.newusers",
        "news.answers",
    ];

    [Test]
    public async Task ShouldParseGroupNames()
    {
        var groups = new SubscriptionsResponseParser().Parse(
            215,
            "information follows",
            Subscriptions
        );

        await Assert.That(groups).IsEquivalentTo(Subscriptions);
    }

    [Test]
    public async Task ShouldReturnEmptyForNonSuccessResponse()
    {
        var groups = new SubscriptionsResponseParser().Parse(503, "program error", ["misc.test"]);

        await Assert.That(groups.IsEmpty).IsTrue();
    }
}
