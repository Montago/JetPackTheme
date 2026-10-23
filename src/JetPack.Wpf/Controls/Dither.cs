namespace JetPack.Wpf.Controls;

/// <summary>Noise for the dithered drawing in <see cref="Vignette"/> and <see cref="DitheredFill"/>.</summary>
internal static class Dither
{
    /// <summary>
    /// Repeatable noise in [0, 1) from pixel coordinates. Added to a colour value before rounding
    /// down, it dithers a gradient instead of leaving bands.
    /// </summary>
    public static double Noise(int x, int y)
    {
        var h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
        h ^= h >> 13;
        h *= 0x5bd1e995;
        h ^= h >> 15;
        return (h & 0xFFFF) / 65536.0;
    }
}
