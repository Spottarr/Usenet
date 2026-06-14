using System.Buffers;
using System.Runtime.CompilerServices;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using Usenet.Util;

namespace Usenet.Tests.Nntp.Responses;

internal sealed class NntpArticleResponseTests
{
    private static NntpArticleResponse ParseBody(string body)
    {
        var bytes = UsenetEncoding.Default.GetBytes(body);
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(bytes.Length, 1));
        bytes.CopyTo(buffer, 0);
        return new ArticleResponseParser(ArticleRequestType.Body).Parse(
            222,
            "1 <1@example>",
            buffer,
            bytes.Length
        );
    }

    [Test]
    public async Task DoubleDisposeIsSafeAndBodyThrowsAfterwards()
    {
        var response = ParseBody("body line\r\n");

        DisposeTwice(response);

        await Assert.That(() => _ = response.Body).ThrowsExactly<ObjectDisposedException>();

        // A second, synchronous Dispose must be a safe no-op.
        static void DisposeTwice(NntpArticleResponse response)
        {
            response.Dispose();
            response.Dispose();
        }
    }

    [Test]
    public async Task ReadBodyLinesThrowsAfterDispose()
    {
        var response = ParseBody("body line\r\n");
        await response.DisposeAsync();

        await Assert.That(() => response.ReadBodyLines()).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    public async Task DisposeAsyncReturnsBufferAndInvalidatesBody()
    {
        var response = ParseBody("body line\r\n");

        await response.DisposeAsync();

        await Assert.That(() => _ = response.Body).ThrowsExactly<ObjectDisposedException>();
    }

    [Test]
    public async Task ForgottenResponseIsReclaimedByFinalizer()
    {
        var before = NntpArticleResponse.LeakedBufferCount;

        AbandonResponse();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await Assert.That(NntpArticleResponse.LeakedBufferCount).IsGreaterThan(before);

        // A reference must not be kept on the stack, or the finalizer never runs.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AbandonResponse() => _ = ParseBody("leaked body line\r\n");
    }
}
