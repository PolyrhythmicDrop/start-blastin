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
        public Godot.Collections.Array<SpawnStep> SpawnSteps { get; set; } = new();

        public override void ConfigureSpawner(EnemySpawner spawner, double? waveTime = null)
        {
            if (spawner is not StaticSpawner staticSpawner)
            {
                return;
            }

            base.ConfigureSpawner(staticSpawner, waveTime);

            ConfigureStaticSpawner(staticSpawner, waveTime);
        }

        private void ConfigureStaticSpawner(StaticSpawner spawner, double? waveTime = null)
        {
            spawner.SpawnSteps = [.. SpawnSteps];

            spawner.BuildSpawnPlan(waveTime);
        }
    }
}
