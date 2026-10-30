using Raylib_cs;

namespace ArenaGame.thingies;

public record struct Wave(
    int Level,
    float Timer,
    float Length,
    float MonsterWaveIntervalMod)
    : Thingy.Ticking.Immutable<Wave>, Thingy.Rendering<Wave>
{
    private const int INIT = 1;
    private static float GetWaveLength(int level) => 25.0f + level * 2.5f;
    private static float GetMonsterWaveIntervalMod(int level) => 0.33f + 0.67f / level;

    static void Thingy<Wave>.Reset(Action clear)
    {
        Wave.Val = new(INIT, 0f, GetWaveLength(INIT), GetMonsterWaveIntervalMod(INIT));
    }

    static void Thingy.Ticking.Immutable<Wave>.Tick(ref Wave self, float dt)
    {
        self.Timer += dt;
        if (self.Timer >= self.Length) {
            self.Level ++;
            self.Timer = 0f;
            self.Length = GetWaveLength(self.Level);
            self.MonsterWaveIntervalMod = GetMonsterWaveIntervalMod(self.Level);
        }
    }

    static void Thingy.Rendering<Wave>.Render(Wave self, float dt)
    {
        var timerText = $"{self.Length - self.Timer:0.00}";
        var timerTextWidth = Raylib.MeasureText(timerText, 30);
        Raylib.DrawText(timerText,
            (Raylib.GetScreenWidth() - timerTextWidth) / 2 + (int) Shake.Val.Offset.X, 
            5 + (int) Shake.Val.Offset.Y,
            30, Color.Red);
    }
}
