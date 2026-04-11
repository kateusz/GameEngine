using System.Numerics;
using DoomGame.Map;

namespace DoomGame.Rendering;

public record RaycastHit(float Distance, int WallType, int Side, int MapX, int MapY);

public class RaycastRenderer
{
    private const int MaxSteps = 64;

    public RaycastHit[] CastRays(Vector2 playerPos, Vector2 playerDir, Vector2 cameraPlane, GameMap map, int screenWidth)
    {
        var hits = new RaycastHit[screenWidth];

        for (var x = 0; x < screenWidth; x++)
        {
            float cameraX = 2f * x / screenWidth - 1f;
            var rayDir = new Vector2(
                playerDir.X + cameraPlane.X * cameraX,
                playerDir.Y + cameraPlane.Y * cameraX);

            int mapX = (int)playerPos.X;
            int mapY = (int)playerPos.Y;

            float deltaDistX = rayDir.X == 0f ? float.MaxValue : MathF.Abs(1f / rayDir.X);
            float deltaDistY = rayDir.Y == 0f ? float.MaxValue : MathF.Abs(1f / rayDir.Y);

            int stepX, stepY;
            float sideDistX, sideDistY;

            if (rayDir.X < 0f) { stepX = -1; sideDistX = (playerPos.X - mapX) * deltaDistX; }
            else { stepX = 1; sideDistX = (mapX + 1f - playerPos.X) * deltaDistX; }

            if (rayDir.Y < 0f) { stepY = -1; sideDistY = (playerPos.Y - mapY) * deltaDistY; }
            else { stepY = 1; sideDistY = (mapY + 1f - playerPos.Y) * deltaDistY; }

            bool hit = false;
            int side = 0;

            for (int step = 0; step < MaxSteps && !hit; step++)
            {
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }

                int cellType = map.GetCell(mapX, mapY);
                if (cellType > 0)
                {
                    float perpWallDist = side == 0
                        ? sideDistX - deltaDistX
                        : sideDistY - deltaDistY;

                    hits[x] = new RaycastHit(MathF.Max(perpWallDist, 0.001f), cellType, side, mapX, mapY);
                    hit = true;
                }
            }

            if (!hit)
                hits[x] = new RaycastHit(100f, 1, 0, mapX, mapY);
        }

        return hits;
    }
}
