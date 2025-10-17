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
    public partial class SpawnerScaleManager : ScaleManager
    {
        // private WaveManager _waveManager;
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
