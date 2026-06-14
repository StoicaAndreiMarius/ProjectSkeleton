using System.Text.Json;
using Silk.NET.Maths;
using TheAdventure.Exceptions;
using TheAdventure.Models;
using TheAdventure.Models.Data;

namespace TheAdventure;

public class Engine
{
    private const string AssetsDir = "Assets";
    private const string SavePath = "highscore.json";
    private const int CoinTarget = 6;
    private const double EnemySpawnSeconds = 2.5;
    private const int BombRadius = 80;

    private readonly GameRenderer _renderer;
    private readonly Input _input;
    private readonly Random _rng = new();
    private readonly SaveManager _save = new(SavePath);
    private readonly GameWorld _world = new();

    private readonly Dictionary<int, GameObject> _gameObjects = new();
    private readonly Dictionary<string, TileSet> _tileSets = new();
    private readonly Dictionary<int, Tile> _tileIdMap = new();
    private Level _level = new();
    private int _worldWidth;
    private int _worldHeight;

    private PlayerObject? _player;
    private DateTimeOffset _lastFrame = DateTimeOffset.Now;
    private DateTimeOffset _lastEnemySpawn = DateTimeOffset.Now;

    // HUD textures
    private int _digitsTex = -1;
    private int _heartTex = -1;
    private int _winTex = -1;
    private int _loseTex = -1;
    private const int DigitW = 30;
    private const int DigitH = 42;
    private const int HeartSize = 24;

    public Engine(GameRenderer renderer, Input input)
    {
        _renderer = renderer;
        _input = input;

        _input.OnMouseClick += (_, c) => SpawnBombAtScreen(c.X, c.Y);
        _input.OnBombKey += (_, _) => SpawnBombAtPlayer();
        _input.OnRestartKey += (_, _) => RestartIfFinished();
    }

    public async Task SetupWorldAsync()
    {
        _world.HighScore = await _save.LoadHighScoreAsync();
        LoadLevel();
        _player = new PlayerObject(
            SpriteSheet.Load(_renderer, "Player.json", AssetsDir),
            _worldWidth / 2, _worldHeight / 2);

        LoadHudTextures();

        for (int i = 0; i < CoinTarget; i++) SpawnCoin();
    }

    private void LoadHudTextures()
    {
        _digitsTex = _renderer.LoadTexture(Path.Combine(AssetsDir, "digits.png"), out _);
        _heartTex = _renderer.LoadTexture(Path.Combine(AssetsDir, "heart.png"), out _);
        _winTex = _renderer.LoadTexture(Path.Combine(AssetsDir, "you_win.png"), out _);
        _loseTex = _renderer.LoadTexture(Path.Combine(AssetsDir, "game_over.png"), out _);
    }

    private void LoadLevel()
    {
        var json = File.ReadAllText(Path.Combine(AssetsDir, "terrain.tmj"));
        var level = JsonSerializer.Deserialize<Level>(json)
            ?? throw new AssetLoadException("Failed to load level");

        foreach (var refr in level.TileSets)
        {
            var tsJson = File.ReadAllText(Path.Combine(AssetsDir, refr.Source));
            var ts = JsonSerializer.Deserialize<TileSet>(tsJson)
                ?? throw new AssetLoadException("Failed to load tileset " + refr.Source);

            foreach (var tile in ts.Tiles)
            {
                tile.TextureId = _renderer.LoadTexture(Path.Combine(AssetsDir, tile.Image), out _);
                _tileIdMap.Add(tile.Id!.Value, tile);
            }
            _tileSets.Add(ts.Name, ts);
        }

        _level = level;
        _worldWidth = (level.Width ?? 0) * (level.TileWidth ?? 0);
        _worldHeight = (level.Height ?? 0) * (level.TileHeight ?? 0);
        _renderer.SetWorldBounds(new Rectangle<int>(0, 0, _worldWidth, _worldHeight));
    }

    public void ProcessFrame()
    {
        var now = DateTimeOffset.Now;
        double ms = (now - _lastFrame).TotalMilliseconds;
        _lastFrame = now;

        if (_world.Status != GameStatus.Playing) return;

        double up = _input.IsUpPressed() ? 1.0 : 0.0;
        double down = _input.IsDownPressed() ? 1.0 : 0.0;
        double left = _input.IsLeftPressed() ? 1.0 : 0.0;
        double right = _input.IsRightPressed() ? 1.0 : 0.0;

        _player?.UpdatePosition(up, down, left, right, _worldWidth, _worldHeight, ms);

        // chase the player
        if (_player != null)
        {
            foreach (var enemy in _gameObjects.Values.OfType<Enemy>())
            {
                enemy.Target = _player.Position;
                enemy.Update(ms);
            }
        }

        HandleCollisions();
        SpawnEnemiesIfDue(now);
        TopUpCoins();
    }

