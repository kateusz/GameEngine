namespace Platformer.assets.scripts;

/// <summary>One frame of player intent. Held keys stay true; <see cref="JumpPressed"/> is the rising edge.</summary>
public readonly struct PlatformerInput
{
    public bool Left { get; init; }
    public bool Right { get; init; }
    public bool JumpPressed { get; init; }
    public bool JumpHeld { get; init; }
}

/// <summary>What <see cref="PlatformerSimulation.Tick"/> changed, so audio and tests can react.</summary>
public struct PlatformerEvents
{
    public bool Jumped;
    public bool Coin;
    public bool Stomped;
    public bool Hurt;
    public bool Won;
    public bool Died;
}

/// <summary>
/// Tile AABB platformer. No engine types — unit tests call <see cref="Reset"/> and <see cref="Tick"/> directly.
/// X is resolved before Y. Item blocks bump only on an upward hit.
/// </summary>
public static class PlatformerSimulation
{
    private const float Skin = 0.001f;
    private const int MaxOverlap = 16;

    public static void Reset(PlatformerGameComponent game)
    {
        EnsureArrays(game);
        if (game.Template is not { Length: > 0 })
            game.Template = PlatformerGameComponent.DefaultLevel();

        game.Rows = (string[])game.Template.Clone();
        game.Phase = PlatformerGameComponent.Ready;
        game.Coins = 0;
        game.Score = 0;
        game.Lives = 3;
        game.VelX = 0f;
        game.VelY = 0f;
        game.Facing = 1;
        game.Grounded = false;
        game.Invuln = 0f;
        game.EnemyCount = 0;
        Array.Clear(game.EnemyAlive);
        Array.Clear(game.EnemyVelX);
        Array.Clear(game.EnemyVelY);

        var spawnFound = false;
        for (var row = 0; row < game.Rows.Length; row++)
        {
            var line = game.Rows[row];
            for (var col = 0; col < line.Length; col++)
            {
                var ch = line[col];
                if (ch == 'P')
                {
                    TileCenter(game, col, row, out var x, out var y);
                    game.SpawnX = x;
                    game.SpawnY = y;
                    spawnFound = true;
                    SetTile(game, col, row, '.');
                }
                else if (ch == 'G' && game.EnemyCount < PlatformerGameComponent.MaxEnemies)
                {
                    var i = game.EnemyCount++;
                    TileCenter(game, col, row, out var x, out var y);
                    game.EnemyX[i] = x;
                    game.EnemyY[i] = y;
                    game.EnemyVelX[i] = -game.EnemySpeed;
                    game.EnemyVelY[i] = 0f;
                    game.EnemyAlive[i] = true;
                    SetTile(game, col, row, '.');
                }
            }
        }

        if (!spawnFound)
        {
            game.SpawnX = game.TileSize;
            game.SpawnY = game.TileSize * 3f;
        }

        game.PlayerX = game.SpawnX;
        game.PlayerY = game.SpawnY;
        UpdateCamera(game);
    }

