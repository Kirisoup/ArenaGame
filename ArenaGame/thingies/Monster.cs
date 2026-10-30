using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;
using static Shared;

public partial record struct Monster(
    Vector2 Position,
    int Health,
    int Size,
    Color Color,
    int Speed,
    Monster.MonsterMovement Movement,
    Monster.FnOnHit? OnHit = null)
    : Thingy.Ticking<Monster>, Thingy.Rendering<Monster>
{
    public static void Render(Monster self, float dt) 
    {
        DrawSquare(self.Position + Shake.Val.Offset, self.Size, self.Color);
    }

    public static Vector2 GetRandomPosOnEdge()
    {
        var side = Raylib.GetRandomValue(0, 3);
        var x = Raylib.GetRandomValue(0, Raylib.GetScreenWidth());
        var y = Raylib.GetRandomValue(0, Raylib.GetScreenHeight());

        Vector2 pos = side switch
        {
            0 => new(0, y),
            1 => new(x, 0),
            2 => new(Raylib.GetScreenWidth(), y),
            3 => new(x, Raylib.GetScreenHeight()),
            _ => new(0, 0)
        };

        return pos;
    }

    public static Vector2 GetDirectionTowardsPlayer(Vector2 position)
    {
        return Vector2.Normalize(Player.Val.Position - position);
    }

    public static void Tick(ref Monster self, float dt, Action<Monster>? addMore,
        out bool removeSelf) 
    {
        removeSelf = false;

        var delta = self.Movement.kind switch
        {
            MonsterMovement.Kind.Homing => GetDirectionTowardsPlayer(self.Position),
            MonsterMovement.Kind.Direct => self.Movement.direction,
            _ => default, // unreachable
        };
        delta *= self.Speed * dt;
        self.Position += delta;
        
        for (int i = 0; i < Bullet.Span.Length; i++)
        {
            var bullet = Bullet.Span[i];
            if (!IsPointInsideSquare(bullet.Position, (self.Position, self.Size))) continue;
            
            Bullet.RemoveAt(i);
            i --;
            
            self.Health --;
            self.OnHit?.Invoke(ref self);

            // hit particles
            var particleCount = self.Health > 0 ? 2 : 10;
            for (int j = 0; j < particleCount; j++) 
            {
                var angle = Raylib.GetRandomValue(0, 359) * Raylib.DEG2RAD;
                var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                velocity *= 1f + Raylib.GetRandomValue(0, 256) / 256f;
                velocity *= 1f * Game.TARGET_FPS;
                if (self.Health > 0) velocity += bullet.Velocity * -0.25f;
                else velocity += bullet.Velocity * 0.5f;

                Particle.Add(new Particle(
                    bullet.Position,
                    velocity,
                    self.Color,
                    Lifetime: 0.5f + Raylib.GetRandomValue(0, 256) / 256f * 1f
                ));
            }

            // Screen shake
            if (Shake.Val.Timer >= Shake.DURATION) Shake.Val.Timer -= (Shake.Val.Timer - 1f) / 2;
            else Shake.Val.Timer = Math.Max(Shake.Val.Timer + Shake.DURATION, Shake.DURATION * 1.25f);
            Shake.Val.Dir = Vector2.Normalize(Player.Val.Position - self.Position);

            if (self.Health <= 0)
            {
                removeSelf = true;
                Player.Val.Score ++;
                Player.Val.Combo ++;
                Raylib.PlaySound(Game.Instance.SoundKill);
                break;
            }
        }
    }
}

