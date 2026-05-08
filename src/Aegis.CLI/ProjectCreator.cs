namespace Aegis.CLI;

public static class ProjectCreator
{
    public static void Create(string name)
    {
        if (Directory.Exists(name))
        {
            Console.WriteLine($"[Aegis] Pasta '{name}' já existe.");
            return;
        }

        // Pastas
        Directory.CreateDirectory(name);
        Directory.CreateDirectory(Path.Combine(name, "res", "sprites"));
        Directory.CreateDirectory(Path.Combine(name, "res", "audio"));
        Directory.CreateDirectory(Path.Combine(name, "src"));

        // aegis.toml
        File.WriteAllText(Path.Combine(name, "aegis.toml"), $"""
[game]
title  = "{name}"
width  = 1280
height = 720
fps    = 60
entry  = "main.lua"
""");

        // .gitignore
        File.WriteAllText(Path.Combine(name, ".gitignore"), "bin/\nobj/\n.vs/\n*.user\n");

        // main.lua (jogo de exemplo completo)
        File.WriteAllText(Path.Combine(name, "main.lua"), """
-- ================================================================
--  Aegis Engine — main.lua
--  Exemplo: movimentação, colisão com borda e score
-- ================================================================

local player  = nil
local speed   = 250       -- pixels por segundo

-- ----------------------------------------------------------------
--  aegis_init: chamado uma vez ao iniciar o jogo
-- ----------------------------------------------------------------
function aegis_init()
    -- Cria o sprite do jogador (quadrado azul 48x48)
    player = aegis.newRect(48, 48, 0.26, 0.53, 1.0)

    -- Centraliza na tela
    local cx = (aegis.screenWidth()  - aegis.getWidth(player))  / 2
    local cy = (aegis.screenHeight() - aegis.getHeight(player)) / 2
    aegis.setPosition(player, cx, cy)

    aegis.log("Jogo iniciado! Use WASD ou Setas para mover.")
end

-- ----------------------------------------------------------------
--  aegis_update: chamado todo frame  (dt = delta time em segundos)
-- ----------------------------------------------------------------
function aegis_update(dt)
    local px = aegis.getX(player)
    local py = aegis.getY(player)
    local pw = aegis.getWidth(player)
    local ph = aegis.getHeight(player)

    -- Movimento 4-direções
    if aegis.keyDown("Right") or aegis.keyDown("D") then px = px + speed * dt end
    if aegis.keyDown("Left")  or aegis.keyDown("A") then px = px - speed * dt end
    if aegis.keyDown("Down")  or aegis.keyDown("S") then py = py + speed * dt end
    if aegis.keyDown("Up")    or aegis.keyDown("W") then py = py - speed * dt end

    -- Clamp dentro da tela
    px = math.max(0, math.min(aegis.screenWidth()  - pw, px))
    py = math.max(0, math.min(aegis.screenHeight() - ph, py))

    aegis.setPosition(player, px, py)
end

-- ----------------------------------------------------------------
--  aegis_draw: chamado todo frame após o update (desenho extra)
-- ----------------------------------------------------------------
function aegis_draw()
    -- Espaço reservado para debug draw, HUD customizado, etc.
end
""");

        // Sprite player.png embutida como PNG mínimo 48x48
        GenerateColorPng(Path.Combine(name, "res", "sprites", "player.png"), 48, 48, 66, 135, 245);

        // README
        File.WriteAllText(Path.Combine(name, "README.md"), $"""
# {name}

Criado com **Aegis Engine v0.1.0** — motor 2D Lua para .NET 8.

## Rodar

```bash
cd {name}
aegis run
```

## Controles do exemplo
| Tecla | Ação |
|-------|------|
| WASD / Setas | Mover jogador |

## Estrutura
```
{name}/
├── main.lua          ← lógica do jogo em Lua
├── aegis.toml        ← configurações (título, resolução)
├── res/
│   └── sprites/      ← imagens PNG
└── src/              ← scripts Lua extras
```

## API Aegis (Lua)
```lua
-- Objetos
aegis.newSprite(path)           -- sprite de arquivo
aegis.newRect(w, h, r, g, b)    -- retângulo colorido
aegis.removeObject(obj)

-- Transformação
aegis.setPosition(obj, x, y)
aegis.move(obj, dx, dy)
aegis.setScale(obj, sx, sy)
aegis.setRotation(obj, rad)
aegis.setAlpha(obj, 0..1)
aegis.setVisible(obj, bool)
aegis.getX(obj) / getY(obj)
aegis.getWidth(bmp) / getHeight(bmp)

-- Input
aegis.keyDown(key)              -- ex: "Right","A","Space"
aegis.keyPressed(key)           -- só no frame que pressionou
aegis.mouseX() / mouseY()
aegis.mouseLeft() / mouseLeftJust()

-- Tela
aegis.screenWidth() / screenHeight()

-- Utils
aegis.log(msg)
aegis.randomInt(min, max)
aegis.randomFloat(min, max)
```
""");

        Console.WriteLine($"""
[Aegis] Projeto '{name}' criado!

  cd {name}
  aegis run
""");
    }

