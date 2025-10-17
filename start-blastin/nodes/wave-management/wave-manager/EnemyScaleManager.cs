using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using SafeResourcePicker;

namespace WaveManagement
{
    [GlobalClass]
    public partial class EnemyScaleManager : ScaleManager
    {
        // private WaveManager _waveManager;
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

        protected override void LoadResourcePools()
        {
            LoadResourcePool(_enemyScalerPool);
        }

        public override void SetCurrentScalers(int wave)
        {
            _currentEnemyScaler = SelectScaler(_enemyScalerPool, wave, _defaultEnemyScaler);
        }
    }
}
