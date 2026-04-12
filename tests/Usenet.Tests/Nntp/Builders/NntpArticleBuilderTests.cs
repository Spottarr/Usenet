using System.Diagnostics.CodeAnalysis;
using Usenet.Exceptions;
using Usenet.Nntp;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Models;
using Usenet.Util;

namespace Usenet.Tests.Nntp.Builders;

internal sealed class NntpArticleBuilderTests
{
    [Test]
    public async Task BuildWithoutMessageIdShouldThrow()
    {
        await Assert
            .That(() =>
            {
                new NntpArticleBuilder()
                    .SetFrom("superuser")
                    .SetSubject("subject")
                    .AddGroups("alt.test")
                    .Build();
            })
            .ThrowsExactly<NntpException>()
            .WithMessage($"{NntpHeaders.MessageId} header not set", StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildWithoutFromShouldThrow()
    {
        await Assert
            .That(() =>
            {
                new NntpArticleBuilder()
                    .SetMessageId("123@hhh.net")
                    .SetSubject("subject")
                    .AddGroups("alt.test")
                    .Build();
            })
            .ThrowsExactly<NntpException>()
            .WithMessage($"{NntpHeaders.From} header not set", StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildWithoutSubjectShouldThrow()
    {
        await Assert
            .That(() =>
            {
                new NntpArticleBuilder()
                    .SetMessageId("123@hhh.net")
                    .SetFrom("superuser")
                    .AddGroups("alt.test")
                    .Build();
            })
            .ThrowsExactly<NntpException>()
            .WithMessage($"{NntpHeaders.Subject} header not set", StringComparison.Ordinal);
    }

    [Test]
    public async Task BuildWithoutGroupShouldThrow()
    {
        await Assert
            .That(() =>
            {
                new NntpArticleBuilder()
                    .SetMessageId("123@hhh.net")
                    .SetFrom("superuser")
                    .SetSubject("subject")
                    .Build();
            })
            .ThrowsExactly<NntpException>()
            .WithMessage($"{NntpHeaders.Newsgroups} header not set", StringComparison.Ordinal);
    }

    [Test]
    [Arguments(NntpHeaders.Subject)]
    [Arguments(NntpHeaders.MessageId)]
    [Arguments(NntpHeaders.Newsgroups)]
    [Arguments(NntpHeaders.From)]
    public async Task SettingHeaderWithReservedKeyShouldThrow(string headerKey)
    {
        await Assert
            .That(() =>
            {
                new NntpArticleBuilder().AddHeader(headerKey, "val");
            })
            .ThrowsExactly<NntpException>()
            .WithMessage("Reserved header key not allowed", StringComparison.Ordinal);
    }

    [Test]
    [Arguments("", "Val", "key")]
    [Arguments(" ", "Val", "key")]
    public async Task SettingHeaderWithEmptyParametersShouldThrow(
        string key,
        string value,
        string expectedParamName
    )
    {
        ArgumentException? caught = null;
        try
        {
            new NntpArticleBuilder().AddHeader(key, value);
        }
        catch (ArgumentException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.ParamName).IsEqualTo(expectedParamName);
    }

    [Test]
    [MethodDataSource(nameof(GetNullParameterData))]
    public async Task SettingHeaderWithNullParametersShouldThrow(
        string? key,
        string? value,
        string expectedParamName
    )
    {
        ArgumentNullException? caught = null;
        try
        {
            new NntpArticleBuilder().AddHeader(key!, value!);
        }
        catch (ArgumentNullException ex)
        {
            caught = ex;
        }

        await Assert.That(caught).IsNotNull();
        await Assert.That(caught!.ParamName).IsEqualTo(expectedParamName);
    }

    public static IEnumerable<(string?, string?, string)> GetNullParameterData()
    {
        yield return ("Key", null, "value");
        yield return (null, "Val", "key");
    }

    [Test]
    [SuppressMessage("ReSharper", "DuplicateKeyCollectionInitialization")]
    public async Task BuildShouldBuildArticle()
    {
        var expected = new NntpArticle(
            0,
            "123@hhh.net",
            "alt.test;alt.testclient",
            new MultiValueDictionary<string, string>
            {
                { NntpHeaders.Subject, "subject" },
                { NntpHeaders.From, "superuser" },
                { "Header1", "Value1" },
                { "Header1", "Value2" },
            },
            []
        );

        var actual = new NntpArticleBuilder()
            .SetMessageId("123@hhh.net")
            .SetFrom("superuser")
            .AddHeader("Header1", "Value2")
            .SetSubject("subject")
            .AddGroups("alt.test")
            .AddGroups("alt.testclient")
            .AddHeader("Header1", "Value1")
            .Build();

        await Assert.That(actual).IsEqualTo(expected);
        await Assert.That(expected.Equals(actual)).IsTrue();
        await Assert.That(expected == actual).IsTrue();
    }

    [Test]
    [SuppressMessage("ReSharper", "DuplicateKeyCollectionInitialization")]
    public async Task BuildInitializedFromExistingArticleShouldBuildSameArticle()
    {
        var expected = new NntpArticle(
            0,
            "123@hhh.net",
            "alt.test;alt.testclient",
            new MultiValueDictionary<string, string>
            {
                { NntpHeaders.Subject, "subject" },
                { NntpHeaders.From, "superuser" },
                { "Header1", "Value1" },
                { "Header1", "Value2" },
            },
            []
        );

        var actual = new NntpArticleBuilder().InitializeFrom(expected).Build();

        await Assert.That(actual).IsEqualTo(expected);
        await Assert.That(expected.Equals(actual)).IsTrue();
        await Assert.That(expected == actual).IsTrue();
    }
}
