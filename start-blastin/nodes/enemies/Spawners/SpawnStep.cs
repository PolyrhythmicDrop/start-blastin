using Godot;
using SafeResourcePicker;

namespace Enemies.Spawners
{
    /// <summary>
    /// Resource for spawning specific enemies at specific times. Used as a component a SpawnPlan in a StaticSpawner.
    /// </summary>
    [GlobalClass]
    public partial class SpawnStep : Resource
    {
        /// <summary>
        /// The percent of the time through the wave when the <see cref="EnemyType"/> should spawn.
        /// For example, if <see cref="WaveTimeRatio"/> is set to 0.5, the enemy spawns halfway through the wave.
        /// </summary>
        [Export(PropertyHint.Range, "0,1.0")]
        public float WaveTimeRatio { get; set; } = 0;

        /// <summary>
        /// The position along the <see cref="EnemySpawner" /> path where the enemy should spawn.
        /// For example, if <see cref="SpawnPosition" /> is set to 0.5, the enemy spawns in the middle of the EnemySpawner's path.
        /// </summary>
        /// <remarks>
        /// This is tied directly to the <see cref="EnemySpawner" />'s ProgressRatio along its parent PathFollow2D node.
        /// </remarks>
        [Export(PropertyHint.Range, "0,1.0")]
        public float SpawnPosition { get; set; } = 0;

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
