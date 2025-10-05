using Microsoft.Extensions.Logging;

namespace Usenet.Extensions;

internal static partial class LogExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Found more than 1 {Header} header. Skipping it.")]
    public static partial void HeaderOccursMoreThanOnce(this ILogger logger, string header);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Header} header has invalid value {Value}. Skipping it.")]
    public static partial void InvalidHeader(this ILogger logger, string header, string value);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid header line: {Line} Expected: {{key}}:{{value}}")]
    public static partial void InvalidHeaderLine(this ILogger logger, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid posting status {Status} in line: {Line}")]
    public static partial void InvalidPostingStatus(this ILogger logger, string status, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid response code: {Code}")]
    public static partial void InvalidResponseCode(this ILogger logger, int code);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid newsgroup origin line: {Line} Expected: {{group}} {{timestamp}} {{createdby}}")]
    public static partial void InvalidGroupOriginLine(this ILogger logger, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid newsgroup information line: {Line} Expected: {{group}} {{high}} {{low}} {{status}}")]
    public static partial void InvalidGroupBasicInformationLine(this ILogger logger, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid newsgroup information line: {Line} Expected: {{group}} {{high}} {{low}} {{count}} {{status}}")]
    public static partial void InvalidGroupExtendedInformationLine(this ILogger logger, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid response message: {Message} Expected: {{number}} {{messageid}}")]
    public static partial void InvalidResponseMessage(this ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid response message: {Message} Expected: {{count}} {{low}} {{high}} {{group}}")]
    public static partial void InvalidGroupResponseMessage(this ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid response message: {Message} Expected: {{yyyymmddhhmmss}}")]
    public static partial void InvalidDateResponseMessage(this ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connecting: {Hostname} {Port} (Use SSl = {UseSsl})")]
    public static partial void Connecting(this ILogger logger, string hostname, int port, bool useSsl);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sending command: {Command}")]
    public static partial void SendingCommand(this ILogger logger, string command);

    [LoggerMessage(Level = LogLevel.Information, Message = "Response received: {Response}")]
    public static partial void ReceivedResponse(this ILogger logger, string response);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Borrowing NNTP client from pool")]
    public static partial void BorrowingNntpClient(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning NNTP client to pool")]
    public static partial void ReturningNntpClient(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Creating new NNTP client ({CurrentPoolSize}/{MaxPoolSize})")]
    public static partial void CreatingNewNntpClient(this ILogger logger, int currentPoolSize, int maxPoolSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Disposing idle NNTP client ({CurrentPoolSize}/{MaxPoolSize})")]
    public static partial void DisposingIdleNntpClient(this ILogger logger, int currentPoolSize, int maxPoolSize);

    [LoggerMessage(Level = LogLevel.Error, Message = "Disposing NNTP client left in an errored state after a previous operation ({CurrentPoolSize}/{MaxPoolSize})")]
    public static partial void DisposingErroredNntpClient(this ILogger logger, int currentPoolSize, int maxPoolSize);
}
