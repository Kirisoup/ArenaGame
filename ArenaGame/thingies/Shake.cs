using System.Numerics;

namespace ArenaGame.thingies;

public record struct Shake(
    float Timer,
    Vector2 Dir,
    Vector2 Offset
) : Thingy.Ticking.Immutable<Shake> {
    public const float DURATION = 0.5f;

    static void Thingy<Shake>.Reset(Action clear)
    {
        Shake.Val = new();
    }

    static void Thingy.Ticking.Immutable<Shake>.Tick(ref Shake self, float dt)
    {
        self.Timer = MathF.Max(self.Timer - dt, 0.0f);
        self.Offset = self.Dir * MathF.Sin(self.Timer / DURATION * 2 * MathF.PI) * self.Timer * 24;
    }
    public void ShakeTowards(Vector2 dir) {
        this.Timer = MathF.Min(this.Timer + Shake.DURATION, 4.0f);
        this.Dir = dir;
    }
}