    public static PlatformerEvents Tick(PlatformerGameComponent game, PlatformerInput input, float dt)
    {
        var events = new PlatformerEvents();
        if (game.Rows is not { Length: > 0 })
            return events;

        if (dt < 0f)
            dt = 0f;
        if (dt > 0.05f)
            dt = 0.05f;

        if (game.Phase == PlatformerGameComponent.Ready)
        {
            if (input.JumpPressed || input.Left || input.Right)
                game.Phase = PlatformerGameComponent.Playing;
            else
            {
                UpdateCamera(game);
                return events;
            }
        }

        if (game.Phase != PlatformerGameComponent.Playing)
        {
            UpdateCamera(game);
            return events;
        }

        if (game.Invuln > 0f)
            game.Invuln -= dt;

        var wish = 0f;
        if (input.Left)
            wish -= 1f;
        if (input.Right)
            wish += 1f;

        if (wish != 0f)
        {
            game.Facing = wish > 0f ? 1 : -1;
            game.VelX = MoveTowards(game.VelX, wish * game.RunSpeed, game.MoveAccel * dt);
        }
        else
        {
            var friction = game.Grounded ? game.GroundFriction : game.GroundFriction * 0.25f;
            game.VelX = MoveTowards(game.VelX, 0f, friction * dt);
        }

        var jumped = false;
        if (input.JumpPressed && game.Grounded)
        {
            game.VelY = game.JumpSpeed;
            game.Grounded = false;
            jumped = true;
            events.Jumped = true;
        }
        else if (!input.JumpHeld && game.VelY > game.JumpCut)
        {
            game.VelY = game.JumpCut;
        }

        game.VelY -= game.Gravity * dt;
        if (game.VelY < -game.MaxFall)
            game.VelY = -game.MaxFall;

        var playerX = game.PlayerX;
        var playerY = game.PlayerY;
        var velX = game.VelX;
        var velY = game.VelY;
        MoveHorizontal(game, ref playerX, playerY, ref velX, game.PlayerHalfW, game.PlayerHalfH, velX * dt);
        var vyBefore = velY;
        var landed = MoveVertical(game, playerX, ref playerY, ref velY, game.PlayerHalfW, game.PlayerHalfH, velY * dt, bump: true, ref events);
        if (jumped)
        {
            game.Grounded = false;
        }
        else
        {
            game.Grounded = landed || ProbeGround(game, playerX, playerY, game.PlayerHalfW, game.PlayerHalfH);
            if (game.Grounded && velY <= 0f)
            {
                SnapDown(game, ref playerY, playerX, game.PlayerHalfW, game.PlayerHalfH);
                velY = 0f;
            }
        }

        game.PlayerX = playerX;
        game.PlayerY = playerY;
        game.VelX = velX;
        game.VelY = velY;

        Collect(game, ref events);
        if (game.Phase != PlatformerGameComponent.Playing)
        {
            UpdateCamera(game);
            return events;
        }

        StepEnemies(game, dt);
        ResolvePlayerEnemies(game, vyBefore, ref events);

        if (game.Phase == PlatformerGameComponent.Playing && game.PlayerY < -2f)
            Hurt(game, ref events, ignoreInvuln: true);

        UpdateCamera(game);
        return events;
    }

    public static void UpdateCamera(PlatformerGameComponent game)
    {
        var halfH = game.OrthoSize;
        var halfW = game.OrthoSize * game.Aspect;
        game.CamX = ClampCamera(game.PlayerX, halfW, game.Width * game.TileSize);
        game.CamY = ClampCamera(game.PlayerY, halfH, game.Height * game.TileSize);
    }

    public static bool IsSolid(char ch) => ch is '#' or 'B' or '?' or 'U' or '=';

    public static char TileAt(PlatformerGameComponent game, int col, int row)
    {
        if (row < 0 || row >= game.Rows.Length)
            return '\0';
        var line = game.Rows[row];
        if (col < 0 || col >= line.Length)
            return '\0';
        return line[col];
    }

    public static bool SolidAt(PlatformerGameComponent game, float x, float y)
    {
        if (y < 0f)
            return false;
        if (x < 0f)
            return true;
        var col = (int)MathF.Floor(x / game.TileSize);
        var rowBottom = (int)MathF.Floor(y / game.TileSize);
        var row = game.Height - 1 - rowBottom;
        return IsSolid(TileAt(game, col, row));
    }

    public static void TileCenter(PlatformerGameComponent game, int col, int rowFromTop, out float x, out float y)
    {
        var size = game.TileSize;
        var rowBottom = game.Height - 1 - rowFromTop;
        x = (col + 0.5f) * size;
        y = (rowBottom + 0.5f) * size;
    }

