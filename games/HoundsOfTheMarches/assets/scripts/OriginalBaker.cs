using System.Globalization;
using System.IO;
using System.Text;

namespace Hounds.Marches;

/// <summary>
/// Writes the original art pack: thick-outline silhouettes, tiles, parallax, HUD, and placeholder tones.
/// Re-running keeps existing files unless <paramref name="force"/> is set.
/// </summary>
public static class OriginalAssetBaker
{
    private const int CellW = 180;
    private const int CellH = 220;
    private const int Frames = 10;

    private static readonly Rgba Ink = new(12, 10, 14);
    private static readonly Rgba Plate = new(176, 178, 184);
    private static readonly Rgba PlateDark = new(92, 96, 108);
    private static readonly Rgba Cloak = new(150, 40, 44);
    private static readonly Rgba CloakDark = new(96, 22, 28);
    private static readonly Rgba Ochre = new(186, 142, 70);
    private static readonly Rgba OchreDark = new(120, 82, 36);
    private static readonly Rgba Steel = new(214, 216, 222);
    private static readonly Rgba SteelDark = new(120, 124, 132);
    private static readonly Rgba Muzzle = new(126, 96, 74);
    private static readonly Rgba Cat = new(198, 132, 62);
    private static readonly Rgba CatDark = new(110, 62, 32);
    private static readonly Rgba Gold = new(214, 170, 74);
    private static readonly Rgba Night = new(18, 22, 34);
    private static readonly Rgba Cream = new(232, 220, 196);

    private static readonly (string Name, int[] Frames)[] Clips =
    [
        ("idle", [0]), ("run", [1, 2]), ("air", [3]),
        ("lightWindup", [3]), ("light", [4]),
        ("heavyWindup", [5]), ("heavy", [6]),
        ("dodge", [7]), ("block", [8]), ("hurt", [9]), ("recover", [4]),
        ("windup", [5]), ("active", [6]),
        ("slashWindup", [5]), ("slash", [6]),
        ("thrustWindup", [3]), ("thrust", [4]),
        ("leap", [7]), ("phase2", [8])
    ];

    public static void Ensure(string assetsRoot, bool force = false)
    {
        var marker = Path.Combine(assetsRoot, "Original", "characters", "player.png");
        if (!force && File.Exists(marker) && File.Exists(Path.Combine(assetsRoot, "audio", "hit.wav")))
            return;
        Bake(assetsRoot);
    }

    public static void Bake(string assetsRoot)
    {
        var original = Path.Combine(assetsRoot, "Original");
        Directory.CreateDirectory(original);
        BakeCharacters(original);
        BakeTiles(Path.Combine(original, "tiles"));
        BakeProps(Path.Combine(original, "props"));
        BakeBackgrounds(Path.Combine(original, "bg"));
        BakeHud(Path.Combine(original, "hud"));
        BakeTitles(Path.Combine(assetsRoot, "shared"));
        BakeAudio(Path.Combine(assetsRoot, "audio"));
        WritePackJson(original);
    }

    private static void BakeCharacters(string original)
    {
        var dir = Path.Combine(original, "characters");
        Directory.CreateDirectory(dir);
        string[] names = ["player", "swordsman", "halberdier", "boss"];
        for (var style = 0; style < names.Length; style++)
        {
            var sheet = new Raster(CellW * Frames, CellH);
            for (var frame = 0; frame < Frames; frame++)
                DrawActor(sheet, frame * CellW, 0, frame, style);
            var pngName = names[style] + ".png";
            sheet.Save(Path.Combine(dir, pngName));
            WriteSheetJson(Path.Combine(dir, names[style] + ".json"), "Original/characters/" + pngName);
        }
    }

