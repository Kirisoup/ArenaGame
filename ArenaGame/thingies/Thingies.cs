using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;

public static class Thingies {
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

public record struct Bullet(
    Vector2 Position,
    Vector2 Velocity)
    : Thingy.Ticking<Bullet>, Thingy.Rendering<Bullet>
{
    public const float BASE_SPEED = 10 * Game.TARGET_FPS;

    private const int SIZE = 8;
    private static readonly Color COLOR = Color.Black;

    public static void Render(Bullet self, float dt) 
    {
        Thingies.DrawSquare(self.Position + Shake.Val.Offset, SIZE, COLOR);
    }

    public static void Tick(ref Bullet self, float dt, Action<Bullet>? addMore,
        out bool removeSelf) 
    {
        self.Position += self.Velocity * dt;

        removeSelf = false;
        removeSelf = removeSelf || self.Position.X < -SIZE / 2.0f || self.Position.X > Raylib.GetScreenWidth()  + SIZE / 2.0f;
        removeSelf = removeSelf || self.Position.Y < -SIZE / 2.0f || self.Position.Y > Raylib.GetScreenHeight() + SIZE / 2.0f;
        if (removeSelf) {
            // Shake.Val.Timer = MathF.Min(Shake.Val.Timer + Shake.DURATION * 2f, 8.0f);
            Shake.Val.Timer += 0.5f + (4 - Shake.Val.Timer) / 4f;
            Shake.Val.Dir = Vector2.Normalize(self.Position - Player.Val.Position);
        }
    }
}

public record struct Particle(
    Vector2 Position,
    Vector2 Velocity,
    Color Color,
    float Lifetime)
    : Thingy.Ticking<Particle>, Thingy.Rendering<Particle>
{
    private const float DRAG = 0.6f;
    private const int SIZE = 6;

    public static void Render(Particle self, float dt) 
    {
        Thingies.DrawSquare(self.Position + Shake.Val.Offset, SIZE, self.Color);
    }

    public static void Tick(ref Particle self, float dt, Action<Particle>? addMore,
            out bool removeSelf) 
    {
        self.Position += self.Velocity * dt;
        self.Velocity *= 1-((1-DRAG) * dt);
        self.Lifetime -= dt;
        removeSelf = self.Lifetime <= 0f;
    }
}

public record struct Player(
    Vector2 Position,
    bool Dead = false,
    float ShotCooldown = 0f,
    int Score = 0)
    : Thingy.Ticking.Immutable<Player>, Thingy.Rendering<Player> 
{
    static Player() => Player.Val = new(new(400, 300));

    private const int SIZE = 32;
    private const int SPEED = 5 * Game.TARGET_FPS;
    private const float SHOT_TIME = 0.1f;
    private static readonly Color COLOR = Color.Red;

    public static void Render(Player self, float dt) 
    {
        Thingies.DrawSquare(self.Position + Shake.Val.Offset, SIZE, COLOR);
        Thingies.DrawSquare(self.Position + Shake.Val.Offset, 8, Color.White);
    }

    public static void Tick(ref Player self, float dt) 
    {
        var offset = new Vector2();
        if (Raylib.IsKeyDown(KeyboardKey.W)) offset.Y -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) offset.Y += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) offset.X -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) offset.X += 1f;

        if (offset.LengthSquared() > 0f)
        {
            offset = Vector2.Normalize(offset) * SPEED * dt;
        }

        self.Position += offset;
        self.Position = Vector2.Clamp(self.Position, 
            new Vector2(SIZE / 2.0f, SIZE / 2.0f),
            new Vector2(Raylib.GetScreenWidth() - SIZE / 2.0f, Raylib.GetScreenHeight() - SIZE / 2.0f));
        
        self.ShotCooldown = Math.Max(0f, self.ShotCooldown - dt);
        if (self.ShotCooldown == 0f && (Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsKeyPressed(KeyboardKey.Space)))
        {
            var mousePos = Raylib.GetMousePosition();
            var velocity = Vector2.Normalize(mousePos - self.Position) * Bullet.BASE_SPEED;
            Bullet.Add(new Bullet(self.Position, velocity));
            Raylib.PlaySound(Game.Instance.SoundShoot);
            self.ShotCooldown = SHOT_TIME;
        }

        foreach (var monster in Monster.Span)
        {
            if (!Thingies.IsPointInsideSquare(self.Position, (monster.Position, monster.Size))) continue;
            self.Dead = true;
            Raylib.PlaySound(Game.Instance.SoundDeath);
            if (self.Score > Game.Instance.highScore)
            {
                Game.Instance.highScore = self.Score;
                File.WriteAllText(Game.PLAYER_SCORE_FILE, self.Score.ToString());
            }

            break;
        }
    }

    static void Thingy<Player>.HandleReset(out bool shouldClear) {
        shouldClear = false;
        Player.Val.Dead = false;
        Player.Val.Position = new(400, 300);
        Player.Val.Score = 0;
    }
}

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
    public delegate void FnOnHit(ref Monster self);
    public readonly struct MonsterMovement {
        public enum Kind { Homing, Direct }

        public readonly Kind kind;
        public readonly Vector2 direction;
        private MonsterMovement(Kind kind, Vector2 direction = default)
        {
            this.kind = kind;
            this.direction = direction;
        }

        public static readonly MonsterMovement Homing = new(Kind.Homing);
        public static MonsterMovement Direct(Vector2 direction) => new(Kind.Direct, direction);
    }

    public static Monster Goblin(Vector2 position) => new(
        position,
        Health: 1,
        Size: 24,
        Color.Green,
        Speed: 4 * Game.TARGET_FPS,
        MonsterMovement.Homing
    );

    public static Monster ToughOrange(Vector2 position) => new(
        position,
        Health: 4,
        Size: 32,
        Color.Orange,
        Speed: 2 * Game.TARGET_FPS,
        MonsterMovement.Homing
    );

    public static Monster Funny2Hit(Vector2 position) => new(
        position,
        Health: 2,
        Size: 48,
        Color.Purple,
        Speed: 6 * Game.TARGET_FPS,
        MonsterMovement.Homing,
        OnHit: static (ref Monster self) => {
            self.Size = 24;
            self.Speed = 2 * Game.TARGET_FPS;
        }
    );

    public static Monster Sniper(Vector2 position, Vector2 direction) => new(
        position,
        Health: 1,
        Size: 16,
        Color.Blue,
        Speed: 8 * Game.TARGET_FPS,
        MonsterMovement.Direct(direction)
    );

    public static Monster Rock(Vector2 position, Vector2 direction) => new(
        position,
        Health: 1024,
        Size: 64,
        Color.Black,
        Speed: 2 * Game.TARGET_FPS,
        MonsterMovement.Direct(direction)
    );
}

