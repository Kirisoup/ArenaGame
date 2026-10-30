using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;
using static Shared;

public record struct Player(
    Vector2 Position,
    bool Dead = false,
    bool IsLowSpeed = false,
    float ShotCooldown = 0f,
    int Score = 0,
    int Combo = 0)
    : Thingy.Ticking.Immutable<Player>, Thingy.Rendering<Player> 
{
    private const int SIZE = 32;
    private const int SIZE_SMOL = 16;
    private const int SPEED_LO = 3 * Game.TARGET_FPS;
    private const int SPEED_HI = 6 * Game.TARGET_FPS;
    private const float SHOT_TIME = 0.1f;
    private static readonly Color COLOR = Color.Red;
    
    public AccuracyStat Accuracy; // property cannot return a reference :(
    public record struct AccuracyStat(int Total, int Hits)
    {
        public readonly float Value { get {
            var res = this.Hits / (float) this.Total;
            if (!float.IsRealNumber(res)) res = 0f;
            return res;
        } }
    }

    static void Thingy<Player>.Reset(Action clear)
    {
        Player.Val.Dead = false;
        Player.Val.Position = new(400, 300);
        Player.Val.Score = 0;
        Player.Val.Combo = 0;
        Player.Val.Accuracy = new(0, 0);
    }

    static void Thingy.Rendering<Player>.Render(Player self, float dt) 
    {
        DrawSquare(self.Position + Shake.Val.Offset, self.IsLowSpeed ? SIZE_SMOL : SIZE, COLOR);
    }

    static void Thingy.Ticking.Immutable<Player>.Tick(ref Player self, float dt) 
    {
        var offset = new Vector2();
        if (Raylib.IsKeyDown(KeyboardKey.W)) offset.Y -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.S)) offset.Y += 1f;
        if (Raylib.IsKeyDown(KeyboardKey.A)) offset.X -= 1f;
        if (Raylib.IsKeyDown(KeyboardKey.D)) offset.X += 1f;

        self.IsLowSpeed = Raylib.IsKeyDown(KeyboardKey.LeftShift);
        if (offset.LengthSquared() > 0f)
        {
            var speed = self.IsLowSpeed
                ? SPEED_LO
                : SPEED_HI;
            offset = Vector2.Normalize(offset) * speed * dt;

            self.Position += offset;
            self.Position = Vector2.Clamp(self.Position, 
                new Vector2(SIZE / 2.0f, SIZE / 2.0f),
                new Vector2(Raylib.GetScreenWidth() - SIZE / 2.0f, Raylib.GetScreenHeight() - SIZE / 2.0f));
        }

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
            if (!IsPointInsideSquare(self.Position, (monster.Position, monster.Size))) continue;
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
}
