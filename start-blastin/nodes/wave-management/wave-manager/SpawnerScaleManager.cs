using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autoloads;
using Enemies.Spawners;
using Godot;
using NanoidDotNet;
using SafeResourcePicker;

namespace WaveManagement
{
    [GlobalClass]
    public partial class SpawnerScaleManager : ScaleManager
    {
        // private WaveManager _waveManager;
        private string _defaultSpawnerScaler =
            "res://resources/wave-scalers/spawner-scalers/default-spawner-scaler.tres";
        private string _defaultFormation =
            "res://resources/wave-scalers/spawner-formations/default-spawner-formation.tres";
        private PackedScene _spawnerScene = GD.Load<PackedScene>(
            "res://nodes/enemies/Spawners/EnemySpawner/enemy-spawner.tscn"
        );
        private SpawnerScaler _currentSpawnerScaler;
        private SpawnerFormationScaler _currentFormationScaler;
        private List<SpawnerScaler> _spawnerScalerPool = new();
        private List<SpawnerFormationScaler> _formationPool = new();

        // Active spawners at each spawn location.
        private Dictionary<SpawnerLocation, List<EnemySpawner>> _activeSpawners = new()
        {
            [SpawnerLocation.Bottom] = new(),
            [SpawnerLocation.Top] = new(),
            [SpawnerLocation.Left] = new(),
            [SpawnerLocation.Right] = new(),
        };

        [Export(SRP_HINT.RESOURCE_PATH, "SpawnerScaler")]
        public string DefaultSpawnerScaler
        {
            get => _defaultSpawnerScaler;
            set => _defaultSpawnerScaler = value;
        }

        [Export(SRP_HINT.RESOURCE_PATH, "SpawnerFormationScaler")]
        public string DefaultFormationScaler
        {
            get => _defaultFormation;
            set => _defaultFormation = value;
        }

        public SpawnerScaler CurrentSpawnerScaler => _currentSpawnerScaler;
        public SpawnerFormationScaler CurrentFormation => _currentFormationScaler;

        [Signal]
        public delegate void SpawnersReadyEventHandler();

        public override void _Ready()
        {
            base._Ready();
        }

        public override void Initialize(WaveManager waveManager)
        {
            base.Initialize(waveManager);
            _currentSpawnerScaler = ResourceLoader.Load<SpawnerScaler>(_defaultSpawnerScaler);
            _currentFormationScaler = ResourceLoader.Load<SpawnerFormationScaler>(
                _defaultFormation
            );
        }

        protected override void LoadResourcePools()
        {
            LoadResourcePool<SpawnerScaler>(_spawnerScalerPool);
            LoadResourcePool<SpawnerFormationScaler>(_formationPool);
        }

        public override void SetCurrentScalers(int wave)
        {
            _currentSpawnerScaler = SelectScaler(_spawnerScalerPool, wave, _defaultSpawnerScaler);
            _currentFormationScaler = SelectScaler(_formationPool, wave, _defaultFormation);
        }

        public void ScaleSpawners(EnemyScaler enemyScaler, float difficultyMod)
        {
            int currentWave = _waveManager.Wave;

            EnemyScaler adjustedEnemyScaler = enemyScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );
            SpawnerScaler adjustedSpawnerScaler = _currentSpawnerScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );

            // var spawners = GetTree().GetNodesInGroup("enemy-spawners");
            foreach (KeyValuePair<SpawnerLocation, List<EnemySpawner>> kvp in _activeSpawners)
            {
                foreach (EnemySpawner spawner in kvp.Value)
                {
                    spawner.SetEnemyScaler(adjustedEnemyScaler);
                    spawner.ApplySpawnerScaler(adjustedSpawnerScaler, _waveManager.Wave);
                }
            }

