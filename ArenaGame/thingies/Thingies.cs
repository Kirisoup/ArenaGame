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
    float timer,
    Vector2 dir,
    Vector2 offset
) : Thingy.Ticking.Immutable<Shake> {
    public static Shake shake = new();

    public const float DURATION = 0.5f;

    public static void Tick(ref Shake self, float dt)
    {
        self.timer = MathF.Max(self.timer - dt, 0.0f);
        self.offset = self.dir * MathF.Sin((self.timer / DURATION) * 2 * MathF.PI) * self.timer * 24;
    }
}

public record struct Bullet(
    Vector2 position,
    Vector2 velocity)
    : Thingy.Ticking<Bullet>, Thingy.Rendering<Bullet>
{
    public static readonly Thingy<Bullet>.Manager all = new();

    public const float BASE_SPEED = 10 * Game.TARGET_FPS;

    private const int SIZE = 8;
    private static readonly Color COLOR = Color.Black;

    public static void Render(Bullet self, float dt) 
    {
        Thingies.DrawSquare(self.position + Shake.shake.offset, SIZE, COLOR);
    }

    public static void Tick(ref Bullet self, float dt, Action<Bullet>? addMore,
        out bool removeSelf) 
    {
        self.position += self.velocity * dt;

        removeSelf = false;
        removeSelf = removeSelf || self.position.X < -SIZE / 2.0f || self.position.X > Raylib.GetScreenWidth()  + SIZE / 2.0f;
        removeSelf = removeSelf || self.position.Y < -SIZE / 2.0f || self.position.Y > Raylib.GetScreenHeight() + SIZE / 2.0f;
    }
}

public record struct Particle(
    Vector2 position,
    Vector2 velocity,
    Color color,
    float lifetime)
    : Thingy.Ticking<Particle>, Thingy.Rendering<Particle>
{
    public static readonly Thingy<Particle>.Manager all = new();

    private const float DRAG = 0.6f;
    private const int SIZE = 6;

    public static void Render(Particle self, float dt) 
    {
        Thingies.DrawSquare(self.position + Shake.shake.offset, SIZE, self.color);
    }

    public static void Tick(ref Particle self, float dt, Action<Particle>? addMore,
            out bool removeSelf) 
    {
        self.position += self.velocity * dt;
        self.velocity *= 1-((1-DRAG) * dt);
        self.lifetime -= dt;
        removeSelf = self.lifetime <= 0f;
    }
}

public record struct Player(
    Vector2 position,
    bool dead = false,
    float shotCooldown = 0f,
    int score = 0)
    : Thingy.Ticking.Immutable<Player>, Thingy.Rendering<Player> 
{
    public static Player thePlayer = new Player(new(400, 300));

    private const int SIZE = 32;
    private const int SPEED = 5 * Game.TARGET_FPS;
    private const float SHOT_TIME = 0.1f;
    private static readonly Color COLOR = Color.Red;

    public static void Render(Player self, float dt) 
    {
        Thingies.DrawSquare(self.position + Shake.shake.offset, SIZE, COLOR);
        Thingies.DrawSquare(self.position + Shake.shake.offset, 6, Color.White);
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

        self.position += offset;
        self.position = Vector2.Clamp(self.position, 
            new Vector2(SIZE / 2.0f, SIZE / 2.0f),
            new Vector2(Raylib.GetScreenWidth() - SIZE / 2.0f, Raylib.GetScreenHeight() - SIZE / 2.0f));
        
        self.shotCooldown = Math.Max(0f, self.shotCooldown - dt);
        if (self.shotCooldown == 0f && (Raylib.IsMouseButtonPressed(MouseButton.Left) || Raylib.IsKeyPressed(KeyboardKey.Space)))
        {
            var mousePos = Raylib.GetMousePosition();
            var velocity = Vector2.Normalize(mousePos - self.position) * Bullet.BASE_SPEED;
            Bullet.all.Add(new Bullet(self.position, velocity));
            Raylib.PlaySound(Game.Instance.SoundShoot);
            self.shotCooldown = SHOT_TIME;
        }

        foreach (var monster in Monster.all)
        {
            if (!Thingies.IsPointInsideSquare(self.position, (monster.position, monster.size))) continue;
            self.dead = true;
            Raylib.PlaySound(Game.Instance.SoundDeath);
            if (self.score > Game.Instance.highScore)
            {
                Game.Instance.highScore = self.score;
                File.WriteAllText(Game.PLAYER_SCORE_FILE, self.score.ToString());
            }

            break;
        }
    }
}

