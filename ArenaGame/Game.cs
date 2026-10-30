using System.Numerics;
using System.Reflection;
using Raylib_cs;
using ArenaGame.thingies;
using Wave = ArenaGame.thingies.Wave;

namespace ArenaGame;

public sealed class Game {
    public static Game Instance { get; private set; } = null!;
    public const int TARGET_FPS = 60;

    public const string PLAYER_SCORE_FILE = "high_score.txt";
    public int highScore;

    public Sound SoundDeath { get; private set; }
    public Sound SoundKill  { get; private set; }
    public Sound SoundShoot { get; private set; }
    
    private bool inTitleScreen = true;
    private bool paused;

    public void Run()
    {
        Instance = this;

        Raylib.InitWindow(800, 600, "Arena Game");
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(TARGET_FPS);
        Raylib.SetExitKey(0);

        var assembly = Assembly.GetExecutingAssembly();
        foreach (var manifestResourceName in assembly.GetManifestResourceNames())
        {
            Console.WriteLine(manifestResourceName);
        }

        Sound LoadSound(string name)
        {
            var fullName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(name));
            var stream = assembly.GetManifestResourceStream(fullName)!;
            using var memStream = new MemoryStream();
            stream.CopyTo(memStream);
            var buffer = memStream.ToArray();
            return Raylib.LoadSoundFromWave(Raylib.LoadWaveFromMemory(".wav", buffer));
        }
        
        this.SoundDeath = LoadSound("death.wav");
        this.SoundKill  = LoadSound("kill.wav");
        this.SoundShoot = LoadSound("shoot.wav");

        if (File.Exists(PLAYER_SCORE_FILE)) _ = int.TryParse(File.ReadAllText(PLAYER_SCORE_FILE), out this.highScore);

        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            if (this.inTitleScreen)
            {
                const string titleText = "ARENA GAME";
                var titleTextWidth = Raylib.MeasureText(titleText, 50);
                Raylib.DrawText(titleText, (Raylib.GetScreenWidth() - titleTextWidth) / 2, (int) (Raylib.GetScreenHeight() * 0.3f), 50, Color.Red);

                const string startText = "Press ENTER to begin";
                var startTextWidth = Raylib.MeasureText(startText, 30);
                Raylib.DrawText(startText, (Raylib.GetScreenWidth() - startTextWidth) / 2, (int) (Raylib.GetScreenHeight() * 0.5f), 30, Color.Black);

                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    this.inTitleScreen = false;
                }
            }
            else if (!Player.Val.Dead)
            {
                // System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Dynamic).TypeHandle);
                // Console.WriteLine(Dynamic.Span.Length);
                if (!this.paused) {
                    Shake.TickAll(dt);
                    Wave.TickAll(dt);
                    Particle.TickAll(dt);
                    Player.TickAll(dt);
                    Bullet.TickAll(dt);
                    Monster.TickAll(dt);
                    Dynamic.TickAll(dt);
                }

                Particle.RenderAll(dt);
                Player.Render(Player.Val, dt);
                Bullet.RenderAll(dt);
                Monster.RenderAll(dt);
                Dynamic.RenderAll(dt);

                Wave.RenderAll(dt);
                Raylib.DrawText("Wave " + Wave.Val.Level, 10 + (int) Shake.Val.Offset.X, 10 + (int) Shake.Val.Offset.Y, 30, Color.Red);
                Raylib.DrawText("Score: " + Player.Val.Score, 10 + (int) Shake.Val.Offset.X, 40 + (int) Shake.Val.Offset.Y, 20, Color.Black);
                Raylib.DrawText("Combo: " + Player.Val.Combo, 10 + (int) Shake.Val.Offset.X, 60 + (int) Shake.Val.Offset.Y, 20, Color.Black);

                if (/* Raylib.IsKeyPressed(KeyboardKey.Space) || */Raylib.IsKeyPressed(KeyboardKey.Escape))
                {
                    this.paused = !this.paused;
                }

                if (this.paused)
                {
                    const string pausedText = "PAUSED";
                    var pausedTextWidth = Raylib.MeasureText(pausedText, 30);
                    Raylib.DrawText(pausedText, (Raylib.GetScreenWidth() - pausedTextWidth) / 2, (Raylib.GetScreenHeight() - 30) / 2, 30, Color.Black);
                }
            }
            else
            {
                Raylib.DrawText("Game Over", 10, 10, 30, Color.Red);
                Raylib.DrawText("Press R to restart", 10, 40, 20, Color.Black);
                
                Raylib.DrawText("High Score: " + this.highScore, 10, 70, 20, Color.Black);
                Raylib.DrawText("Your Score: " + Player.Val.Score, 10, 90, 20, Color.Black);

                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    Player.Reset();
                    Bullet.Reset();
                    Particle.Reset();
                    Monster.Reset();
                    Dynamic.Reset();
                    Shake.Reset();
                    Wave.Reset();
                }
            }

            Raylib.EndDrawing();
        }
        
        Raylib.CloseWindow();
    }
}