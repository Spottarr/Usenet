namespace Usenet.Yenc;

internal static class YencCharacters
{
    public const byte Null = 0;
    public const byte Tab = 9;
    public const byte Lf = 10;
    public const byte Cr = 13;
    public const byte Space = 32;
    public const byte Dot = 46;
    public const byte Equal = 61;

    // yEnc maps every source byte to (byte + EncodeOffset) mod 256. Critical bytes are
    // escaped by emitting an '=' and adding a further EscapeOffset to the mapped value.
    public const byte EncodeOffset = 42;
    public const byte EscapeOffset = 64;
}