    private void HandleCollisions()
    {
        if (_player == null) return;

        // coin pickup
        var pickedUp = _gameObjects.Values.OfType<Coin>()
            .Where(c => Hits(_player, c))
            .ToList();

        foreach (var c in pickedUp)
        {
            _gameObjects.Remove(c.Id);
            _world.AddScore(GameWorld.CoinValue);
        }

        // enemy hit
        if (!_player.IsInvulnerable)
        {
            bool hit = _gameObjects.Values.OfType<Enemy>().Any(e => Hits(_player, e));
            if (hit)
            {
                _player.TakeHit();
                _world.Damage();
            }
        }
    }

    private static bool Hits(ICollidable a, ICollidable b)
    {
        int dx = a.Position.X - b.Position.X;
        int dy = a.Position.Y - b.Position.Y;
        int r = a.Radius + b.Radius;
        return dx * dx + dy * dy <= r * r;
    }

    private void SpawnEnemiesIfDue(DateTimeOffset now)
    {
        if ((now - _lastEnemySpawn).TotalSeconds < EnemySpawnSeconds) return;
        _lastEnemySpawn = now;

        // spawn at a random edge of the world, away from player
        int x, y;
        if (_rng.Next(2) == 0)
        {
            x = _rng.Next(0, 2) == 0 ? 40 : _worldWidth - 40;
            y = _rng.Next(40, _worldHeight - 40);
        }
        else
        {
            x = _rng.Next(40, _worldWidth - 40);
            y = _rng.Next(0, 2) == 0 ? 40 : _worldHeight - 40;
        }

        var sheet = SpriteSheet.Load(_renderer, "Slime.json", AssetsDir);
        var enemy = new Enemy(sheet, x, y);
        _gameObjects.Add(enemy.Id, enemy);
    }

    private void TopUpCoins()
    {
        int count = _gameObjects.Values.OfType<Coin>().Count();
        while (count < CoinTarget)
        {
            SpawnCoin();
            count++;
        }
    }

    private void SpawnCoin()
    {
        int x = _rng.Next(40, Math.Max(41, _worldWidth - 40));
        int y = _rng.Next(40, Math.Max(41, _worldHeight - 40));
        var sheet = SpriteSheet.Load(_renderer, "Coin.json", AssetsDir);
        var coin = new Coin(sheet, x, y);
        _gameObjects.Add(coin.Id, coin);
    }

    private void SpawnBombAtScreen(int sx, int sy)
    {
        if (_world.Status != GameStatus.Playing) return;
        var w = _renderer.ToWorldCoordinates(sx, sy);
        DropBomb(w.X, w.Y);
    }

    private void SpawnBombAtPlayer()
    {
        if (_world.Status != GameStatus.Playing || _player == null) return;
        DropBomb(_player.Position.X, _player.Position.Y);
    }

    private void DropBomb(int x, int y)
    {
        var sheet = SpriteSheet.Load(_renderer, "BombExploding.json", AssetsDir);
        sheet.ActivateAnimation("Explode");
        var bomb = new TemporaryGameObject(sheet, 1.1, (x, y));
        _gameObjects.Add(bomb.Id, bomb);

        // kill enemies within radius after a tiny moment? simplest: instant
        var killed = _gameObjects.Values.OfType<Enemy>()
            .Where(e =>
            {
                int dx = e.Position.X - x;
                int dy = e.Position.Y - y;
                return dx * dx + dy * dy <= BombRadius * BombRadius;
            })
            .ToList();

        foreach (var e in killed)
        {
            _gameObjects.Remove(e.Id);
            _world.AddScore(GameWorld.KillValue);
        }
    }

    private void RestartIfFinished()
    {
        if (_world.Status == GameStatus.Playing) return;

        // wipe the world but keep the high score
        _gameObjects.Clear();
        _world.Reset();
        if (_player != null)
        {
            _player = new PlayerObject(
                SpriteSheet.Load(_renderer, "Player.json", AssetsDir),
                _worldWidth / 2, _worldHeight / 2);
        }
        for (int i = 0; i < CoinTarget; i++) SpawnCoin();
        _lastEnemySpawn = DateTimeOffset.Now;
    }

