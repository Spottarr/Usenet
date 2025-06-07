﻿using Newtonsoft.Json;
using Usenet.Nntp.Models;
using Xunit;

namespace Usenet.Tests.Nntp.Models;

public class NntpMessageIdTests
{
    [Theory]
    [InlineData("123@example.com", "<123@example.com>")]
    [InlineData("<123@example.com>", "<123@example.com>")]
    [InlineData(null, "")]
    [InlineData("", "")]
    internal void ShouldBeFormattedCorrectly(string? messageId, string expectedMessageId)
    {
        var actual = new NntpMessageId(messageId);
        Assert.Equal(expectedMessageId, actual.ToString());
        Assert.Equal(expectedMessageId.Trim('<', '>'), actual.Value);
    }

    [Theory]
    [InlineData("123@example.com", "<123@example.com>")]
    [InlineData("<123@example.com>", "<123@example.com>")]
    [InlineData(null, null)]
    [InlineData("", "")]
    internal void EqualsWithSameValuesShouldReturnTrue(string? first, string? second)
    {
        var firstMessageId = new NntpMessageId(first);
        var secondMessageId = new NntpMessageId(second);
        Assert.Equal(firstMessageId, secondMessageId);
        Assert.True(firstMessageId == secondMessageId);
        Assert.True(firstMessageId.Equals(secondMessageId));
    }

    [Theory]
    [InlineData("123@example.com")]
    internal void SerializedInstanceShouldBeDeserializedCorrectly(string messageId)
    {
        var expectedMessageId = new NntpMessageId(messageId);
        var json = JsonConvert.SerializeObject(expectedMessageId);
        var actualMessageId = JsonConvert.DeserializeObject<NntpMessageId>(json);
        Assert.Equal(expectedMessageId, actualMessageId);
    }
}