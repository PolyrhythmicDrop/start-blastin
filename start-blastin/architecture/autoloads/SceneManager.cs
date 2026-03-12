using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackgroundGenerator;
using Entities;
using Godot;
using SafeResourcePicker;
using Services;
using UI;
using Utility;
using WaveManagement;

namespace Autoloads
{
    public partial class SceneManager : Node
    {
        // Static instance singleton for global access.
        public static SceneManager Instance { get; private set; }
        private string _defaultScenePath;
        private string _overrideScenePath;
        private Node _currentSceneRoot;

        // ~~ Cached scenes ~~

        private PackedScene _startupScreenScene = GD.Load<PackedScene>("uid://cycox7vegt6tq");
        private PackedScene _newGameScene = GD.Load<PackedScene>("uid://dgcswjykgey5y");
        private PackedScene _backgroundScene = GD.Load<PackedScene>("uid://bq7imdtl082ko");
        private PackedScene _playerScene = GD.Load<PackedScene>("uid://nenl15kjphyb");
        private PackedScene _playerUiScene = GD.Load<PackedScene>(
            "res://nodes/ui/ui-layer/ui-layer.tscn"
        );
        private PackedScene _waveManagerScene = GD.Load<PackedScene>("uid://dxdlpe5yuamuy");

        // ~~~~

        /// <summary>
        /// Whether or not the default scene logic should be overridden with a different scene.
        /// Set the Scene Path metadata in the Godot editor to load a scene (for example, a test level or debug scene).
        /// </summary>
        private bool _shouldOverrideScene = false;

        private int _playerCount = 1;
        public int PlayerCount
        {
            get => _playerCount;
            set => _playerCount = value;
        }

        [Export(SRP_HINT.RESOURCE_PATH, "PackedScene")]
        public string DefaultScenePath
        {
            get => _defaultScenePath;
            set => _defaultScenePath = value;
        }

        [ExportGroup("Override Scene")]
        [Export(PropertyHint.GroupEnable, "Override Scene")]
        public bool OverrideScene
        {
            get => _shouldOverrideScene;
            set => _shouldOverrideScene = value;
        }

        [Export(SRP_HINT.RESOURCE_PATH, "PackedScene")]
        public string OverrideScenePath
        {
            get => _overrideScenePath;
            set => _overrideScenePath = value;
        }

        public override void _Ready()
        {
            Instance = this;

            // SetMinWindowSize();
            InitializeServices();
            InitializeRNG();
            InitializeEnemyFinder();

            bool success;
            if (_shouldOverrideScene)
            {
                success = ChangeScene(GD.Load<PackedScene>(_overrideScenePath));
                if (!success)
                {
                    DebugLogger.LogMessage("Failed to load override scene!", true, true);
                }
                else
                {
                    DebugLogger.LogMessage("Adding players from override scene", true);
                    SetUpPlayersInOverrideScene();
                }
            }
            else
            {
                InitializeBackground();
                success = ChangeScene(GD.Load<PackedScene>(_defaultScenePath));
                if (!success)
                {
                    DebugLogger.LogMessage("Failed to load default scene!", true, true);
                }
            }
        }

        private void InitializeBackground()
        {
            AddChild(_backgroundScene.Instantiate<ScrollingBackground>());
        }

        /// <summary>
        /// Initialize the seeded RNG.
        /// TODO: This should eventually be part of the main menu, where the user can set a seed. For now, putting this at the start of Ready() so I know it's called before we start making random number calls.
        /// </summary>
        private void InitializeRNG()
        {
            RNG.InitializeRNG();
        }

        private void InitializeServices()
        {
            // Add PlayerService
            ServiceManager.Instance.RegisterService(new PlayerService());
            ServiceManager.Instance.RegisterService(AudioService.Instance);
        }

        private void InitializeEnemyFinder()
        {
            EnemyFinder.Initialize();
        }

