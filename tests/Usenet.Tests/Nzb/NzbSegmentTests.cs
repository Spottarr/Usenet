using Usenet.Nzb;

namespace Usenet.Tests.Nzb;

internal sealed class NzbSegmentTests
{
    [Test]
    internal async Task EqualsWithSameValuesShouldReturnTrue()
    {
        var expected = new NzbSegment(1, 1000, 1200, "1234567890@base.msg");
        var actual = new NzbSegment(1, 1000, 1200, "1234567890@base.msg");
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments(1, 1000, 1200, "nomatch@bla.bla")]
    [Arguments(1, 1000, 1300, "1234567890@base.msg")]
    [Arguments(1, 1100, 1200, "1234567890@base.msg")]
    [Arguments(2, 1000, 1200, "1234567890@base.msg")]
    internal async Task EqualsWithDifferentValuesShouldReturnFalse(
        int number,
        long offset,
        long size,
        string messageId
    )
    {
        var expected = new NzbSegment(1, 1000, 1200, "1234567890@base.msg");
        var actual = new NzbSegment(number, offset, size, messageId);
        await Assert.That(actual).IsNotEqualTo(expected);
    }
}