public partial record struct Monster {
    public static void Render(Monster self, float dt) 
    {
        Thingies.DrawSquare(self.Position + Shake.Val.Offset, self.Size, self.Color);
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
            if (!Thingies.IsPointInsideSquare(bullet.Position, (self.Position, self.Size))) continue;
            
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
                    Lifetime: 0.5f + Raylib.GetRandomValue(0, 256) / 256f * 0.25f
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
                Raylib.PlaySound(Game.Instance.SoundKill);
                break;
            }
        }
    }
}

public record struct Wave(
    int Level,
    float Timer,
    float Length,
    float MonsterWaveIntervalMod)
    : Thingy.Ticking.Immutable<Wave>, Thingy.Rendering<Wave>
{
    static Wave() => Wave.Val = Init();
    public static Wave Init() => new(INIT, 0f, GetWaveLength(INIT), GetMonsterWaveIntervalMod(INIT));

    private const int INIT = 1;
    private static float GetWaveLength(int level) => 25.0f + level * 2.5f;
    private static float GetMonsterWaveIntervalMod(int level) => 0.33f + 0.67f / level;

    static void Thingy<Wave>.HandleReset(out bool shouldClear) {
        shouldClear = false;
        Wave.Val = Init();
    }

    public static void Tick(ref Wave self, float dt)
    {
        self.Timer += dt;
        if (self.Timer >= self.Length) {
            self.Level ++;
            self.Timer = 0f;
            self.Length = GetWaveLength(self.Level);
            self.MonsterWaveIntervalMod = GetMonsterWaveIntervalMod(self.Level);
        }
    }

    public static void Render(Wave self, float dt)
    {
        var timerText = $"{self.Length - self.Timer:0.00}";
        var timerTextWidth = Raylib.MeasureText(timerText, 30);
        Raylib.DrawText(timerText,
            (Raylib.GetScreenWidth() - timerTextWidth) / 2 + (int) Shake.Val.Offset.X, 
            5 + (int) Shake.Val.Offset.Y,
            30, Color.Red);
    }
}