    private static void DrawActor(Raster r, int ox, int oy, int frame, int style)
    {
        var lean = frame switch { 1 => 8, 2 => -6, 3 => -14, 4 => 14, 5 => -20, 6 => 16, 7 => 22, 9 => -16, _ => 0 };
        var crouch = frame switch { 6 => 6, 7 => 30, _ => 0 };
        var cx = ox + 74 + lean;
        var foot = oy + 204 - crouch;
        var wide = style == 2 ? 22 : style == 3 ? 18 : style == 0 ? 16 : 13;
        var hip = foot - 52;
        var shoulder = hip - 46;
        var headY = shoulder - 28;
        var body = style == 1 ? Cat : style == 2 ? new Rgba(70, 64, 58) : style == 3 ? new Rgba(48, 40, 38) : Plate;
        var bodyDark = style == 1 ? CatDark : style == 2 ? new Rgba(40, 36, 34) : style == 3 ? new Rgba(28, 22, 24) : PlateDark;
        var cloth = style is 1 or 2 ? new Rgba(28, 22, 20) : Cloak;
        var clothDark = style is 1 or 2 ? new Rgba(16, 12, 12) : CloakDark;

        // Cloak behind the body. Captain's phase pose (frame 8) opens it.
        var cloakW = style == 3 ? (frame == 8 ? 34 : 26) : style == 0 ? 22 : 8;
        if (cloakW > 8)
            r.Box(cx - cloakW - lean / 3, shoulder - 6, cloakW + wide, foot - shoulder - 8, cloth, clothDark);

        var legSpread = frame switch { 1 => 16, 2 => -14, 7 => 8, _ => 4 };
        r.Box(cx - 8 - legSpread, hip, 12, foot - hip, bodyDark, Ink);
        r.Box(cx - 2 + legSpread, hip, 12, foot - hip, body, bodyDark);

        r.Box(cx - wide, shoulder, wide * 2, hip - shoulder + 8, body, bodyDark);
        if (style == 0)
            r.Box(cx - wide + 4, shoulder + 8, wide * 2 - 10, 16, Ochre, OchreDark);

        // Head, muzzle or helm.
        r.Blob(cx + 4, headY, style == 2 ? 20 : 16, style == 2 ? 18 : 16, body, bodyDark);
        if (style == 0)
        {
            r.Blob(cx + 22, headY + 4, 14, 8, Muzzle, new Rgba(80, 58, 42));
            r.Tri(cx - 8, headY - 14, cx - 18, headY - 28, cx + 2, headY - 16, bodyDark);
            r.Tri(cx + 6, headY - 16, cx + 2, headY - 30, cx + 16, headY - 12, Plate);
        }
        else
        {
            r.Tri(cx - 10, headY - 10, cx - 4, headY - 32, cx + 2, headY - 10, CatDark);
            r.Tri(cx + 4, headY - 12, cx + 14, headY - 34, cx + 18, headY - 8, Cat);
            if (style != 2)
                r.Ellipse(cx + 18, headY + 2, 7, 5, Gold);
            r.Line(cx - 16, hip, cx - 28, foot - 8, 3, CatDark);
        }

        if (style == 3)
        {
            r.Ellipse(cx - 2, shoulder + 18, 8, 8, Gold);
            r.Tri(cx - 2, shoulder + 12, cx - 8, shoulder + 4, cx + 4, shoulder + 4, Ink);
        }

        var sword = style == 2 ? 78 : style == 3 ? 70 : 58;
        var thick = style == 2 ? 4 : 3;
        var handX = cx + wide - 2;
        var handY = shoulder + 16;
        var (tx, ty) = frame switch
        {
            3 => (handX - 36, handY - 48),
            4 => (handX + sword, handY - 4),
            5 => (handX - 10, handY - sword - 10),
            6 => (handX + sword - 8, handY + 36),
            7 => (handX + 20, handY + 10),
            9 => (handX - 20, handY + 20),
            _ => (handX + 10, foot - 16)
        };
        r.Line(handX, handY, tx, ty, thick, Steel);
        r.Line(handX, handY, tx, ty, System.Math.Max(1, thick - 2), SteelDark);
        if (style == 2 && frame is 5 or 6)
            r.Box(tx - 8, ty - 6, 18, 14, Steel, SteelDark);

        var shieldFront = frame == 8;
        if (style is 0 or 3)
        {
            var sx = shieldFront ? cx + wide + 4 : cx - wide - 22;
            r.Blob(sx, shoulder + 10, 12, 20, style == 3 ? Gold : new Rgba(120, 42, 40), OchreDark);
            r.Rect(sx - 4, shoulder + 4, 8, 12, Cream);
        }

        r.Rect(cx + 8, headY - 2, 8, 3, frame == 9 ? new Rgba(160, 40, 36) : Cream);
        r.Outline(ox + 2, oy + 2, CellW - 4, CellH - 4, 5, Ink);
    }