    private static void StepEnemies(PlatformerGameComponent game, float dt)
    {
        var halfW = game.EnemyHalfW;
        var halfH = game.EnemyHalfH;
        for (var i = 0; i < game.EnemyCount; i++)
        {
            if (!game.EnemyAlive[i])
                continue;

            if (MathF.Abs(game.EnemyVelX[i]) < 0.01f)
                game.EnemyVelX[i] = -game.EnemySpeed;

            var dir = MathF.Sign(game.EnemyVelX[i]);
            game.EnemyVelY[i] -= game.Gravity * dt;
            if (game.EnemyVelY[i] < -game.MaxFall)
                game.EnemyVelY[i] = -game.MaxFall;

            var vx = game.EnemyVelX[i];
            var vy = game.EnemyVelY[i];
            var x = game.EnemyX[i];
            var y = game.EnemyY[i];
            var dummy = new PlatformerEvents();

            var hitWall = MoveHorizontal(game, ref x, y, ref vx, halfW, halfH, vx * dt);
            var landed = MoveVertical(game, x, ref y, ref vy, halfW, halfH, vy * dt, bump: false, ref dummy);
            var grounded = landed || ProbeGround(game, x, y, halfW, halfH);
            if (grounded && vy <= 0f)
            {
                SnapDown(game, ref y, x, halfW, halfH);
                vy = 0f;
            }

            if (hitWall)
            {
                vx = -dir * game.EnemySpeed;
            }
            else if (grounded && dir != 0f)
            {
                var probeX = x + dir * (halfW + 0.12f);
                var probeY = y - halfH - 0.08f;
                if (!SolidAt(game, probeX, probeY))
                    vx = -dir * game.EnemySpeed;
            }

            game.EnemyX[i] = x;
            game.EnemyY[i] = y;
            game.EnemyVelX[i] = vx;
            game.EnemyVelY[i] = vy;

            if (y < -3f)
                game.EnemyAlive[i] = false;
        }
    }

    private static void ResolvePlayerEnemies(PlatformerGameComponent game, float vyBefore, ref PlatformerEvents events)
    {
        var playerBottom = game.PlayerY - game.PlayerHalfH;
        for (var i = 0; i < game.EnemyCount; i++)
        {
            if (!game.EnemyAlive[i] || game.Phase != PlatformerGameComponent.Playing)
                continue;

            if (!Overlap(game.PlayerX, game.PlayerY, game.PlayerHalfW, game.PlayerHalfH,
                    game.EnemyX[i], game.EnemyY[i], game.EnemyHalfW, game.EnemyHalfH))
                continue;

            var enemyTop = game.EnemyY[i] + game.EnemyHalfH;
            var stomp = vyBefore < 0f
                        && playerBottom >= game.EnemyY[i] - 0.1f
                        && playerBottom <= enemyTop + 0.25f;
            if (stomp)
            {
                game.EnemyAlive[i] = false;
                game.VelY = game.StompBounce;
                game.Grounded = false;
                game.Score += game.StompScore;
                events.Stomped = true;
                playerBottom = game.PlayerY - game.PlayerHalfH;
                continue;
            }

            Hurt(game, ref events, ignoreInvuln: false);
        }
    }

    private static void Collect(PlatformerGameComponent game, ref PlatformerEvents events)
    {
        Span<TileHit> hits = stackalloc TileHit[MaxOverlap];
        var count = Overlaps(game, game.PlayerX, game.PlayerY, game.PlayerHalfW, game.PlayerHalfH, 0.02f, hits);
        for (var i = 0; i < count; i++)
        {
            if (game.Phase != PlatformerGameComponent.Playing)
                return;

            var hit = hits[i];
            if (hit.Ch == 'C')
            {
                SetTile(game, hit.Col, hit.Row, '.');
                game.Coins++;
                game.Score += game.CoinScore;
                events.Coin = true;
            }
            else if (hit.Ch == 'F')
            {
                game.Phase = PlatformerGameComponent.Won;
                game.VelX = 0f;
                game.VelY = 0f;
                events.Won = true;
                return;
            }
            else if (hit.Ch == '^')
            {
                Hurt(game, ref events, ignoreInvuln: true);
            }
        }
    }

    private static void Hurt(PlatformerGameComponent game, ref PlatformerEvents events, bool ignoreInvuln)
    {
        if (game.Phase != PlatformerGameComponent.Playing)
            return;
        if (!ignoreInvuln && game.Invuln > 0f)
            return;

        events.Hurt = true;
        game.Lives--;
        if (game.Lives <= 0)
        {
            game.Phase = PlatformerGameComponent.Dead;
            game.VelX = 0f;
            game.VelY = 0f;
            events.Died = true;
            return;
        }

        game.PlayerX = game.SpawnX;
        game.PlayerY = game.SpawnY;
        game.VelX = 0f;
        game.VelY = 0f;
        game.Grounded = false;
        game.Invuln = game.InvulnTime;
    }

