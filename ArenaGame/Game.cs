using System.Numerics;
using System.Reflection;
using Raylib_cs;

namespace ArenaGame;

public class Game
{
    private class Monster
    {
        public Vector2 Position;
        public int Health;
        public int Size;
        public Color Color;
        public int Speed;

        public static Monster Type1(Vector2 position)
        {
            return new Monster
            {
                Position = position,
                Health = 1,
                Size = 24,
                Color = Color.Green,
                Speed = 4 * TargetFps
            };
        }

        public static Monster Type2(Vector2 position)
        {
            return new Monster
            {
                Position = position,
                Health = 4,
                Size = 32,
                Color = Color.Orange,
                Speed = 2 * TargetFps
            };
        }
    }
    
    private const int TargetFps = 60;

    private const int PlayerSpeed = 5 * TargetFps;
    private const int PlayerSize = 32;
    private const float PlayerShotTime = 0.1f;

    private const string PlayerScoreFile = "high_score.txt";

    private bool _inTitleScreen = true;
    private bool _paused;
    
    private Vector2 _playerPos = new(400, 300);
    private float _playerShotCooldown;
    private bool _playerDead;
    private int _playerScore;
    private int _highScore;

    private const int BulletSpeed = 10 * TargetFps;
    private const int BulletSize = 8;
    
    private readonly List<(Vector2, Vector2)> _bullets = []; // position, velocity
    
    private readonly List<Monster> _monsters = [];
    private float _nextMonsterSpawn;
    private float _monsterTimer;

    private const float ShakeTime = 0.5f;
    private float _shakeTimer;
    private Vector2 _shakeDir;

    private Vector2 ShakeOffset => _shakeDir * MathF.Sin((_shakeTimer / ShakeTime) * 2 * MathF.PI) * _shakeTimer * 24;

    private float WaveLength => 25.0f + _wave * 2.5f;
    private float MonsterWaveIntervalMod => 1.0f / _wave;
    
    private int _wave = 1;
    private float _waveTimer;

    private Sound _soundDeath;
    private Sound _soundKill;
    private Sound _soundShoot;

    public void Run()
    {
        Raylib.InitWindow(800, 600, "Arena Game");
        Raylib.InitAudioDevice();
        Raylib.SetTargetFPS(TargetFps);

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
        
        _soundDeath = LoadSound("death.wav");
        _soundKill = LoadSound("kill.wav");
        _soundShoot = LoadSound("shoot.wav");

        if (File.Exists(PlayerScoreFile) && int.TryParse(File.ReadAllText(PlayerScoreFile), out var result))
        {
            _highScore = result;
        }
        
        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();
            
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            if (_inTitleScreen)
            {
                const string titleText = "ARENA GAME";
                var titleTextWidth = Raylib.MeasureText(titleText, 50);
                Raylib.DrawText(titleText, (Raylib.GetScreenWidth() - titleTextWidth) / 2, (int) (Raylib.GetScreenHeight() * 0.3f), 50, Color.Red);

                const string startText = "Press ENTER to begin";
                var startTextWidth = Raylib.MeasureText(startText, 30);
                Raylib.DrawText(startText, (Raylib.GetScreenWidth() - startTextWidth) / 2, (int) (Raylib.GetScreenHeight() * 0.5f), 30, Color.Black);

                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    _inTitleScreen = false;
                }
            }
            else if (!_playerDead)
            {
                var shake = ShakeOffset;

                UpdatePlayer(dt);
                UpdateBullets(dt);
                UpdateMonsters(dt);
                SpawnMonsters(dt);
                
                Raylib.DrawText("Wave " + _wave, 10 + (int) shake.X, 10 + (int) shake.Y, 30, Color.Red);
                Raylib.DrawText("Score: " + _playerScore, 10 + (int) shake.X, 40 + (int) shake.Y, 20, Color.Black);

                var timerText = $"{WaveLength - _waveTimer:0.00}";
                var timerTextWidth = Raylib.MeasureText(timerText, 30);
                Raylib.DrawText(timerText, (Raylib.GetScreenWidth() - timerTextWidth) / 2 + (int) shake.X, 5 + (int) shake.Y, 30, Color.Red);

                if (Raylib.IsKeyPressed(KeyboardKey.Space))
                {
                    _paused = !_paused;
                }

                if (_paused)
                {
                    const string pausedText = "PAUSED";
                    var pausedTextWidth = Raylib.MeasureText(pausedText, 30);
                    Raylib.DrawText(pausedText, (Raylib.GetScreenWidth() - pausedTextWidth) / 2, (Raylib.GetScreenHeight() - 30) / 2, 30, Color.Black);
                }
                else
                {
                    _shakeTimer = MathF.Max(_shakeTimer - dt, 0.0f);
                    
                    _waveTimer += dt;
                    if (_waveTimer >= WaveLength)
                    {
                        _wave++;
                        _waveTimer = 0.0f;
                    }
                }
            }
            else
            {
                Raylib.DrawText("Game Over", 10, 10, 30, Color.Red);
                Raylib.DrawText("Press R to restart", 10, 40, 20, Color.Black);
                
                Raylib.DrawText("High Score: " + _highScore, 10, 70, 20, Color.Black);
                Raylib.DrawText("Your Score: " + _playerScore, 10, 90, 20, Color.Black);

                if (Raylib.IsKeyPressed(KeyboardKey.R))
                {
                    _playerDead = false;
                    _playerPos = new Vector2(400, 300);
                    _bullets.Clear();
                    _monsters.Clear();
                    _nextMonsterSpawn = 0.0f;
                    _monsterTimer = 0.0f;
                    _playerScore = 0;
                    _wave = 1;
                    _waveTimer = 0.0f;
                }
            }
            
