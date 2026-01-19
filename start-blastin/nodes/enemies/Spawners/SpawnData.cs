using System;
using Godot;
using SafeResourcePicker;

namespace Enemies.Spawners
{
    [GlobalClass]
    public partial class SpawnData : Resource
    {
        // private EnemyResource _enemyResource;
        // private int _weight;

        // [Export]
        // public EnemyResource EnemyResource
        // {
        //     get => _enemyResource;
        //     set => _enemyResource = value;
        // }

        // [Export]
        // public int Weight
        // {
        //     get => _weight;
        //     set => _weight = value;
        // }

        /// <summary>
        /// Type of enemy to spawn at this step.
        /// </summary>
        [Export(SRP_HINT.RESOURCE_PATH, "EnemyResource")]
        public string EnemyType { get; set; }

        private bool _squadEnabled = false;

        [ExportGroup("Squadron Enabled")]
        [Export(PropertyHint.GroupEnable)]
        public bool SquadEnabled
        {
            get => _squadEnabled;
            set { _squadEnabled = value; }
        }

        private float _splitPoint = 0.15f;

        [Export(PropertyHint.Range, "0,1.0")]
        public float SplitPoint
        {
            get => _splitPoint;
            set => _splitPoint = value;
        }

        private SquadronLayout _squadron;

        [Export]
        public SquadronLayout Squadron
        {
            get => _squadron;
            set => _squadron = value;
        }
    }
}
