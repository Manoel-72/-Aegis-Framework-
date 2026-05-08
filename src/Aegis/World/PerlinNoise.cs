namespace Aegis.World;

/// <summary>
/// Perlin 2D determinístico e simples. Usado para mapas procedural sem dependências externas.
/// </summary>
public sealed class PerlinNoise
{
    private readonly int[] _perm = new int[512];

    public PerlinNoise(int seed = 1337)
    {
        var p = Enumerable.Range(0, 256).ToArray();
        var rng = new Random(seed);
        for (var i = p.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }
        for (var i = 0; i < 512; i++) _perm[i] = p[i & 255];
    }

    public float Noise(float x, float y)
    {
        var xi = (int)MathF.Floor(x) & 255;
        var yi = (int)MathF.Floor(y) & 255;
        var xf = x - MathF.Floor(x);
        var yf = y - MathF.Floor(y);

        var u = Fade(xf);
        var v = Fade(yf);

        var aa = _perm[_perm[xi] + yi];
        var ab = _perm[_perm[xi] + yi + 1];
        var ba = _perm[_perm[xi + 1] + yi];
        var bb = _perm[_perm[xi + 1] + yi + 1];

        var x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
        var x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
        return (Lerp(x1, x2, v) + 1f) * 0.5f;
    }

    public float Fractal(float x, float y, int octaves = 4, float persistence = 0.5f, float lacunarity = 2f)
    {
        octaves = Math.Clamp(octaves, 1, 12);
        var amp = 1f;
        var freq = 1f;
        var sum = 0f;
        var max = 0f;
        for (var i = 0; i < octaves; i++)
        {
            sum += Noise(x * freq, y * freq) * amp;
            max += amp;
            amp *= persistence;
            freq *= lacunarity;
        }
        return max <= 0f ? 0f : sum / max;
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
    private static float Lerp(float a, float b, float t) => a + t * (b - a);
    private static float Grad(int hash, float x, float y)
    {
        return (hash & 3) switch
        {
            0 =>  x + y,
            1 => -x + y,
            2 =>  x - y,
            _ => -x - y,
        };
    }
}