    private static bool MoveHorizontal(
        PlatformerGameComponent game,
        ref float x,
        float y,
        ref float vx,
        float halfW,
        float halfH,
        float dx)
    {
        if (dx == 0f)
            return false;

        x += dx;
        var hit = false;
        if (x - halfW < 0f)
        {
            x = halfW + Skin;
            vx = 0f;
            hit = true;
        }

        Span<TileHit> hits = stackalloc TileHit[MaxOverlap];
        var count = Overlaps(game, x, y, halfW, halfH, 0.02f, hits);
        var limit = dx > 0f ? float.PositiveInfinity : float.NegativeInfinity;
        var found = false;
        var bodyBottom = y - halfH + 0.08f;
        var bodyTop = y + halfH - 0.02f;
        for (var i = 0; i < count; i++)
        {
            var tile = hits[i];
            if (!IsSolid(tile.Ch))
                continue;

            var overlap = MathF.Min(bodyTop, tile.Top) - MathF.Max(bodyBottom, tile.Bottom);
            if (overlap <= 0.02f)
                continue;

            if (dx > 0f)
            {
                if (tile.Left < limit)
                    limit = tile.Left;
            }
            else if (tile.Right > limit)
            {
                limit = tile.Right;
            }

            found = true;
        }

        if (!found)
            return hit;

        x = dx > 0f ? limit - halfW - Skin : limit + halfW + Skin;
        vx = 0f;
        return true;
    }

    private static bool MoveVertical(
        PlatformerGameComponent game,
        float x,
        ref float y,
        ref float vy,
        float halfW,
        float halfH,
        float dy,
        bool bump,
        ref PlatformerEvents events)
    {
        if (dy == 0f)
            return false;

        y += dy;
        Span<TileHit> hits = stackalloc TileHit[MaxOverlap];
        var count = Overlaps(game, x, y, halfW, halfH, 0.02f, hits);
        var limit = dy > 0f ? float.PositiveInfinity : float.NegativeInfinity;
        var found = false;
        var bodyLeft = x - halfW + 0.08f;
        var bodyRight = x + halfW - 0.08f;

        for (var i = 0; i < count; i++)
        {
            var tile = hits[i];
            if (!IsSolid(tile.Ch))
                continue;

            if (dy > 0f && tile.Top < y)
                continue;
            if (dy < 0f && tile.Bottom > y)
                continue;

            var overlap = MathF.Min(bodyRight, tile.Right) - MathF.Max(bodyLeft, tile.Left);
            if (overlap <= 0.02f)
                continue;

            if (dy > 0f)
            {
                if (tile.Bottom < limit)
                    limit = tile.Bottom;
            }
            else if (tile.Top > limit)
            {
                limit = tile.Top;
            }

            found = true;
            if (bump && dy > 0f && tile.Ch == '?')
            {
                SetTile(game, tile.Col, tile.Row, 'U');
                game.Coins++;
                game.Score += game.CoinScore;
                events.Coin = true;
            }
        }

        if (!found)
            return false;

        if (dy > 0f)
        {
            y = limit - halfH - Skin;
            vy = 0f;
            return false;
        }

        y = limit + halfH;
        vy = 0f;
        return true;
    }

    private static bool ProbeGround(PlatformerGameComponent game, float x, float y, float halfW, float halfH) =>
        SupportTop(game, x, y, halfW, halfH, out _);

    private static void SnapDown(PlatformerGameComponent game, ref float y, float x, float halfW, float halfH)
    {
        if (SupportTop(game, x, y, halfW, halfH, out var top))
            y = top + halfH;
    }

    private static bool SupportTop(PlatformerGameComponent game, float x, float y, float halfW, float halfH, out float top)
    {
        var feet = y - halfH;
        var sampleY = feet - 0.05f;
        var best = float.NegativeInfinity;
        var found = false;
        found |= ConsiderSupport(game, x - halfW + 0.1f, sampleY, feet, ref best);
        found |= ConsiderSupport(game, x, sampleY, feet, ref best);
        found |= ConsiderSupport(game, x + halfW - 0.1f, sampleY, feet, ref best);
        top = best;
        return found;
    }

