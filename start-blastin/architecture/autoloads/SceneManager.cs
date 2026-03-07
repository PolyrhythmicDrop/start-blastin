using System;
using System.Collections.Generic;
using Entities;
using Godot;
using SafeResourcePicker;
using Services;
using UI;
using Utility;

namespace Autoloads
{
    public partial class SceneManager : Node
    {
        private string _defaultScenePath;
        private string _overrideScenePath;
        private PackedScene _loadedScene;
        private Node _loadedSceneNode;
        private Node _overrideSceneNode;

        /// <summary>
        /// Whether or not the default scene logic should be overridden with a different scene.
        /// Set the Scene Path metadata in the Godot editor to load a scene (for example, a test level or debug scene).
        /// </summary>
        private bool _shouldOverrideScene = false;

        public PackedScene LoadedScene
        {
            get => _loadedScene;
        }

        private int _playerCount;
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
            // SetMinWindowSize();
            InitializeServices();
            InitializeRNG();
            InitializeEnemyFinder();

            bool success;
            if (_shouldOverrideScene)
            {
                success = LoadScene(_overrideScenePath);
                if (!success)
                {
                    DebugLogger.LogMessage("Failed to load override scene!", true, true);
                }
                else
                {
                    DebugLogger.LogMessage("Adding players...", true);
                    AddPlayers();
                }
            }
            else
            {
                success = LoadScene(_defaultScenePath);
                if (!success)
                {
                    DebugLogger.LogMessage("Failed to load default scene!", true, true);
                }
            }
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

        private bool LoadScene(string scenePath)
        {
            try
            {
                _loadedScene = GD.Load<PackedScene>(scenePath);
                if (_loadedScene == null)
                {
                    throw new NullReferenceException($"Scene at {scenePath} could not be loaded!");
                }
                _loadedSceneNode = _loadedScene.Instantiate();
                if (_loadedSceneNode == null)
                {
                    throw new NullReferenceException("Scene node could not be instantiated!");
                }
                else
                {
                    AddChild(_loadedSceneNode);
                    DebugLogger.LogMessage("Scene loaded!", true);
                    return true;
                }
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
        private void AddPlayers()
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
                    UiLayer ui = GD.Load<PackedScene>("res://nodes/ui/ui-layer/ui-layer.tscn")
                        .Instantiate<UiLayer>();
                    ui.Initialize(player.PlayerId);
                    _loadedSceneNode.AddChild(ui);
                }
            }
        }
    }
}
