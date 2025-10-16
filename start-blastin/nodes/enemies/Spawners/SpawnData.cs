using System;
using Godot;

namespace Enemies
{
    [GlobalClass]
    public partial class SpawnData : Resource
    {
        private EnemyResource _enemyResource;
        private int _weight;

        [Export]
        public EnemyResource EnemyResource
        {
            get => _enemyResource;
            set => _enemyResource = value;
        }

        [Export]
        public int Weight
        {
            get => _weight;
            set => _weight = value;
        }
    }
}
