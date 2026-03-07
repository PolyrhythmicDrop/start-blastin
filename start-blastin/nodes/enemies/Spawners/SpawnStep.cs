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

        [Export]
        public SpawnData Data { get; set; }
    }
}
