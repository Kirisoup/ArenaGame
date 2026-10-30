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
    bool IsObstacle,
    Monster.MonsterMovement Movement,
    Monster.FnOnHit? OnHit = null)
    : Thingy.Ticking<Monster>, Thingy.Rendering<Monster>
{
    static void Thingy.Rendering<Monster>.Render(Monster self, float dt) 
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

    static void Thingy.Ticking<Monster>.Tick(ref Monster self, float dt, Action<Monster>? addMore,
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

            Player.Val.Accuracy.Total ++;
            if (!self.IsObstacle) {
                Player.Val.Accuracy.Hits ++;
                Player.Val.Combo ++;
            }

            Bullet.RemoveAt(i);
            i --;
            
            self.Health --;
            self.OnHit?.Invoke(ref self);

            // hit particles
            Particle.SpawnConfetti(
                count: self.Health > 0 ? 2 : 10,
                color: self.Color,
                position: bullet.Position,
                baseSpeed: 1f * Game.TARGET_FPS,
                externalVelocity: self.Health > 0
                    ? bullet.Velocity * -0.25f
                    : bullet.Velocity * 0.5f,
                baseLifetime: 0.25f,
                lifetimeVariation: 0.75f
            );

            // Screen shake
            if (self.IsObstacle) Shake.Val.MissedShake(self.Position);
            else Shake.Val.HitShake(self.Position);

            if (self.Health <= 0)
            {
                removeSelf = true;
                if (!self.IsObstacle) Player.Val.Score ++;
                Raylib.PlaySound(Game.Instance.SoundKill);
                break;
            }
        }
    }
}

