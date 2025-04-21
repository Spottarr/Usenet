﻿using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using Usenet.Util;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Nntp.Parsers
{
    public class ArticleResponseParserTests
    {
        public static readonly IEnumerable<object[]> MultiLineParseData =
        [
            [
                220, "123 <123@poster.com>", (int) ArticleRequestType.Article,
                Array.Empty<string>(),
                new XSerializable<NntpArticle>(new NntpArticle(123, "<123@poster.com>", null, null,
                    new List<string>(0)))
            ],
            [
                220, "123 <123@poster.com>", (int) ArticleRequestType.Article,
                new[]
                {
                    "Path: pathost!demo!whitehouse!not-for-mail",
                    "From: \"Demo User\" <nobody@example.net>",
                    "",
                    "This is just a test article (1).",
                    "With two lines."
                },
                new XSerializable<NntpArticle>(new NntpArticle(123, "<123@poster.com>", null,
                    new MultiValueDictionary<string, string>
                    {
                        {"Path", "pathost!demo!whitehouse!not-for-mail"},
                        {"From", "\"Demo User\" <nobody@example.net>"},
                    }, new List<string>
                    {
                        "This is just a test article (1).",
                        "With two lines."
                    }))
            ],
            [
                222, "123 <123@poster.com>", (int) ArticleRequestType.Body,
                new[]
                {
                    "This is just a test article (2).",
                    "With two lines."
                },
                new XSerializable<NntpArticle>(new NntpArticle(123, "<123@poster.com>", null, null, new List<string>
                {
                    "This is just a test article (2).",
                    "With two lines."
                }))
            ],
            [
                221, "123 <123@poster.com>", (int) ArticleRequestType.Head,
                new[]
                {
                    "Multi: line1",
                    " line2",
                    " line3",
                    "Path: pathost!demo!whitehouse!not-for-mail"
                },
                new XSerializable<NntpArticle>(new NntpArticle(123, "<123@poster.com>", null,
                    new MultiValueDictionary<string, string>
                    {
                        {"Multi", "line1 line2 line3"},
                        {"Path", "pathost!demo!whitehouse!not-for-mail"},
                    }, new List<string>(0)))
            ],
            [
                221, "123 <123@poster.com>", (int) ArticleRequestType.Head,
                new[]
                {
                    "Invalid header line",
                    "Path: pathost!demo!whitehouse!not-for-mail"
                },
                new XSerializable<NntpArticle>(new NntpArticle(123, "<123@poster.com>", null,
                    new MultiValueDictionary<string, string>
                    {
                        {"Path", "pathost!demo!whitehouse!not-for-mail"},
                    }, new List<string>(0)))
            ]
        ];

        [Theory]
        [MemberData(nameof(MultiLineParseData))]
        internal void MultiLineResponseShouldBeParsedCorrectly(
            int responseCode, 
            string responseMessage, 
            int requestType,
            string[] lines, 
            XSerializable<NntpArticle> expected)
        {
            NntpArticle expectedArticle = expected.Object;
            NntpArticleResponse articleResponse = new ArticleResponseParser((ArticleRequestType)requestType)
                .Parse(responseCode, responseMessage, lines.ToList());
            NntpArticle actualArticle = articleResponse.Article;
            Assert.Equal(expectedArticle, actualArticle);
        }

        public static readonly IEnumerable<object[]> InvalidMultiLineParseData =
        [
            [
                412, "No newsgroup selected", (int) ArticleRequestType.Article, Array.Empty<string>()
            ],
            [
                420, "No current article selected", (int) ArticleRequestType.Article, Array.Empty<string>()
            ],
            [
                423, "No article with that number", (int) ArticleRequestType.Article, Array.Empty<string>()
            ],
            [
                430, "No such article found", (int) ArticleRequestType.Article, Array.Empty<string>()
            ]
        ];

        [Theory]
        [MemberData(nameof(InvalidMultiLineParseData))]
        internal void InvalidMultiLineResponseShouldBeParsedCorrectly(
            int responseCode,
            string responseMessage,
            int requestType,
            string[] lines)
        {
            NntpArticleResponse articleResponse = new ArticleResponseParser((ArticleRequestType)requestType).Parse(
                responseCode, responseMessage, lines.ToList());
            Assert.Null(articleResponse.Article);
        }
    }
}