    private static void BakeTiles(string dir)
    {
        Directory.CreateDirectory(dir);
        var mud = new Raster(64, 64);
        mud.Clear(new Rgba(92, 64, 40));
        for (var i = 0; i < 18; i++)
            mud.Ellipse(8 + (i * 17) % 56, 10 + (i * 13) % 48, 4 + i % 3, 3, i % 2 == 0 ? new Rgba(120, 84, 48) : new Rgba(70, 48, 30));
        Edge(mud, new Rgba(40, 28, 18));
        mud.Save(Path.Combine(dir, "mud.png"));

        var stone = new Raster(64, 64);
        stone.Clear(new Rgba(92, 96, 104));
        stone.Rect(4, 4, 26, 18, new Rgba(140, 144, 150));
        stone.Rect(34, 8, 24, 16, new Rgba(120, 124, 132));
        stone.Rect(8, 30, 22, 26, new Rgba(128, 132, 140));
        stone.Rect(34, 32, 24, 24, new Rgba(108, 112, 120));
        Edge(stone, new Rgba(36, 38, 44));
        stone.Save(Path.Combine(dir, "stone.png"));

        var bridge = new Raster(64, 64);
        bridge.Clear(new Rgba(0, 0, 0, 0));
        for (var i = 0; i < 4; i++)
            bridge.Rect(4 + i * 15, 16, 12, 32, i % 2 == 0 ? new Rgba(120, 78, 42) : new Rgba(96, 62, 34));
        bridge.Rect(2, 14, 60, 6, new Rgba(70, 46, 28));
        bridge.Outline(0, 0, 64, 64, 2, Ink);
        bridge.Save(Path.Combine(dir, "bridge.png"));

        var palisade = new Raster(64, 64);
        palisade.Clear(new Rgba(0, 0, 0, 0));
        for (var i = 0; i < 5; i++)
            palisade.Rect(6 + i * 12, 4, 8, 56, i % 2 == 0 ? new Rgba(110, 78, 48) : new Rgba(86, 60, 36));
        palisade.Outline(0, 0, 64, 64, 3, Ink);
        palisade.Save(Path.Combine(dir, "palisade.png"));
    }

