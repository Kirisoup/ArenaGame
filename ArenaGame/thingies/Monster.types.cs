using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;

public partial record struct Monster
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
