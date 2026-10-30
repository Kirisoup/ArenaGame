using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;

public static class Shared {
    public static void DrawSquare(Vector2 position, int size, Color color) {
        Raylib.DrawRectangle(
            (int) position.X - size / 2, (int) position.Y - size / 2,
            size, size,
            color);
    }

    public static bool IsPointInsideSquare(
        Vector2 point,
        (Vector2 position, int size) square
    ) =>
        point.X >= square.position.X - square.size * 0.5f && point.X <= square.position.X + square.size * 0.5f &&
        point.Y >= square.position.Y - square.size * 0.5f && point.Y <= square.position.Y + square.size * 0.5f;
}
