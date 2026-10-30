using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;
using static Shared;

public record struct Bullet(
    Vector2 Position,
    Vector2 Velocity)
    : Thingy.Ticking<Bullet>, Thingy.Rendering<Bullet>
{
    public const float BASE_SPEED = 10 * Game.TARGET_FPS;

    private const int SIZE = 8;
    private static readonly Color COLOR = Color.Black;

    static void Thingy.Rendering<Bullet>.Render(Bullet self, float dt) 
    {
        DrawSquare(self.Position + Shake.Val.Offset, SIZE, COLOR);
    }

    static void Thingy.Ticking<Bullet>.Tick(ref Bullet self, float dt, Action<Bullet>? addMore,
        out bool removeSelf) 
    {
        self.Position += self.Velocity * dt;

        removeSelf = false;
        removeSelf = removeSelf || self.Position.X < -SIZE / 2.0f || self.Position.X > Raylib.GetScreenWidth()  + SIZE / 2.0f;
        removeSelf = removeSelf || self.Position.Y < -SIZE / 2.0f || self.Position.Y > Raylib.GetScreenHeight() + SIZE / 2.0f;
        if (removeSelf) {
            Shake.Val.MissedShake(self.Position);
            Player.Val.Combo = 0;
            Player.Val.Accuracy.Total ++;
        }
    }
}