            EventBus.Instance.RaiseSpawnersReady();
        }

        #region Management

        private void PrintScalerProperties(WaveScaler scaler)
        {
            foreach (Godot.Collections.Dictionary property in scaler.GetPropertyList())
            {
                foreach (KeyValuePair<Variant, Variant> kvp in property)
                {
                    if (kvp.Key.ToString() == "name")
                    {
                        StringName stringName = new(kvp.Value.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates and adds spawners to the scene based on the currently-selected formation.
        /// </summary>
        public async Task AssembleFormation()
        {
            // Get the number of spawners that should be in each location.
            foreach (KeyValuePair<SpawnerLocation, List<EnemySpawner>> activeKvp in _activeSpawners)
            {
                // int requestedQuantity = _currentFormationScaler.Formation[activeKvp.Key];
                if (
                    _currentFormationScaler.Formation.TryGetValue(
                        activeKvp.Key,
                        out var requestedQuantity
                    )
                )
                {
                    int activeCount = activeKvp.Value.Count;

                    // If the selected formation scaler requests more spawners in the location than are currently active, add a new spawner to that location
                    if (requestedQuantity > activeCount)
                    {
                        int quantityToAdd = requestedQuantity - activeCount;
                        await AddSpawner(activeKvp.Key, quantityToAdd);
                    }
                    else if (requestedQuantity < activeCount)
                    {
                        int quantityToRemove = activeCount - requestedQuantity;
                        RemoveSpawner(activeKvp.Key, quantityToRemove);
                    }
                }
                else
                {
                    // If the location doesn't exist in the current formation scaler, remove all spawners in that location, since there aren't supposed to be any.
                    RemoveSpawner(activeKvp.Key, activeKvp.Value.Count);
                }
            }
        }

        /// <summary>
        /// Adds the desired <paramref name="quantity"/> of enemy spawners to the passed <paramref name="location"/>.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="quantity"></param>
        private async Task AddSpawner(SpawnerLocation location, int quantity)
        {
            if (quantity == 0)
            {
                return;
            }
            // Get the scene tree and the root level node
            SceneTree tree = GetTree();
            Node levelNode = tree.GetFirstNodeInGroup("level");

            // Set the spawner's position, size, and rotation based on the location.
            Vector2 position;
            float rotationDegrees;
            Curve2D curve;

            switch (location)
            {
                default:
                case SpawnerLocation.Top:
                    position = new Vector2(50, -82);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
                case SpawnerLocation.Left:
                    position = new Vector2(-82, 50);
                    rotationDegrees = 0;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Right:
                    position = new Vector2(2000, 1100);
                    rotationDegrees = 180;
                    curve = curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-left-or-right.tres"
                    );
                    break;
                case SpawnerLocation.Bottom:
                    position = new Vector2(1870, 1162);
                    rotationDegrees = 180;
                    curve = ResourceLoader.Load<Curve2D>(
                        "res://resources/curves/spawner-top-or-bottom.tres"
                    );
                    break;
            }

            for (int i = 0; i < quantity; i++)
            {
                EnemySpawner spawner = _spawnerScene.Instantiate<EnemySpawner>();
                spawner.Name = $"{spawner.GetType().Name}-{Nanoid.Generate(size: 8)}";
                spawner.Curve = curve;
                spawner.Position = position;
                spawner.RotationDegrees = rotationDegrees;
                spawner.Location = location;
                _activeSpawners[location].Add(spawner);
                levelNode.CallDeferred(MethodName.AddChild, spawner);
                await ToSignal(spawner, Node.SignalName.Ready);
            }
        }

        private void RemoveSpawner(SpawnerLocation location, int quantity)
        {
            if (quantity == 0)
            {
                return;
            }
            // Get all the spawners at the specified location
            if (_activeSpawners.TryGetValue(location, out var spawnerList))
            {
                // Get a list of objects to remove using the quantity.
                List<EnemySpawner> removalList = spawnerList.GetRange(0, quantity);
                // Remove the spawners from _activeSpawners list.
                spawnerList.RemoveRange(0, quantity);
                // Free each spawner in the list of objects to remove.
                foreach (EnemySpawner spawner in removalList)
                {
                    spawner.QueueFree();
                }
            }
        }

        #endregion
    }
}
