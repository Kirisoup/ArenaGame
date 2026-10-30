using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;
using static Shared;

public record struct Particle(
    Vector2 Position,
    Vector2 Velocity,
    Color Color,
    float Lifetime)
    : Thingy.Ticking<Particle>, Thingy.Rendering<Particle>
{
    private const float DRAG = 0.6f;
    private const int SIZE = 6;

    static void Thingy.Rendering<Particle>.Render(Particle self, float dt) 
    {
        DrawSquare(self.Position + Shake.Val.Offset, SIZE, self.Color);
    }

    static void Thingy.Ticking<Particle>.Tick(ref Particle self, float dt, Action<Particle>? addMore,
            out bool removeSelf) 
    {
        self.Position += self.Velocity * dt;
        self.Velocity *= 1-((1-DRAG) * dt);
        self.Lifetime -= dt;
        removeSelf = self.Lifetime <= 0f;
    }

    public static void SpawnConfetti(
        int count, Color color, Vector2 position,
        float baseSpeed, Vector2 externalVelocity,
        float baseLifetime, float lifetimeVariation
    ) {
        for (int i = 0; i < count; i++) {
            var angle = Raylib.GetRandomValue(0, 359) * Raylib.DEG2RAD;
            var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            velocity *= 1f + Raylib.GetRandomValue(0, 256) / 256f;
            velocity *= baseSpeed;
            velocity += externalVelocity;
            Particle.Add(new Particle(
                position,
                velocity,
                color,
                Lifetime: baseLifetime + Raylib.GetRandomValue(0, 256) / 256f * lifetimeVariation
            ));
        }
    }
}
