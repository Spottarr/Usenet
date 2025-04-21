using System.Net.Security;
using System.Net.Sockets;
using Usenet.Exceptions;
using Microsoft.Extensions.Logging;
using Usenet.Extensions;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using Usenet.Util;

namespace Usenet.Nntp
{
    /// <summary>
    /// A standard implementation of an NNTP connection.
    /// Based on Kristian Hellang's NntpLib.Net project https://github.com/khellang/NntpLib.Net.
    /// </summary>
    /// <remarks>This implementation of the <see cref="INntpConnection"/> interface does support SSL encryption but
    /// does not support compressed multi-line results.</remarks>
    public sealed class NntpConnection : INntpConnection
    {
        private readonly ILogger _log = Logger.Create<NntpConnection>();
        private readonly TcpClient _client = new TcpClient();
        private StreamWriter _writer;
        private NntpStreamReader _reader;
        private const string AuthInfoPass = "AUTHINFO PASS";

        /// <inheritdoc/>
        public CountingStream Stream { get; private set; }

        /// <inheritdoc/>
        public async Task<TResponse> ConnectAsync<TResponse>(string hostname, int port, bool useSsl, IResponseParser<TResponse> parser)
        {
            _log.Connecting(hostname, port, useSsl);
            await _client.ConnectAsync(hostname, port).ConfigureAwait(false);
            Stream = await GetStreamAsync(hostname, useSsl).ConfigureAwait(false);
            _writer = new StreamWriter(Stream, UsenetEncoding.Default) { AutoFlush = true };
            _reader = new NntpStreamReader(Stream, UsenetEncoding.Default);
            return GetResponse(parser);
        }

        /// <inheritdoc/>
        public TResponse Command<TResponse>(string command, IResponseParser<TResponse> parser)
        {
            ThrowIfNotConnected();
            Guard.ThrowIfNull(command, nameof(command));

            var logCommand = command.StartsWith(AuthInfoPass, StringComparison.Ordinal)
                ? $"{AuthInfoPass} [REDACTED]"
                : command;
            _log.SendingCommand(logCommand);
            _writer.WriteLine(command);
            return GetResponse(parser);
        }

        /// <inheritdoc/>
        public TResponse MultiLineCommand<TResponse>(string command, IMultiLineResponseParser<TResponse> parser) //, bool decompress = false)
        {
            Guard.ThrowIfNull(parser, nameof(parser));

            var response = Command(command, new ResponseParser());

            var dataBlock = parser.IsSuccessResponse(response.Code)
                ? ReadMultiLineDataBlock()
                : [];

            return parser.Parse(response.Code, response.Message, dataBlock);
        }

        /// <inheritdoc/>
        public TResponse GetResponse<TResponse>(IResponseParser<TResponse> parser)
        {
            Guard.ThrowIfNull(parser, nameof(parser));

            var responseText = _reader.ReadLine();
            _log.ReceivedResponse(responseText);

            if (responseText == null)
            {
                throw new NntpException("Received no response.");
            }
            if (responseText.Length < 3 || !IntShims.TryParse(responseText.AsSpan(0, 3), out var code))
            {
                throw new NntpException("Received invalid response.");
            }
            return parser.Parse(code, responseText.Substring(3).Trim());
        }

        /// <inheritdoc/>
        public void WriteLine(string line)
        {
            ThrowIfNotConnected();
            _writer.WriteLine(line);
        }

        private void ThrowIfNotConnected()
        {
            if (!_client.Connected)
            {
                throw new NntpException("Client not connected.");
            }
        }

        private async Task<CountingStream> GetStreamAsync(string hostname, bool useSsl)
        {
            var stream = _client.GetStream();
            if (!useSsl)
            {
                return new CountingStream(stream);
            }
            var sslStream = new SslStream(stream);
            await sslStream.AuthenticateAsClientAsync(hostname).ConfigureAwait(false);
            return new CountingStream(sslStream);
        }

        private IEnumerable<string> ReadMultiLineDataBlock()
        {
            string line;
            while ((line = _reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _client?.Dispose();
            _writer?.Dispose();
            _reader?.Dispose();
        }
    }
}