public readonly record struct Dynamic(Dynamic.Implementation Impl)
    : Thingy.Ticking<Dynamic>, Thingy.Rendering<Dynamic>
{
    public static void Tick(ref Dynamic self, float dt, Action<Dynamic>? addMore,
        out bool removeSelf)
    {
        self.Impl.Tick(dt, addMore, out removeSelf);
    }

    public static void Render(Dynamic self, float dt)
    {
        self.Impl.Render(dt);
    }

    public interface Implementation {
        void Tick(float dt, Action<Dynamic>? addMore, out bool removeSelf)
        {
            removeSelf = false;
        }
        void Render(float dt) {}
    }

    public static void Init() {
        Console.WriteLine("init");
        Dynamic.Add(new(new BasicSpawner()));
        Dynamic.Add(new(new RockSpawner(9f)));
        Dynamic.Add(new(new SniperSpawner(6f)));
    }

    static Dynamic() => Init();
    static void Thingy<Dynamic>.HandleReset(out bool shouldClear) {
        shouldClear = false;
    }

    public record struct BasicSpawner(float NextSpawn, float Timer) : Implementation 
    {
        public void Tick(float dt, Action<Dynamic>? addMore, out bool removeSelf)
        {
            removeSelf = false;
            if (this.Timer >= this.NextSpawn) {
                var pos = Monster.GetRandomPosOnEdge();
                var u = Wave.Val.Level + 1 / 2 - 1;
                var monster = Raylib.GetRandomValue(0, 7) switch {
                    7 => Monster.ToughOrange(pos),
                    var n => (n < u) ? Monster.Funny2Hit(pos) : Monster.Goblin(pos),
                };
                // Raylib.GetRandomValue(0, 7) == 7 ? Monster.ToughOrange(pos) : Monster.Goblin(pos);
                Monster.Add(monster);
            }
            
            if (this.NextSpawn == 0.0f || this.Timer >= this.NextSpawn)
            {
                this.NextSpawn = Raylib.GetRandomValue(1000, 3000) / 1000.0f * Wave.Val.MonsterWaveIntervalMod;
                this.Timer = 0.0f;
            }
    
            this.Timer += dt;
        }
    }

    public record struct RockSpawner(float NextSpawn = 9f, float Timer = 0f) : Implementation {
        public void Tick(float dt, Action<Dynamic>? addMore, out bool removeSelf)
        {
            removeSelf = false;
            if (this.Timer >= this.NextSpawn) 
            {
                var pos = Monster.GetRandomPosOnEdge();
                Monster.Add(Monster.Rock(pos, Monster.GetDirectionTowardsPlayer(pos)));
            }

            if (this.NextSpawn == 0.0f || this.Timer >= this.NextSpawn)
            {
                var x = Math.Clamp(Wave.Val.Level/2 - 2, 1, 3);
                this.NextSpawn = (Raylib.GetRandomValue(9000/x, 27000/x) / 1000.0f) * Wave.Val.MonsterWaveIntervalMod;
                this.Timer = 0.0f;
            }
    
            this.Timer += dt;
        }
    }

    public record struct SniperSpawner(float NextSpawn = 6f, float Timer = 0f) : Implementation 
    {
        public void Tick(float dt, Action<Dynamic>? addMore, out bool removeSelf)
        {
            removeSelf = false;
            if (this.Timer >= this.NextSpawn) 
            {
                addMore?.Invoke(new(new Inner(
                    Monster.GetRandomPosOnEdge(),
                    Count: (Wave.Val.Level - 1) * 2 - 1
                )));
            }
            
            if (this.NextSpawn == 0.0f || this.Timer >= this.NextSpawn)
            {
                this.NextSpawn = (Raylib.GetRandomValue(6000, 18000) / 1000.0f) * Wave.Val.MonsterWaveIntervalMod;
                this.Timer = 0.0f;
            }
    
            this.Timer += dt;
        }

        public record struct Inner(
            Vector2 Pos,
            int Count,
            float Timer = 0f,
            float LastShot = 0f)
            : Implementation
        {
            private const float INTERVAL = 0.4f;

            public void Tick(float dt, Action<Dynamic>? addMore, out bool removeSelf)
            {
                removeSelf = this.Count <= 0;
                if (removeSelf) return;

                if (this.Timer - this.LastShot >= INTERVAL) {
                    this.Count --;
                    this.LastShot = Timer;

                    var monster = Monster.Sniper(
                        this.Pos,
                        Monster.GetDirectionTowardsPlayer(this.Pos)
                    );
                    Monster.Add(monster);
                }
                this.Timer += dt;
                removeSelf = this.Count <= 0;
            }
        }
    }
}