public partial record struct Monster(
    Vector2 position,
    int health,
    int size,
    Color color,
    int speed,
    Monster.Movement movement,
    Monster.OnHit? onHit = null)
    : Thingy.Ticking<Monster>, Thingy.Rendering<Monster>
{
    public delegate void OnHit(ref Monster self);
    public readonly struct Movement {
        public enum Kind { Homing, Direct }

        public readonly Kind kind;
        public readonly Vector2 direction;
        private Movement(Kind kind, Vector2 direction = default)
        {
            this.kind = kind;
            this.direction = direction;
        }

        public static readonly Movement Homing = new(Kind.Homing);
        public static Movement Direct(Vector2 direction) => new(Kind.Direct, direction);
    }

    public static Monster Goblin(Vector2 position) => new(
        position,
        health: 1,
        size: 24,
        Color.Green,
        speed: 4 * Game.TARGET_FPS,
        Movement.Homing
    );

    public static Monster ToughOrange(Vector2 position) => new(
        position,
        health: 4,
        size: 32,
        Color.Orange,
        speed: 2 * Game.TARGET_FPS,
        Movement.Homing
    );

    public static Monster Funny2Hit(Vector2 position) => new(
        position,
        health: 2,
        size: 48,
        Color.Purple,
        speed: 6 * Game.TARGET_FPS,
        Movement.Homing,
        onHit: static (ref Monster self) => {
            self.size = 24;
            self.speed = 2 * Game.TARGET_FPS;
        }
    );

    public static Monster Sniper(Vector2 position, Vector2 direction) => new(
        position,
        health: 1,
        size: 16,
        Color.Blue,
        speed: 8 * Game.TARGET_FPS,
        Movement.Direct(direction)
    );

    public static Monster Rock(Vector2 position, Vector2 direction) => new(
        position,
        health: 1024,
        size: 64,
        Color.Black,
        speed: 2 * Game.TARGET_FPS,
        Movement.Direct(direction)
    );
}

public partial record struct Monster {
    public static readonly Thingy<Monster>.Manager all = new();

    public static void Render(Monster self, float dt) 
    {
        Thingies.DrawSquare(self.position + Shake.shake.offset, self.size, self.color);
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
        return Vector2.Normalize(Player.thePlayer.position - position);
    }

    public static void Tick(ref Monster self, float dt, Action<Monster>? addMore,
        out bool removeSelf) 
    {
        removeSelf = false;

        var delta = self.movement.kind switch
        {
            Movement.Kind.Homing => GetDirectionTowardsPlayer(self.position),
            Movement.Kind.Direct => self.movement.direction,
            _ => default, // unreachable
        };
        delta *= self.speed * dt;
        self.position += delta;
        
        for (int i = 0; i < Bullet.all.Count(); i++)
        {
            var bullet = Bullet.all[i];
            if (!Thingies.IsPointInsideSquare(bullet.position, (self.position, self.size))) continue;
            
            Bullet.all.RemoveAt(i);
            i --;
            
            self.health --;
            self.onHit?.Invoke(ref self);

            // hit particles
            var particleCount = self.health > 0 ? 5 : 10;
            for (int j = 0; j < particleCount; j++) 
            {
                var angle = Raylib.GetRandomValue(0, 359) * Raylib.DEG2RAD;
                var velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                velocity *= 1f + Raylib.GetRandomValue(0, 256) / 256f;
                velocity *= 1f * Game.TARGET_FPS;
                if (self.health > 0) velocity += delta * 2f;
                else velocity += bullet.velocity * 0.75f;

                Particle.all.Add(new Particle(
                    self.position,
                    velocity,
                    self.color,
                    lifetime: 0.5f + Raylib.GetRandomValue(0, 256) / 256f * 0.25f
                ));
            }

            // Screen shake
            Shake.shake.timer = MathF.Min(Shake.shake.timer + Shake.DURATION, 3.0f);
            Shake.shake.dir = Vector2.Normalize(Player.thePlayer.position - self.position);

            if (self.health <= 0)
            {
                removeSelf = true;
                Player.thePlayer.score ++;
                Raylib.PlaySound(Game.Instance.SoundKill);
                break;
            }
        }
    }
}

public record struct Wave(
    int level,
    float timer,
    float length,
    float monsterWaveIntervalMod)
    : Thingy.Ticking.Immutable<Wave>, Thingy.Rendering<Wave>
{
    public static Wave wave = Init();
    public static Wave Init() => new(INIT, 0f, GetWaveLength(INIT), GetMonsterWaveIntervalMod(INIT));

    private const int INIT = 3;
    private static float GetWaveLength(int level) => 25.0f + level * 2.5f;
    private static float GetMonsterWaveIntervalMod(int level) => 0.33f + 0.67f / level;

    public static void Tick(ref Wave self, float dt)
    {
        self.timer += dt;
        if (self.timer >= self.length) {
            self.level ++;
            self.timer = 0f;
            self.length = GetWaveLength(self.level);
            self.monsterWaveIntervalMod = GetMonsterWaveIntervalMod(self.level);
        }
    }

    public static void Render(Wave self, float dt)
    {
        var timerText = $"{self.length - self.timer:0.00}";
        var timerTextWidth = Raylib.MeasureText(timerText, 30);
        Raylib.DrawText(timerText,
            (Raylib.GetScreenWidth() - timerTextWidth) / 2 + (int) Shake.shake.offset.X, 
            5 + (int) Shake.shake.offset.Y,
            30, Color.Red);
    }
}

