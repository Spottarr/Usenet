using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpHeaderCollectionTests
{
    private static readonly string[] ExpectedKeys = ["Path", "References", "References", "Subject"];
    private static readonly string[] ExpectedReferences =
    [
        "<parent-1@example.com>",
        "<parent-2@example.com>",
    ];

    private static NntpHeaderCollection Build() =>
        new([
            new("Path", "news.example.com!not-for-mail"),
            new("References", "<parent-1@example.com>"),
            new("References", "<parent-2@example.com>"),
            new("Subject", "Example"),
        ]);

    [Test]
    public async Task ShouldPreserveOrder()
    {
        var headers = Build();

        var keys = headers.Select(h => h.Key).ToArray();

        await Assert.That(keys).IsEquivalentTo(ExpectedKeys);
    }

    [Test]
    public async Task LookupShouldBeCaseInsensitive()
    {
        var headers = Build();

        await Assert.That(headers.Contains("path")).IsTrue();
        await Assert.That(headers.TryGetValue("SUBJECT", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo("Example");
    }

    [Test]
    public async Task TryGetValueShouldReturnFirstValue()
    {
        var headers = Build();

        await Assert.That(headers.TryGetValue("References", out var value)).IsTrue();
        await Assert.That(value).IsEqualTo("<parent-1@example.com>");
    }

    [Test]
    public async Task TryGetValueForMissingKeyShouldReturnFalse()
    {
        var headers = Build();

        await Assert.That(headers.TryGetValue("Missing", out _)).IsFalse();
    }

    [Test]
    public async Task GetValuesShouldReturnEveryValueForKey()
    {
        var headers = Build();

        var values = headers.GetValues("references").ToArray();

        await Assert.That(values).IsEquivalentTo(ExpectedReferences);
    }

    [Test]
    public async Task EqualsShouldBeOrderIndependent()
    {
        var first = new NntpHeaderCollection([new("h1", "v1"), new("h2", "v2"), new("h2", "v3")]);
        var second = new NntpHeaderCollection([new("h2", "v3"), new("h1", "v1"), new("h2", "v2")]);

        await Assert.That(first).IsEqualTo(second);
        await Assert.That(first == second).IsTrue();
        await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    [Test]
    public async Task EqualsShouldBeCaseInsensitiveOnKeys()
    {
        var first = new NntpHeaderCollection([new("Subject", "Example")]);
        var second = new NntpHeaderCollection([new("subject", "Example")]);

        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task EqualsWithDifferentValuesShouldReturnFalse()
    {
        var first = new NntpHeaderCollection([new("h1", "v1"), new("h1", "v2")]);
        var second = new NntpHeaderCollection([new("h1", "v1"), new("h1", "v3")]);

        await Assert.That(first).IsNotEqualTo(second);
        await Assert.That(first != second).IsTrue();
    }
}