            Raylib.EndDrawing();
        }
        
        Raylib.CloseWindow();
    }

    private void UpdatePlayer(float dt)
    {
        var shake = ShakeOffset;
        Raylib.DrawRectangle((int) _playerPos.X - PlayerSize / 2 + (int) shake.X, (int) _playerPos.Y - PlayerSize / 2 + (int) shake.Y, PlayerSize, PlayerSize, Color.Red);

        if (_paused) return;
        
        var offset = new Vector2();
        if (Raylib.IsKeyDown(KeyboardKey.W))
        {
            offset.Y -= 1;
        }

        if (Raylib.IsKeyDown(KeyboardKey.S))
        {
            offset.Y += 1;
        }

        if (Raylib.IsKeyDown(KeyboardKey.A))
        {
            offset.X -= 1;
        }

        if (Raylib.IsKeyDown(KeyboardKey.D))
        {
            offset.X += 1;
        }

        if (offset.LengthSquared() > 0)
        {
            offset = Vector2.Normalize(offset) * PlayerSpeed * dt;
        }

        _playerPos += offset;
        _playerPos = Vector2.Clamp(_playerPos, new Vector2(PlayerSize / 2.0f, PlayerSize / 2.0f),
            new Vector2(Raylib.GetScreenWidth() - PlayerSize / 2.0f, Raylib.GetScreenHeight() - PlayerSize / 2.0f));

        if (_playerShotCooldown > 0.0f)
        {
            _playerShotCooldown -= dt;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && _playerShotCooldown <= 0.0f)
        {
            var mousePos = Raylib.GetMousePosition();
            var velocity = Vector2.Normalize(mousePos - _playerPos) * BulletSpeed;
            _bullets.Add((_playerPos, velocity));
            Raylib.PlaySound(_soundShoot);
            _playerShotCooldown = PlayerShotTime;
        }

        foreach (var monster in _monsters)
        {
            if (_playerPos.X >= monster.Position.X - monster.Size / 2.0 && _playerPos.X <= monster.Position.X + monster.Size / 2.0 &&
                _playerPos.Y >= monster.Position.Y - monster.Size / 2.0 && _playerPos.Y <= monster.Position.Y + monster.Size / 2.0)
            {
                _playerDead = true;
                Raylib.PlaySound(_soundDeath);

                if (_playerScore > _highScore)
                {
                    _highScore = _playerScore;
                    File.WriteAllText(PlayerScoreFile, _playerScore.ToString());
                }
                
                break;
            }
        }

    }

    private void UpdateBullets(float dt)
    {
        var shake = ShakeOffset;
        
        for (var i = 0; i < _bullets.Count; i++)
        {
            var (position, velocity) = _bullets[i];
            Raylib.DrawRectangle((int) position.X - BulletSize / 2 + (int) shake.X, (int) position.Y - BulletSize / 2 + (int) shake.Y, BulletSize, BulletSize, Color.Black);

            if (_paused) continue;
            
            _bullets[i] = (position + velocity * dt, velocity);

            if (position.X < -BulletSize / 2.0f || position.X > Raylib.GetScreenWidth() + BulletSize / 2.0f || position.Y < -BulletSize / 2.0f ||
                position.Y > Raylib.GetScreenHeight() + BulletSize / 2.0f)
            {
                _bullets.RemoveAt(i);
                i--;
            }
        }
    }

    private void UpdateMonsters(float dt)
    {
        var shake = ShakeOffset;
        
        for (var i = 0; i < _monsters.Count; i++)
        {
            var monster = _monsters[i];
            Raylib.DrawRectangle((int) monster.Position.X - monster.Size / 2 + (int) shake.X, (int) monster.Position.Y - monster.Size / 2 + (int) shake.Y, monster.Size, monster.Size, monster.Color);

            if (_paused) continue;
            
            var delta = Vector2.Normalize(_playerPos - monster.Position) * monster.Speed * dt;
            _monsters[i].Position = monster.Position + delta;

            for (var j = 0; j < _bullets.Count; j++)
            {
                var bulletPos = _bullets[j].Item1;
                
                if (bulletPos.X >= monster.Position.X - monster.Size / 2.0f && bulletPos.X <= monster.Position.X + monster.Size / 2.0f &&
                    bulletPos.Y >= monster.Position.Y - monster.Size / 2.0f && bulletPos.Y <= monster.Position.Y + monster.Size / 2.0f)
                {
                    monster.Health--;
                    _bullets.RemoveAt(j);
                    j--;
                    
                    // Screen shake
                    _shakeTimer = MathF.Min(_shakeTimer + ShakeTime, 3.0f);
                    _shakeDir = Vector2.Normalize(_playerPos - monster.Position);
                    
                    if (monster.Health <= 0)
                    {
                        _monsters.RemoveAt(i);
                        i--;
                        _playerScore++;
                        Raylib.PlaySound(_soundKill);
                        break;
                    }
                }                   
            }
        }
    }

    private void SpawnMonsters(float dt)
    {
        if (_monsterTimer >= _nextMonsterSpawn)
        {
            var side = Raylib.GetRandomValue(0, 3);
            var x = Raylib.GetRandomValue(0, Raylib.GetScreenWidth());
            var y = Raylib.GetRandomValue(0, Raylib.GetScreenHeight());

            var pos = side switch
            {
                0 => new Vector2(0, y),
                1 => new Vector2(x, 0),
                2 => new Vector2(Raylib.GetScreenWidth(), y),
                3 => new Vector2(x, Raylib.GetScreenHeight()),
                _ => new Vector2()
            };

            var monster = Raylib.GetRandomValue(0, 5) == 5 ? Monster.Type2(pos) : Monster.Type1(pos);

            _monsters.Add(monster);
        }
        
        if (_nextMonsterSpawn == 0.0f || _monsterTimer >= _nextMonsterSpawn)
        {
            _nextMonsterSpawn = (Raylib.GetRandomValue(1000, 3000) / 1000.0f) * MonsterWaveIntervalMod;
            _monsterTimer = 0.0f;
        }

        _monsterTimer += dt;
    }
    
}