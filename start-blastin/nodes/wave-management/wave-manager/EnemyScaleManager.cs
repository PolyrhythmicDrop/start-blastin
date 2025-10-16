using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using SafeResourcePicker;

namespace WaveManagement
{
    [GlobalClass]
    public partial class EnemyScaleManager : Node
    {
        private WaveManager _waveManager;
        private string _defaultEnemyScaler;
        private EnemyScaler _currentEnemyScaler;
        private List<EnemyScaler> _enemyScalerPool = new();

        [Export(SRP_HINT.RESOURCE_PATH, "EnemyScaler")]
        public string DefaultEnemyScaler
        {
            get => _defaultEnemyScaler;
            set => _defaultEnemyScaler = value;
        }

        public EnemyScaler CurrentEnemyScaler => _currentEnemyScaler;

        public override void _Ready()
        {
            LoadEnemyScalerPool();
        }

        public void Initialize(WaveManager waveManager)
        {
            _waveManager = waveManager;
        }

        // <summary>
        /// Loads all configuration resources from their parent directory to populate their respective config pools.
        /// Runs once on _Ready(). Wave configurations are selected from the loaded resources based on the current wave and the resource's wave threshold.
        /// </summary>
        private void LoadEnemyScalerPool()
        {
            string enemyScalerDir = "res://resources/wave-scalers/enemy-scalers/";
            string[] configStrings = ResourceLoader.ListDirectory(enemyScalerDir);

            foreach (string resourceName in configStrings)
            {
                string fullPath = enemyScalerDir + resourceName;
                GD.Print(
                    $"{MethodBase.GetCurrentMethod().Name}: Adding resource from {fullPath} to enemy config pool..."
                );
                _enemyScalerPool.Add(ResourceLoader.Load<EnemyScaler>(fullPath));
            }
        }

        public void SetCurrentScaler(int wave)
        {
            try
            {
                // All enemy config resources whose min and max wave encloses the current wave number
                List<EnemyScaler> matchingConfigs = _enemyScalerPool.FindAll(config =>
                    config.MinWave <= wave && config.MaxWave >= wave
                );

                if (matchingConfigs.Count <= 0)
                {
                    // If we can't find something within our wave range, see if we can find a "default" with a -1 max range.
                    matchingConfigs = _enemyScalerPool.FindAll(config => config.MaxWave == -1);

                    // If we STILL can't find anything, throw an exception
                    if (matchingConfigs.Count <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Could not find a wave configuration that fits the current wave number or is set to infinite! Loading default config at {_defaultEnemyScaler}..."
                        );
                    }
                }
                else
                {
                    // Create a random number that lands somewhere within the number matching wave configs.
                    int selection = GD.RandRange(0, matchingConfigs.Count - 1);
                    _currentEnemyScaler = matchingConfigs[selection];
                    GD.Print(
                        $"{MethodBase.GetCurrentMethod().Name}: Current enemy config set! Selection: {selection} | Config = {_currentEnemyScaler.ResourceName}"
                    );
                }
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
                _currentEnemyScaler = ResourceLoader.Load<EnemyScaler>(_defaultEnemyScaler);
            }
        }
    }
}
