namespace Usenet.Util;

/// <summary>
/// Utiliy class for calculating CRC-32 checksums.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
internal static class Crc32
{
    private const uint Polynomial = 0xEDB88320;
    private const uint Seed = 0xFFFFFFFF;
    private static readonly uint[] _lookupTable = CreateLookupTable();

    public static uint CalculateChecksum(IEnumerable<byte> buffer)
    {
        Guard.ThrowIfNull(buffer, nameof(buffer));

        var value = Seed;
        foreach (var b in buffer)
        {
            value = (value >> 8) ^ _lookupTable[(value & 0xFF) ^ b];
        }

        return value ^ Seed;
    }

    public static uint Initialize() => Seed;

    public static uint Calculate(uint value, int @byte) =>
        (value >> 8) ^ _lookupTable[(value & 0xFF) ^ @byte];

    public static uint Finalize(uint value) => value ^ Seed;

    private static uint[] CreateLookupTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var entry = i;
            for (var j = 0; j < 8; j++)
            {
                entry = (entry & 1) == 1 ? (entry >> 1) ^ Polynomial : entry >> 1;
            }

            table[i] = entry;
        }

        return table;
    }
}