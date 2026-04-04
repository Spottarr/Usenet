using Newtonsoft.Json;
using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpMessageIdTests
{
    [Test]
    public async Task NullMessageIdShouldThrow()
    {
        await Assert.That(() => new NntpMessageId(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    [Arguments("123@example.com", "<123@example.com>")]
    [Arguments("<123@example.com>", "<123@example.com>")]
    [Arguments("", "")]
    internal async Task ShouldBeFormattedCorrectly(string messageId, string expectedMessageId)
    {
        var actual = new NntpMessageId(messageId);
        await Assert.That(actual.ToString()).IsEqualTo(expectedMessageId);
        await Assert.That(actual.Value).IsEqualTo(expectedMessageId.Trim('<', '>'));
    }

    [Test]
    [Arguments("123@example.com", "<123@example.com>")]
    [Arguments("<123@example.com>", "<123@example.com>")]
    [Arguments("", "")]
    internal async Task EqualsWithSameValuesShouldReturnTrue(string first, string second)
    {
        var firstMessageId = new NntpMessageId(first);
        var secondMessageId = new NntpMessageId(second);
        await Assert.That(secondMessageId).IsEqualTo(firstMessageId);
        await Assert.That(firstMessageId == secondMessageId).IsTrue();
        await Assert.That(firstMessageId.Equals(secondMessageId)).IsTrue();
    }

    [Test]
    [Arguments("123@example.com")]
    internal async Task SerializedInstanceShouldBeDeserializedCorrectly(string messageId)
    {
        var expectedMessageId = new NntpMessageId(messageId);
        var json = JsonConvert.SerializeObject(expectedMessageId);
        var actualMessageId = JsonConvert.DeserializeObject<NntpMessageId>(json)!;
        await Assert.That(actualMessageId).IsEqualTo(expectedMessageId);
    }
}
