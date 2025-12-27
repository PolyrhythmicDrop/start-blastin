using System;
using System.Collections.Generic;
using Enemies;
using Enemies.Spawners;
using Godot;

namespace WaveManagement
{
    public enum SpawnerLocation
    {
        Top,
        Left,
        Right,
        Bottom,
    }

    /// <summary>
    /// Configuration for an EnemySpawner object, including the spawner's location and <see cref="SpawnPool"/>,
    /// Used by a SpawnerFormationScaler and the ScaleManager to generate spawners.
    /// </summary>
    [GlobalClass]
    public partial class SpawnerConfig : Resource
    {
        private SpawnerLocation _location = SpawnerLocation.Top;

        private SpawnPool _spawnPool = new();

        private Godot.Collections.Array<SpawnData> _spawnPoolGD;

        [Export]
        public SpawnerLocation Location
        {
            get => _location;
            set => _location = value;
        }

        public SpawnPool SpawnPool => _spawnPool;

        [Export]
        public Godot.Collections.Array<SpawnData> SpawnPoolGD
        {
            get => _spawnPoolGD;
            set
            {
                _spawnPool = new(value);
                _spawnPoolGD = value;
            }
        }
    }
}