    private static void BakeProps(string dir)
    {
        Directory.CreateDirectory(dir);
        var puddle = new Raster(96, 48);
        puddle.Ellipse(48, 26, 40, 12, new Rgba(48, 70, 96, 180));
        puddle.Ellipse(40, 22, 16, 5, new Rgba(160, 180, 196, 120));
        puddle.Save(Path.Combine(dir, "puddle.png"));

        var ladder = new Raster(48, 128);
        ladder.Rect(6, 4, 6, 120, new Rgba(120, 84, 48));
        ladder.Rect(36, 4, 6, 120, new Rgba(96, 66, 38));
        for (var y = 16; y < 120; y += 16)
            ladder.Rect(8, y, 32, 5, OchreDark);
        ladder.Outline(0, 0, 48, 128, 3, Ink);
        ladder.Save(Path.Combine(dir, "ladder.png"));

        var gate = new Raster(120, 180);
        gate.Rect(8, 30, 18, 140, new Rgba(100, 102, 110));
        gate.Rect(94, 30, 18, 140, new Rgba(88, 90, 98));
        gate.Rect(16, 70, 88, 100, new Rgba(62, 58, 56));
        gate.Ellipse(60, 48, 48, 36, new Rgba(120, 122, 128));
        gate.Rect(48, 78, 24, 40, Night);
        gate.Outline(0, 0, 120, 180, 4, Ink);
        gate.Save(Path.Combine(dir, "gate.png"));

        var shrine = new Raster(96, 140);
        shrine.Rect(18, 70, 60, 60, new Rgba(110, 112, 118));
        shrine.Rect(28, 48, 40, 28, new Rgba(90, 92, 98));
        shrine.Ellipse(48, 36, 10, 16, new Rgba(210, 120, 40));
        shrine.Ellipse(48, 28, 6, 10, new Rgba(240, 200, 90));
        shrine.Outline(0, 0, 96, 140, 4, Ink);
        shrine.Save(Path.Combine(dir, "shrine.png"));

        var banner = new Raster(64, 120);
        banner.Rect(28, 2, 8, 110, new Rgba(70, 48, 32));
        banner.Box(8, 10, 48, 70, Cloak, CloakDark);
        banner.Tri(16, 28, 32, 18, 32, 40, Gold);
        banner.Outline(0, 0, 64, 120, 3, Ink);
        banner.Save(Path.Combine(dir, "banner.png"));

        var shield = new Raster(72, 96);
        shield.Blob(36, 44, 26, 36, new Rgba(130, 36, 40), CloakDark);
        shield.Tri(20, 30, 36, 16, 52, 30, Cream);
        shield.Outline(0, 0, 72, 96, 4, Ink);
        shield.Save(Path.Combine(dir, "shield.png"));

        var blood = new Raster(80, 48);
        blood.Ellipse(40, 28, 30, 12, new Rgba(90, 16, 22));
        blood.Ellipse(30, 26, 10, 6, new Rgba(140, 30, 32));
        blood.Outline(0, 0, 80, 48, 2, Ink);
        blood.Save(Path.Combine(dir, "blood.png"));

        var stinger = new Raster(48, 48);
        stinger.Tri(24, 6, 10, 40, 38, 40, Cream);
        stinger.Tri(24, 14, 18, 36, 30, 36, Gold);
        stinger.Outline(0, 0, 48, 48, 3, Ink);
        stinger.Save(Path.Combine(dir, "stinger.png"));
    }

    private static void BakeBackgrounds(string dir)
    {
        Directory.CreateDirectory(dir);
        var far = new Raster(640, 360);
        for (var y = 0; y < far.H; y++)
        {
            var t = y / (float)far.H;
            var c = new Rgba((byte)(18 + 20 * (1 - t)), (byte)(24 + 16 * (1 - t)), (byte)(46 + 10 * t));
            far.Rect(0, y, far.W, 1, c);
        }
        far.Ellipse(480, 70, 28, 28, new Rgba(210, 206, 186, 180));
        far.Ellipse(80, 250, 180, 50, new Rgba(40, 48, 62));
        far.Ellipse(360, 270, 220, 60, new Rgba(32, 40, 54));
        far.Save(Path.Combine(dir, "far.png"));

        var mid = new Raster(640, 360);
        mid.Ellipse(120, 250, 90, 70, new Rgba(48, 44, 40, 200));
        mid.Rect(90, 180, 50, 90, new Rgba(62, 50, 42, 210));
        mid.Tri(80, 190, 115, 140, 150, 190, new Rgba(90, 46, 40, 220));
        mid.Ellipse(400, 260, 140, 50, new Rgba(36, 40, 38, 180));
        mid.Rect(460, 160, 36, 120, new Rgba(50, 48, 46, 200));
        mid.Save(Path.Combine(dir, "mid.png"));

        var near = new Raster(640, 360);
        for (var i = 0; i < 9; i++)
        {
            var x = 20 + i * 70;
            near.Rect(x, 200, 14, 150, new Rgba(24, 22, 26, 210));
            near.Tri(x - 10, 210, x + 7, 170, x + 24, 210, new Rgba(30, 28, 32, 210));
        }
        near.Save(Path.Combine(dir, "near.png"));
    }

    private static void BakeHud(string dir)
    {
        Directory.CreateDirectory(dir);
        var heart = new Raster(48, 48);
        heart.Ellipse(16, 18, 10, 10, Cloak);
        heart.Ellipse(32, 18, 10, 10, Cloak);
        heart.Tri(6, 22, 42, 22, 24, 42, Cloak);
        heart.Outline(0, 0, 48, 48, 3, Ink);
        heart.Save(Path.Combine(dir, "heart.png"));

        for (var d = 0; d <= 9; d++)
        {
            var digit = new Raster(40, 48);
            Blit(digit, 8, 6, (char)('0' + d), 4, Cream);
            digit.Outline(0, 0, 40, 48, 2, Ink);
            digit.Save(Path.Combine(dir, $"d{d}.png"));
        }
    }