        private void SetMinWindowSize()
        {
            Vector2I minSize = Vector2I.Zero;
            minSize.X = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
            minSize.Y = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
            GetWindow().MinSize = minSize;
        }

        /// <summary>
        /// Changes the main scene to a new scene.
        /// Removes the current scene root (if any) from the tree and frees it.
        /// Loads the scene at <paramref name="scenePath"/> and adds it as a child of the SceneManager.
        /// </summary>
        /// <param name="scenePath">The path or UID of the scene to load.</param>
        /// <returns></returns>
        private bool ChangeScene(PackedScene scene)
        {
            try
            {
                var pSceneNode =
                    scene.Instantiate()
                    ?? throw new NullReferenceException("Scene node could not be instantiated!");

                if (_currentSceneRoot != null)
                {
                    RemoveChild(_currentSceneRoot);
                    _currentSceneRoot.QueueFree();
                }

                AddChild(pSceneNode);
                _currentSceneRoot = pSceneNode;

                DebugLogger.LogMessage($"{_currentSceneRoot.Name} scene loaded!", true);
                return true;
            }
            catch (NullReferenceException e)
            {
                DebugLogger.LogMessage(e.Message, true, true);
                return false;
            }
        }

        /// <summary>
        /// Gets the players currently present in the tree and adds them to the PlayerService's list of players.
        /// </summary>
        private void SetUpPlayersInOverrideScene()
        {
            var players = GetTree().GetNodesInGroup("players");
            _playerCount = players.Count;

            for (int i = 0; i < _playerCount; i++)
            {
                if (players[i] is Player player)
                {
                    // Set PlayerId
                    player.SetPlayerId(i + 1);
                    player.Name = $"Player_{player.PlayerId}";
                    DebugLogger.LogMessage($"{player.Name} PlayerId: {player.PlayerId}", true);

                    // Add the player to the PlayerService list
                    PlayerService playerService =
                        ServiceManager.Instance.GetService<PlayerService>();
                    playerService.AddPlayer(player);

                    // Instantiate the player's UI
                    UiLayer ui = _playerUiScene.Instantiate<UiLayer>();
                    ui.Initialize(player.PlayerId);
                    _currentSceneRoot.AddChild(ui);
                }
            }
        }

        private async Task<List<Player>> AddPlayers()
        {
            List<Player> players = new();
            Vector2 startPos = GetViewport().GetVisibleRect().Size / 2;
            startPos = new(startPos.X, startPos.Y + 100);

            for (int i = 1; i <= _playerCount; i++)
            {
                Player player = _playerScene.Instantiate<Player>();

                // Set PlayerId
                player.SetPlayerId(i);
                player.Name = $"Player-{player.PlayerId}";

                // Add the player to the PlayerService list
                PlayerService playerService = ServiceManager.Instance.GetService<PlayerService>();

                _currentSceneRoot.AddChild(player);
                // Set the starting position to an offset if there are more than one player so they don't start on top of each other.
                player.GlobalPosition = startPos + new Vector2((i - 1) * 200, 0);
                playerService.AddPlayer(player);

                // Instantiate the player's UI
                UiLayer ui = _playerUiScene.Instantiate<UiLayer>();
                ui.Initialize(player.PlayerId);
                _currentSceneRoot.AddChild(ui);

                players.Add(player);
            }

            return players;
        }

        public async Task LoadNewGame(Difficulty difficulty)
        {
            ChangeScene(_newGameScene);

            // Load the wave manager with the appropriate difficulty
            WaveManager wm = _waveManagerScene.Instantiate<WaveManager>();
            wm.Difficulty = difficulty;
            _currentSceneRoot.AddChild(wm);

            List<Player> players = await AddPlayers();

            EventBus.Instance.RaiseGameInitialized([.. players], 1, wm.WaveTime);

            foreach (Player p in players)
            {
                await p.EnterLevel();
            }

            wm.InitializeFirstWave();
        }
    }
}
