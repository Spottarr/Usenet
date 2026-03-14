using System.Text;
using Usenet.Util;

namespace Usenet.Nntp;

/// <summary>
/// This NNTP streamreader respects the <a href="https://tools.ietf.org/html/rfc3977#section-3.1.1">rules</a> for multi-line data blocks.
/// It will undo dot-stuffing and will stop at the terminating line (".").
/// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
/// </summary>
internal class NntpStreamReader : StreamReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NntpStreamReader"/> class for the specified stream, with the default usenet encoding.
    /// </summary>
    /// <param name="stream">The stream to be read.</param>
    public NntpStreamReader(Stream stream)
        : base(stream, UsenetEncoding.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NntpStreamReader"/> class for the specified stream, with the specified character encoding.
    /// </summary>
    /// <param name="stream">The stream to be read.</param>
    /// <param name="encoding">The character encoding to use.</param>
    public NntpStreamReader(Stream stream, Encoding encoding)
        : base(stream, encoding) { }

    /// <summary>
    /// Reads a line of characters from the current stream and returns the data as a string.
    /// Dot-stuffing will be undone and the terminating line (".") will result in a null value
    /// indicating end of input.
    /// </summary>
    /// <returns>The next line from the input stream, or null if the end of the input stream is reached.</returns>
    public override string? ReadLine()
    {
        var line = base.ReadLine();
        return ProcessLine(line);
    }

    /// <summary>
    /// Asynchronously reads a line of characters from the current stream and returns the data as a string.
    /// Dot-stuffing will be undone and the terminating line (".") will result in a null value
    /// indicating end of input.
    /// </summary>
    /// <returns>The next line from the input stream, or null if the end of the input stream is reached.</returns>
    public override async Task<string?> ReadLineAsync()
    {
        var line = await base.ReadLineAsync().ConfigureAwait(false);
        return ProcessLine(line);
    }

    /// <summary>
    /// Asynchronously reads a line of characters from the current stream and returns the data as a string.
    /// Dot-stuffing will be undone and the terminating line (".") will result in a null value
    /// indicating end of input.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The next line from the input stream, or null if the end of the input stream is reached.</returns>
#if NET8_0_OR_GREATER
    public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        var line = await base.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        return ProcessLine(line);
    }
#else
    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var line = await base.ReadLineAsync().ConfigureAwait(false);
        return ProcessLine(line);
    }
#endif

    private static string? ProcessLine(string? line)
    {
        if (line == null)
        {
            return null;
        }

        if (line.Length == 0 || line[0] != '.')
        {
            return line;
        }

        if (line.Length == 1)
        {
            return null;
        }

        return line[1] == '.' ? line[1..] : line;
    }
}