public readonly record struct MiscThingy(MiscThingy.Impl impl)
    : Thingy.Ticking<MiscThingy>, Thingy.Rendering<MiscThingy>
{
    public static readonly Thingy<MiscThingy>.Manager all = new();

    public static void Tick(ref MiscThingy self, float dt, Action<MiscThingy>? addMore,
        out bool removeSelf)
    {
        self.impl.Tick(dt, addMore, out removeSelf);
    }

    public static void Render(MiscThingy self, float dt)
    {
        self.impl.Render(dt);
    }

    public interface Impl {
        void Tick(float dt, Action<MiscThingy>? addMore, out bool removeSelf)
        {
            removeSelf = false;
        }
        void Render(float dt) {}
    }

    public static void Init() {
        all.Add(new(new BasicSpawner()));
        all.Add(new(new RockSpawner(9f)));
        all.Add(new(new SniperSpawner(6f)));
    }

    static MiscThingy() { Init(); }

    public record struct BasicSpawner(float nextSpawn, float timer) : Impl 
    {
        public void Tick(float dt, Action<MiscThingy>? addMore, out bool removeSelf)
        {
            removeSelf = false;
            if (this.timer >= this.nextSpawn) {
                var pos = Monster.GetRandomPosOnEdge();
                var u = (Wave.wave.level + 1 / 2) - 1;
                var monster = Raylib.GetRandomValue(0, 7) switch {
                    7 => Monster.ToughOrange(pos),
                    var n => (n < u) ? Monster.Funny2Hit(pos) : Monster.Goblin(pos),
                };
                // Raylib.GetRandomValue(0, 7) == 7 ? Monster.ToughOrange(pos) : Monster.Goblin(pos);
                Monster.all.Add(monster);
            }
            
            if (this.nextSpawn == 0.0f || this.timer >= this.nextSpawn)
            {
                this.nextSpawn = (Raylib.GetRandomValue(1000, 3000) / 1000.0f) * Wave.wave.monsterWaveIntervalMod;
                this.timer = 0.0f;
            }
    
            this.timer += dt;
        }
    }

    public record struct RockSpawner(float nextSpawn = 9f, float timer = 0f) : Impl {
        public void Tick(float dt, Action<MiscThingy>? addMore, out bool removeSelf)
        {
            removeSelf = false;
            if (this.timer >= this.nextSpawn) 
            {
                var pos = Monster.GetRandomPosOnEdge();
                Monster.all.Add(Monster.Rock(pos, Monster.GetDirectionTowardsPlayer(pos)));
            }
            
            if (this.nextSpawn == 0.0f || this.timer >= this.nextSpawn)
            {
                var x = Math.Clamp(Wave.wave.level/2 - 2, 1, 3);
                this.nextSpawn = (Raylib.GetRandomValue(9000/x, 27000/x) / 1000.0f) * Wave.wave.monsterWaveIntervalMod;
                this.timer = 0.0f;
            }
    
            this.timer += dt;
        }
    }

    public record struct SniperSpawner(float nextSpawn = 6f, float timer = 0f) : Impl 
    {
        public void Tick(float dt, Action<MiscThingy>? addMore, out bool removeSelf)
        {
            removeSelf = false;
            if (this.timer >= this.nextSpawn) 
            {
                addMore?.Invoke(new(new Inner(
                    Monster.GetRandomPosOnEdge(),
                    count: (Wave.wave.level - 1) * 2 - 1
                )));
            }
            
            if (this.nextSpawn == 0.0f || this.timer >= this.nextSpawn)
            {
                this.nextSpawn = (Raylib.GetRandomValue(6000, 18000) / 1000.0f) * Wave.wave.monsterWaveIntervalMod;
                this.timer = 0.0f;
            }
    
            this.timer += dt;
        }

        public record struct Inner(
            Vector2 pos,
            int count,
            float timer = 0f,
            float lastShot = 0f)
            : Impl
        {
            private const float INTERVAL = 0.4f;

            public void Tick(float dt, Action<MiscThingy>? addMore, out bool removeSelf)
            {
                removeSelf = this.count <= 0;
                if (removeSelf) return;

                if (this.timer - this.lastShot >= INTERVAL) {
                    this.count --;
                    this.lastShot = timer;

                    var monster = Monster.Sniper(
                        this.pos,
                        Monster.GetDirectionTowardsPlayer(this.pos)
                    );
                    Monster.all.Add(monster);
                }
                this.timer += dt;
                removeSelf = this.count <= 0;
            }
        }
    }
}