    private static void BakeTitles(string dir)
    {
        Directory.CreateDirectory(dir);
        (string file, string text)[] titles =
        [
            ("title_village", "ASHMERE"),
            ("title_bridge", "THE BRIDGE"),
            ("title_courtyard", "THE COURTYARD"),
            ("title_victory", "THE CAPTAIN FALLS"),
            ("title_rest", "SHRINE - E TO REST"),
            ("title_dead", "YOU FALL"),
            ("title_blood", "STINGERS RETURN"),
            ("title_help", "AD MOVE  J LIGHT  K HEAVY  SHIFT DODGE  L BLOCK  E REST"),
            ("title_phase", "THE CLOAK COMES OFF"),
            ("inscribe_oath", "HELD BY OATH, NOT MERCY"),
            ("inscribe_gate", "THE GILT CAPTAIN TOOK THE GATE")
        ];
        foreach (var (file, text) in titles)
            WriteTitle(Path.Combine(dir, file + ".png"), text);
    }

    private static void WriteTitle(string path, string text)
    {
        const int scale = 4;
        var gw = 6 * scale;
        var gh = 8 * scale;
        var raster = new Raster(System.Math.Max(8, text.Length * gw + scale * 2), gh + scale * 2);
        raster.Clear(new Rgba(16, 12, 14, 220));
        raster.Rect(0, 0, raster.W, 3, Gold);
        raster.Rect(0, raster.H - 3, raster.W, 3, Gold);
        for (var i = 0; i < text.Length; i++)
            Blit(raster, scale + i * gw, scale, text[i], scale, Cream);
        raster.Save(path);
    }

    private static void Blit(Raster raster, int x, int y, char ch, int scale, Rgba color)
    {
        var rows = Glyph(char.ToUpperInvariant(ch));
        for (var row = 0; row < rows.Length; row++)
        {
            var bits = rows[row];
            for (var col = 0; col < bits.Length; col++)
            {
                if (bits[col] != '1')
                    continue;
                raster.Rect(x + col * scale, y + row * scale, scale, scale, color);
            }
        }
    }

    private static void BakeAudio(string dir)
    {
        Directory.CreateDirectory(dir);
        WavFile.WriteTone(Path.Combine(dir, "hit.wav"), 180, 0.09f, 0.9f, true, 3);
        WavFile.WriteTone(Path.Combine(dir, "swing.wav"), 420, 0.07f, 0.5f, true, 9);
        WavFile.WriteTone(Path.Combine(dir, "parry.wav"), 880, 0.14f, 0.7f, false, 1);
        WavFile.WriteTone(Path.Combine(dir, "hurt.wav"), 196, 0.12f, 0.8f, false, 4);
        WavFile.WriteTone(Path.Combine(dir, "death.wav"), 110, 0.4f, 0.85f, false, 5);
        WavFile.WriteTone(Path.Combine(dir, "checkpoint.wav"), 660, 0.22f, 0.6f, false, 2);
    }