    // Gera PNG sólido minimal (sem dependências externas)
    private static void GenerateColorPng(string path, int w, int h, byte r, byte g, byte b)
    {
        using var ms = new System.IO.MemoryStream();
        // PNG signature
        ms.Write(new byte[] { 137,80,78,71,13,10,26,10 });
        // IHDR
        WriteChunk(ms, "IHDR", BuildIHDR(w, h));
        // IDAT — scanlines sem filtro
        var raw = new byte[h * (1 + w * 3)];
        for (int y = 0; y < h; y++)
        {
            raw[y * (1 + w * 3)] = 0; // filtro None
            for (int x = 0; x < w; x++)
            {
                raw[y * (1 + w * 3) + 1 + x * 3 + 0] = r;
                raw[y * (1 + w * 3) + 1 + x * 3 + 1] = g;
                raw[y * (1 + w * 3) + 1 + x * 3 + 2] = b;
            }
        }
        var deflated = Deflate(raw);
        WriteChunk(ms, "IDAT", deflated);
        WriteChunk(ms, "IEND", Array.Empty<byte>());
        File.WriteAllBytes(path, ms.ToArray());
    }

    private static byte[] BuildIHDR(int w, int h)
    {
        var b = new byte[13];
        void W4(int off, int v) {
            b[off]=((byte)(v>>24)); b[off+1]=((byte)(v>>16));
            b[off+2]=((byte)(v>>8)); b[off+3]=((byte)v);
        }
        W4(0, w); W4(4, h);
        b[8]=8; b[9]=2; b[10]=0; b[11]=0; b[12]=0; // 8-bit RGB
        return b;
    }

    private static byte[] Deflate(byte[] data)
    {
        using var out_ = new System.IO.MemoryStream();
        using (var ds = new System.IO.Compression.DeflateStream(
            out_, System.IO.Compression.CompressionLevel.Fastest))
            ds.Write(data, 0, data.Length);
        // zlib wrapper: CMF + FLG + deflate + Adler32
        var deflated = out_.ToArray();
        var zlib = new byte[deflated.Length + 6];
        zlib[0] = 0x78; zlib[1] = 0x9C;
        Array.Copy(deflated, 0, zlib, 2, deflated.Length);
        uint a = Adler32(data);
        zlib[^4]=(byte)(a>>24); zlib[^3]=(byte)(a>>16);
        zlib[^2]=(byte)(a>>8);  zlib[^1]=(byte)a;
        return zlib;
    }

    private static uint Adler32(byte[] data)
    {
        uint s1=1, s2=0;
        foreach (var b in data) { s1=(s1+b)%65521; s2=(s2+s1)%65521; }
        return (s2<<16)|s1;
    }

    private static void WriteChunk(System.IO.Stream s, string type, byte[] data)
    {
        var t = System.Text.Encoding.ASCII.GetBytes(type);
        Write4(s, data.Length);
        s.Write(t);
        s.Write(data);
        uint crc = Crc32(t, data);
        Write4(s, (int)crc);
    }

    private static void Write4(System.IO.Stream s, int v) =>
        s.Write(new byte[]{ (byte)(v>>24),(byte)(v>>16),(byte)(v>>8),(byte)v });

    private static uint Crc32(byte[] a, byte[] b)
    {
        uint c = 0xFFFFFFFF;
        foreach (var x in a) c = (c>>8)^_crcTable[(c^x)&0xFF];
        foreach (var x in b) c = (c>>8)^_crcTable[(c^x)&0xFF];
        return c^0xFFFFFFFF;
    }

    private static readonly uint[] _crcTable = BuildCrcTable();
    private static uint[] BuildCrcTable()
    {
        var t = new uint[256];
        for (uint i=0;i<256;i++){
            uint c=i;
            for (int k=0;k<8;k++) c=(c&1)==1?(0xEDB88320^(c>>1)):(c>>1);
            t[i]=c;
        }
        return t;
    }
}
