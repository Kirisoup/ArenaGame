using System.Numerics;
using Raylib_cs;

namespace ArenaGame.thingies;

public readonly record struct Dynamic(Dynamic.Implementation Impl)
    : Thingy.Ticking<Dynamic>, Thingy.Rendering<Dynamic>
{
    static void Thingy<Dynamic>.Reset(Action clear) {
        clear();
        Dynamic.Add(new(new BasicSpawner()));
        Dynamic.Add(new(new RockSpawner(9f)));
        Dynamic.Add(new(new SniperSpawner(6f)));
    }

    static void Thingy.Ticking<Dynamic>.Tick(ref Dynamic self, float dt, Action<Dynamic>? addMore,
        out bool removeSelf)
    {
        self.Impl.Tick(dt, addMore, out removeSelf);
    }

    static void Thingy.Rendering<Dynamic>.Render(Dynamic self, float dt)
    {
        self.Impl.Render(dt);
    }

    public interface Implementation
    {
        void Tick(float dt, Action<Dynamic>? addMore, out bool removeSelf)
        {
            removeSelf = false;
        }
        void Render(float dt) {}
    }

    public record struct BasicSpawner(float NextSpawn, float Timer) : Implementation 
    {
        public void Tick(float dt, Action<Dynamic>? addMore, out bool removeSelf)
        {
            removeSelf = false;
            if (this.Timer >= this.NextSpawn)
            {
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
            var x = Wave.Val.Level/2 - 2;
            if (x <= 0) return;

            if (this.Timer >= this.NextSpawn) 
            {
                var pos = Monster.GetRandomPosOnEdge();
                Monster.Add(Monster.Rock(pos, Monster.GetDirectionTowardsPlayer(pos)));
            }

            if (this.NextSpawn == 0.0f || this.Timer >= this.NextSpawn)
            {
                this.NextSpawn = Raylib.GetRandomValue(9000, 27000)
                    / (Math.Clamp(x, 1, 3) * 1000.0f) 
                    * Wave.Val.MonsterWaveIntervalMod;
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
                    this.LastShot = this.Timer;

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