    private static void WritePackJson(string original)
    {
        var images = new StringBuilder();
        void Img(string key, string path)
        {
            if (images.Length > 0)
                images.Append(",\n");
            images.Append(CultureInfo.InvariantCulture, $"    \"{key}\": \"{path}\"");
        }

        Img("mud", "Original/tiles/mud.png");
        Img("stone", "Original/tiles/stone.png");
        Img("bridge", "Original/tiles/bridge.png");
        Img("palisade", "Original/tiles/palisade.png");
        Img("puddle", "Original/props/puddle.png");
        Img("ladder", "Original/props/ladder.png");
        Img("gate", "Original/props/gate.png");
        Img("shrine", "Original/props/shrine.png");
        Img("banner", "Original/props/banner.png");
        Img("shield", "Original/props/shield.png");
        Img("blood", "Original/props/blood.png");
        Img("coin", "Original/props/stinger.png");
        Img("heart", "Original/hud/heart.png");
        Img("parallax_far", "Original/bg/far.png");
        Img("parallax_mid", "Original/bg/mid.png");
        Img("parallax_near", "Original/bg/near.png");
        for (var d = 0; d <= 9; d++)
            Img($"digit{d}", $"Original/hud/d{d}.png");
        foreach (var key in new[]
                 {
                     "title_village", "title_bridge", "title_courtyard", "title_victory", "title_rest",
                     "title_dead", "title_blood", "title_help", "title_phase", "inscribe_oath", "inscribe_gate"
                 })
            Img(key, "shared/" + key + ".png");

        var json = "{\n  \"id\": \"original\",\n  \"sheets\": {\n" +
                   "    \"player\": \"Original/characters/player.json\",\n" +
                   "    \"swordsman\": \"Original/characters/swordsman.json\",\n" +
                   "    \"halberdier\": \"Original/characters/halberdier.json\",\n" +
                   "    \"boss\": \"Original/characters/boss.json\"\n" +
                   "  },\n  \"images\": {\n" + images + "\n  }\n}\n";
        File.WriteAllText(Path.Combine(original, "pack.json"), json);
    }

    private static void WriteSheetJson(string path, string texture)
    {
        var sb = new StringBuilder();
        sb.Append("{\n  \"animations\": {\n");
        for (var i = 0; i < Clips.Length; i++)
        {
            var clip = Clips[i];
            sb.Append(CultureInfo.InvariantCulture, $"    \"{clip.Name}\": [");
            for (var f = 0; f < clip.Frames.Length; f++)
            {
                if (f > 0)
                    sb.Append(", ");
                var index = clip.Frames[f];
                sb.Append(CultureInfo.InvariantCulture,
                    $"{{\"texture\": \"{texture}\", \"x\": {index * CellW}, \"y\": 0, \"w\": {CellW}, \"h\": {CellH}}}");
            }
            sb.Append(']');
            if (i + 1 < Clips.Length)
                sb.Append(',');
            sb.Append('\n');
        }
        sb.Append("  }\n}\n");
        File.WriteAllText(path, sb.ToString());
    }

    private static void Edge(Raster raster, Rgba color)
    {
        raster.Rect(0, 0, raster.W, 3, color);
        raster.Rect(0, raster.H - 3, raster.W, 3, color);
        raster.Rect(0, 0, 3, raster.H, color);
        raster.Rect(raster.W - 3, 0, 3, raster.H, color);
    }

    private static string[] Glyph(char c) => c switch
    {
        'A' => ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
        'B' => ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
        'C' => ["01110", "10001", "10000", "10000", "10000", "10001", "01110"],
        'D' => ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
        'E' => ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        'F' => ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
        'G' => ["01110", "10001", "10000", "10111", "10001", "10001", "01111"],
        'H' => ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
        'I' => ["01110", "00100", "00100", "00100", "00100", "00100", "01110"],
        'J' => ["00111", "00010", "00010", "00010", "00010", "10010", "01100"],
        'K' => ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
        'L' => ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
        'M' => ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
        'N' => ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
        'O' => ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
        'P' => ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
        'Q' => ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
        'R' => ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
        'S' => ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
        'T' => ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        'U' => ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
        'V' => ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
        'W' => ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
        'X' => ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
        'Y' => ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
        'Z' => ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
        '0' => ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
        '1' => ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
        '2' => ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
        '3' => ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
        '4' => ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
        '5' => ["11111", "10000", "11110", "00001", "00001", "10001", "01110"],
        '6' => ["00110", "01000", "10000", "11110", "10001", "10001", "01110"],
        '7' => ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
        '8' => ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
        '9' => ["01110", "10001", "10001", "01111", "00001", "00010", "01100"],
        '-' => ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
        ',' => ["00000", "00000", "00000", "00000", "00100", "00100", "01000"],
        _ => ["00000", "00000", "00000", "00000", "00000", "00000", "00000"]
    };
}
