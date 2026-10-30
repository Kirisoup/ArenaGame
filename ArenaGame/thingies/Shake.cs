using System.Numerics;

namespace ArenaGame.thingies;

public record struct Shake(
    float Timer,
    Vector2 Dir,
    Vector2 Offset
) : Thingy.Ticking.Immutable<Shake> {
    static Shake() => Shake.Val = new();

    public const float DURATION = 0.5f;

    public static void Tick(ref Shake self, float dt)
    {
        self.Timer = MathF.Max(self.Timer - dt, 0.0f);
        self.Offset = self.Dir * MathF.Sin(self.Timer / DURATION * 2 * MathF.PI) * self.Timer * 24;
    }
    static void Thingy<Shake>.HandleReset(out bool shouldClear) {
        shouldClear = false;
        Shake.Val = new();
    }

    public void ShakeTowards(Vector2 dir) {
        this.Timer = MathF.Min(this.Timer + Shake.DURATION, 4.0f);
        this.Dir = dir;
    }
}
