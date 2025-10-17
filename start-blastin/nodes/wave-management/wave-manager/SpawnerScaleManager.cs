using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
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
        private string _defaultFormation;
        private SpawnerScaler _currentSpawnerScaler;
        private SpawnerFormationScaler _currentFormationScaler;
        private List<SpawnerScaler> _spawnerScalerPool = new();
        private List<SpawnerFormationScaler> _formationPool = new();

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

        public override void _Ready()
        {
            LoadResourcePools();
        }

        public void Initialize(WaveManager waveManager)
        {
            _waveManager = waveManager;
        }

        private void LoadResourcePools()
        {
            LoadResourcePool<SpawnerScaler>(_spawnerScalerPool);
            LoadResourcePool<SpawnerFormationScaler>(_formationPool);
        }

        private void LoadResourcePool<T>(List<T> pool)
            where T : WaveScaler
        {
            string directory = "";
            try
            {
                if (typeof(T) == typeof(SpawnerScaler))
                {
                    directory = "res://resources/wave-scalers/spawner-scalers/";
                }
                else if (typeof(T) == typeof(SpawnerFormationScaler))
                {
                    directory = "res://resources/wave-scalers/spawner-formations/";
                }

                if (directory == "")
                {
                    throw new InvalidCastException(
                        $"Type {typeof(T).Name} does not have a valid resource pool for this object!"
                    );
                }

                string[] resourceStrings = ResourceLoader.ListDirectory(directory);
                foreach (string resourceName in resourceStrings)
                {
                    string fullPath = directory + resourceName;
                    GD.Print(
                        $"{MethodBase.GetCurrentMethod().Name}: Adding resource from {fullPath} to {pool}..."
                    );
                    pool.Add(ResourceLoader.Load<T>(fullPath));
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
            }
        }

        public void SetCurrentScalers(int wave)
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Setting current scalers..."
            );
            _currentSpawnerScaler = SelectScaler<SpawnerScaler>(
                _spawnerScalerPool,
                wave,
                _defaultSpawnerScaler
            );
            _currentFormationScaler = SelectScaler<SpawnerFormationScaler>(
                _formationPool,
                wave,
                _defaultFormation
            );
        }

        public T SelectScaler<T>(List<T> pool, int wave, string defaultPath)
            where T : WaveScaler
        {
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Selecting scaler for type {typeof(T).Name} for {wave}, using {defaultPath} as the default path..."
            );
            try
            {
                List<T> matchingConfigs = pool.FindAll(config =>
                    (config.MinWave <= wave || config.MinWave == -1)
                    && (config.MaxWave >= wave || config.MaxWave == -1)
                );
                if (matchingConfigs.Count <= 0)
                {
                    throw new InvalidOperationException(
                        $"Could not find a {typeof(T).Name} that fits wave {wave} or is set to infinite! Loading default config at {defaultPath}..."
                    );
                }

                int selection = GD.RandRange(0, matchingConfigs.Count - 1);
                GD.Print(
                    $"Returning {matchingConfigs[selection].ResourceName} as the selected scaler!"
                );
                return matchingConfigs[selection];
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
                return ResourceLoader.Load<T>(defaultPath);
            }
        }

        public void ScaleSpawners(EnemyScaler enemyScaler, float difficultyMod)
        {
            int currentWave = _waveManager.Wave;
            GD.Print(
                $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: currentWave = {currentWave} | passed enemyScaler = {enemyScaler.ResourceName} | currentSpawnScaler = {_currentSpawnerScaler.ResourceName}"
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
