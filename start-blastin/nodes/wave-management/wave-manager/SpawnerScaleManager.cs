using System;
using System.Collections.Generic;
using System.Reflection;
using Enemies.Spawners;
using Godot;
using SafeResourcePicker;

namespace WaveManagement
{
    [GlobalClass]
    public partial class SpawnerScaleManager : Node
    {
        private WaveManager _waveManager;
        private string _defaultSpawnerScaler;
        private SpawnerScaler _currentSpawnerScaler;
        private List<SpawnerScaler> _spawnerScalerPool = new();

        [Export(SRP_HINT.RESOURCE_PATH, "SpawnerScaler")]
        public string DefaultSpawnerScaler
        {
            get => _defaultSpawnerScaler;
            set => _defaultSpawnerScaler = value;
        }

        public SpawnerScaler CurrentSpawnerScaler => _currentSpawnerScaler;

        public override void _Ready()
        {
            LoadSpawnerScalerPool();
        }

        public void Initialize(WaveManager waveManager)
        {
            _waveManager = waveManager;
        }

        private void LoadSpawnerScalerPool()
        {
            string spawnerScalerDir = "res://resources/wave-scalers/spawner-scalers/";
            string[] configStrings = ResourceLoader.ListDirectory(spawnerScalerDir);

            foreach (string resourceName in configStrings)
            {
                string fullPath = spawnerScalerDir + resourceName;
                GD.Print(
                    $"{MethodBase.GetCurrentMethod().Name}: Adding resource from {fullPath} to spawner scaler pool..."
                );
                _spawnerScalerPool.Add(ResourceLoader.Load<SpawnerScaler>(fullPath));
            }
        }

        public void SetCurrentScaler(int wave)
        {
            try
            {
                // All enemy config resources whose min and max wave encloses the current wave number
                List<SpawnerScaler> matchingConfigs = _spawnerScalerPool.FindAll(config =>
                    (config.MinWave <= wave || config.MinWave == -1)
                    && (config.MaxWave >= wave || config.MaxWave == -1)
                );

                if (matchingConfigs.Count <= 0)
                {
                    // // If we can't find something within our wave range, see if we can find a "default" with a -1 max range.
                    // matchingConfigs = _spawnerScalerPool.FindAll(config => config.MaxWave == -1);

                    // If we STILL can't find anything, throw an exception
                    if (matchingConfigs.Count <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Could not find a spawn scaler that fits the current wave number or is set to infinite! Loading default config at {_defaultSpawnerScaler}..."
                        );
                    }
                }
                else
                {
                    // Create a random number that lands somewhere within the number matching wave configs.
                    int selection = GD.RandRange(0, matchingConfigs.Count - 1);
                    _currentSpawnerScaler = matchingConfigs[selection];
                    GD.Print(
                        $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Current spawner scaler set! Selection: {selection} | Config = {_currentSpawnerScaler.ResourceName}"
                    );
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
                _currentSpawnerScaler = ResourceLoader.Load<SpawnerScaler>(_defaultSpawnerScaler);
            }
        }

        public void ScaleSpawners(EnemyScaler enemyScaler, float difficultyMod)
        {
            int currentWave = _waveManager.Wave;
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: currentWave = {currentWave} | passed enemyScaler = {enemyScaler} | currentSpawnScaler = {_currentSpawnerScaler}"
            );
            EnemyScaler adjustedEnemyScaler = enemyScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );
            SpawnerScaler adjustedSpawnerScaler = _currentSpawnerScaler.GetAdjustedScaler(
                difficultyMod,
                currentWave
            );

            var spawners = GetTree().GetNodesInGroup("enemy-spawners");
            foreach (EnemySpawner spawner in spawners)
            {
                spawner.SetEnemyScaler(adjustedEnemyScaler);
                spawner.ApplySpawnerScaler(adjustedSpawnerScaler, _waveManager.Wave);
            }
        }
    }
}