    private static bool ConsiderSupport(PlatformerGameComponent game, float x, float sampleY, float feet, ref float best)
    {
        if (!TryTileAtWorld(game, x, sampleY, out var tile) || !IsSolid(tile.Ch))
            return false;
        if (tile.Top > feet + 0.02f || tile.Top < feet - 0.2f)
            return false;
        if (tile.Top > best)
            best = tile.Top;
        return true;
    }

    private static int Overlaps(
        PlatformerGameComponent game,
        float x,
        float y,
        float halfW,
        float halfH,
        float inset,
        Span<TileHit> output)
    {
        var left = x - halfW + inset;
        var right = x + halfW - inset;
        var bottom = y - halfH + inset;
        var top = y + halfH - inset;
        if (right <= left || top <= bottom)
            return 0;

        var size = game.TileSize;
        var c0 = (int)MathF.Floor(left / size);
        var c1 = (int)MathF.Floor((right - 0.0001f) / size);
        var b0 = (int)MathF.Floor(bottom / size);
        var b1 = (int)MathF.Floor((top - 0.0001f) / size);
        var count = 0;
        for (var col = c0; col <= c1; col++)
        {
            for (var rowBottom = b0; rowBottom <= b1; rowBottom++)
            {
                if (count >= output.Length)
                    return count;

                var row = game.Height - 1 - rowBottom;
                var ch = TileAt(game, col, row);
                var tileLeft = col * size;
                var tileBottom = rowBottom * size;
                output[count++] = new TileHit(col, row, ch, tileLeft, tileBottom, tileLeft + size, tileBottom + size);
            }
        }

        return count;
    }

    private static bool TryTileAtWorld(PlatformerGameComponent game, float x, float y, out TileHit tile)
    {
        tile = default;
        if (y < 0f || x < 0f)
            return false;

        var size = game.TileSize;
        var col = (int)MathF.Floor(x / size);
        var rowBottom = (int)MathF.Floor(y / size);
        var row = game.Height - 1 - rowBottom;
        var ch = TileAt(game, col, row);
        if (ch == '\0')
            return false;

        var left = col * size;
        var bottom = rowBottom * size;
        tile = new TileHit(col, row, ch, left, bottom, left + size, bottom + size);
        return true;
    }

    private static void SetTile(PlatformerGameComponent game, int col, int row, char ch)
    {
        if ((uint)row >= (uint)game.Rows.Length)
            return;
        var line = game.Rows[row];
        if ((uint)col >= (uint)line.Length || line[col] == ch)
            return;

        var chars = line.ToCharArray();
        chars[col] = ch;
        game.Rows[row] = new string(chars);
    }

    private static void EnsureArrays(PlatformerGameComponent game)
    {
        const int n = PlatformerGameComponent.MaxEnemies;
        if (game.EnemyX == null || game.EnemyX.Length < n)
            game.EnemyX = new float[n];
        if (game.EnemyY == null || game.EnemyY.Length < n)
            game.EnemyY = new float[n];
        if (game.EnemyVelX == null || game.EnemyVelX.Length < n)
            game.EnemyVelX = new float[n];
        if (game.EnemyVelY == null || game.EnemyVelY.Length < n)
            game.EnemyVelY = new float[n];
        if (game.EnemyAlive == null || game.EnemyAlive.Length < n)
            game.EnemyAlive = new bool[n];
    }

    private static float ClampCamera(float target, float halfView, float level)
    {
        var min = halfView;
        var max = level - halfView;
        if (max < min)
            return level * 0.5f;
        return System.Math.Clamp(target, min, max);
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        var delta = target - current;
        if (MathF.Abs(delta) <= maxDelta)
            return target;
        return current + MathF.Sign(delta) * maxDelta;
    }

    private static bool Overlap(float ax, float ay, float ahw, float ahh, float bx, float by, float bhw, float bhh) =>
        MathF.Abs(ax - bx) < ahw + bhw && MathF.Abs(ay - by) < ahh + bhh;

    private readonly struct TileHit(int col, int row, char ch, float left, float bottom, float right, float top)
    {
        public int Col { get; } = col;
        public int Row { get; } = row;
        public char Ch { get; } = ch;
        public float Left { get; } = left;
        public float Bottom { get; } = bottom;
        public float Right { get; } = right;
        public float Top { get; } = top;
    }
}
