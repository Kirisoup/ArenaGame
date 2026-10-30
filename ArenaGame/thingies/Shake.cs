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

    public void HitShake(Vector2 position)
    {
        if (this.Timer >= DURATION) this.Timer -= (this.Timer - 1f) / 2;
        else this.Timer = Math.Max(this.Timer + DURATION, DURATION * 1.25f);
        this.Dir = Vector2.Normalize(Player.Val.Position - position);
    }

    public void MissedShake(Vector2 position)
    {
        this.Timer += 0.5f + (4 - this.Timer) / 4f;
        this.Dir = Vector2.Normalize(position - Player.Val.Position);
    }
}