    public void RenderFrame()
    {
        _renderer.SetDrawColor(0, 0, 0, 255);
        _renderer.ClearScreen();

        if (_player != null)
        {
            _renderer.CameraLookAt(_player.Position.X, _player.Position.Y);
        }

        RenderTerrain();
        RenderAllObjects();
        RenderHud();

        _renderer.PresentFrame();
    }

    private void RenderTerrain()
    {
        foreach (var layer in _level.Layers)
        {
            for (int i = 0; i < (_level.Width ?? 0); i++)
            {
                for (int j = 0; j < (_level.Height ?? 0); j++)
                {
                    int idx = j * (layer.Width ?? 0) + i;
                    var raw = layer.Data[idx];
                    if (raw == null || raw <= 0) continue;
                    int tileId = raw.Value - 1;
                    if (!_tileIdMap.TryGetValue(tileId, out var tile)) continue;

                    int tw = tile.ImageWidth ?? 0;
                    int th = tile.ImageHeight ?? 0;
                    var src = new Rectangle<int>(0, 0, tw, th);
                    var dst = new Rectangle<int>(i * tw, j * th, tw, th);
                    _renderer.RenderTexture(tile.TextureId, src, dst);
                }
            }
        }
    }

    private void RenderAllObjects()
    {
        var toRemove = new List<int>();

        // draw renderables, collect expired temporaries
        foreach (var obj in _gameObjects.Values)
        {
            if (obj is RenderableGameObject r) r.Render(_renderer);

            if (obj is TemporaryGameObject { IsExpired: true } t)
            {
                toRemove.Add(t.Id);
            }
        }

        foreach (var id in toRemove) _gameObjects.Remove(id);

        _player?.Render(_renderer);
    }

    private void RenderHud()
    {
        var (w, _) = _renderer.ViewportSize;

        // hearts
        for (int i = 0; i < GameWorld.MaxHealth; i++)
        {
            byte a = (byte)(i < _world.Health ? 255 : 90);
            _renderer.SetDrawColor(255, 255, 255, a);
            DrawHeart(8 + i * (HeartSize + 4), 8, a);
        }

        // current score (left)
        DrawNumber(_world.Score, 8, 36);

        // high score (right)
        int hsWidth = DigitWidthFor(_world.HighScore);
        DrawNumber(_world.HighScore, w - hsWidth - 8, 8);

        // banner
        switch (_world.Status)
        {
            case GameStatus.Won:
                DrawBanner(_winTex);
                break;
            case GameStatus.Lost:
                DrawBanner(_loseTex);
                break;
        }
    }

    private void DrawHeart(int x, int y, byte alpha)
    {
        var src = new Rectangle<int>(0, 0, HeartSize, HeartSize);
        var dst = new Rectangle<int>(x, y, HeartSize, HeartSize);
        // dim the missing hearts with an overlay rect; the simplest path
        _renderer.RenderTextureScreen(_heartTex, src, dst);
        if (alpha < 200)
        {
            _renderer.FillRectScreen(x, y, HeartSize, HeartSize, 0, 0, 0, 150);
        }
    }

    private int DigitWidthFor(int value)
    {
        return Math.Max(1, value.ToString().Length) * DigitW;
    }

    private void DrawNumber(int value, int x, int y)
    {
        string s = value.ToString();
        for (int i = 0; i < s.Length; i++)
        {
            int digit = s[i] - '0';
            var src = new Rectangle<int>(digit * DigitW, 0, DigitW, DigitH);
            var dst = new Rectangle<int>(x + i * DigitW, y, DigitW, DigitH);
            _renderer.RenderTextureScreen(_digitsTex, src, dst);
        }
    }

    private void DrawBanner(int textureId)
    {
        var (w, h) = _renderer.ViewportSize;
        const int bw = 520, bh = 140;
        var src = new Rectangle<int>(0, 0, bw, bh);
        var dst = new Rectangle<int>((w - bw) / 2, (h - bh) / 2, bw, bh);
        _renderer.RenderTextureScreen(textureId, src, dst);
    }

    public async Task PersistHighScoreAsync()
    {
        await _save.SaveHighScoreAsync(_world.HighScore);
    }
}
