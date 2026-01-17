using Godot;

namespace Enemies.Spawners
{
    /// <summary>
    /// Resource for generating concrete StaticSpawner objects.
    /// </summary>
    [GlobalClass]
    public partial class StaticSpawnerConfig : SpawnerConfig
    {
        [Export]
        public Godot.Collections.Array<SpawnStep> SpawnPlan { get; set; } = new();
    }
}
