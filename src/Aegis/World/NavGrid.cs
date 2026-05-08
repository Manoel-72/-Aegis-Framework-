using Microsoft.Xna.Framework;

namespace Aegis.World;

/// <summary>
/// Grid de navegação com A* otimizado por binary heap.
/// Células sólidas bloqueiam caminho. Retorna pontos em coordenadas de mundo, no centro da célula.
/// </summary>
public sealed class NavGrid
{
    private readonly bool[] _solid;

    public int Width { get; }
    public int Height { get; }
    public int CellSize { get; }
    public bool AllowDiagonal { get; set; }

    public NavGrid(int width, int height, int cellSize, bool allowDiagonal = false)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        CellSize = Math.Max(1, cellSize);
        AllowDiagonal = allowDiagonal;
        _solid = new bool[Width * Height];
    }

    public void SetSolid(int x, int y, bool solid)
    {
        if (!InBounds(x, y)) return;
        _solid[y * Width + x] = solid;
    }

    public bool IsSolid(int x, int y) => !InBounds(x, y) || _solid[y * Width + x];

    public List<Vector2>? FindPath(Vector2 from, Vector2 to)
    {
        var sx = (int)MathF.Floor(from.X / CellSize);
        var sy = (int)MathF.Floor(from.Y / CellSize);
        var gx = (int)MathF.Floor(to.X / CellSize);
        var gy = (int)MathF.Floor(to.Y / CellSize);

        if (!InBounds(sx, sy) || !InBounds(gx, gy)) return null;
        if (IsSolid(sx, sy) || IsSolid(gx, gy)) return null;

        var count = Width * Height;
        var gScore = new float[count];
        var came = new int[count];
        var closed = new bool[count];
        Array.Fill(gScore, float.PositiveInfinity);
        Array.Fill(came, -1);

        int start = Index(sx, sy);
        int goal = Index(gx, gy);
        gScore[start] = 0f;

        var open = new MinHeap();
        open.Push(start, Heuristic(sx, sy, gx, gy));

        while (open.Count > 0)
        {
            var current = open.Pop();
            if (closed[current]) continue;
            if (current == goal) return BuildPath(came, current);

            closed[current] = true;
            int cx = current % Width;
            int cy = current / Width;

            foreach (var n in Neighbors(cx, cy))
            {
                int nx = n.X, ny = n.Y;
                if (IsSolid(nx, ny)) continue;
                int ni = Index(nx, ny);
                if (closed[ni]) continue;

                var step = n.Diagonal ? 1.41421356f : 1f;
                var tentative = gScore[current] + step;
                if (tentative >= gScore[ni]) continue;

                came[ni] = current;
                gScore[ni] = tentative;
                var f = tentative + Heuristic(nx, ny, gx, gy);
                open.Push(ni, f);
            }
        }

        return null;
    }

    public static NavGrid FromTilemap(TilemapNode map, int[] solidGids, bool allowDiagonal = false)
    {
        var nav = new NavGrid(map.MapWidth, map.MapHeight, Math.Max(1, map.TileWidth), allowDiagonal);
        var set = new HashSet<int>(solidGids.Select(g => g & 0x1FFFFFFF));
        for (int y = 0; y < map.MapHeight; y++)
        for (int x = 0; x < map.MapWidth; x++)
        {
            if (map.AnyLayerHasSolidGid(x, y, set)) nav.SetSolid(x, y, true);
        }
        return nav;
    }

    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    private int Index(int x, int y) => y * Width + x;
    private float Heuristic(int x, int y, int gx, int gy)
    {
        var dx = MathF.Abs(gx - x);
        var dy = MathF.Abs(gy - y);
        return AllowDiagonal ? MathF.Max(dx, dy) : dx + dy;
    }

    private List<Vector2> BuildPath(int[] came, int current)
    {
        var reversed = new List<Vector2>();
        while (current >= 0)
        {
            int x = current % Width;
            int y = current / Width;
            reversed.Add(new Vector2(x * CellSize + CellSize * 0.5f, y * CellSize + CellSize * 0.5f));
            current = came[current];
        }
        reversed.Reverse();
        return reversed;
    }

    private readonly record struct Neighbor(int X, int Y, bool Diagonal);
    private IEnumerable<Neighbor> Neighbors(int x, int y)
    {
        yield return new Neighbor(x + 1, y, false);
        yield return new Neighbor(x - 1, y, false);
        yield return new Neighbor(x, y + 1, false);
        yield return new Neighbor(x, y - 1, false);
        if (!AllowDiagonal) yield break;

        // Evita cortar canto: diagonal só é válida se os dois lados ortogonais não forem sólidos.
        if (!IsSolid(x + 1, y) && !IsSolid(x, y + 1)) yield return new Neighbor(x + 1, y + 1, true);
        if (!IsSolid(x - 1, y) && !IsSolid(x, y + 1)) yield return new Neighbor(x - 1, y + 1, true);
        if (!IsSolid(x + 1, y) && !IsSolid(x, y - 1)) yield return new Neighbor(x + 1, y - 1, true);
        if (!IsSolid(x - 1, y) && !IsSolid(x, y - 1)) yield return new Neighbor(x - 1, y - 1, true);
    }

    private sealed class MinHeap
    {
        private readonly List<(int Node, float Priority)> _items = new();
        public int Count => _items.Count;

        public void Push(int node, float priority)
        {
            _items.Add((node, priority));
            SiftUp(_items.Count - 1);
        }

        public int Pop()
        {
            var root = _items[0].Node;
            var last = _items[^1];
            _items.RemoveAt(_items.Count - 1);
            if (_items.Count > 0)
            {
                _items[0] = last;
                SiftDown(0);
            }
            return root;
        }

        private void SiftUp(int i)
        {
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (_items[p].Priority <= _items[i].Priority) break;
                (_items[p], _items[i]) = (_items[i], _items[p]);
                i = p;
            }
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int l = i * 2 + 1, r = l + 1, s = i;
                if (l < _items.Count && _items[l].Priority < _items[s].Priority) s = l;
                if (r < _items.Count && _items[r].Priority < _items[s].Priority) s = r;
                if (s == i) break;
                (_items[s], _items[i]) = (_items[i], _items[s]);
                i = s;
            }
        }
    }
}
