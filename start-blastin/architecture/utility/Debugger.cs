using System;
using Autoloads;
using Enemies;
using Godot;
using WaveManagement;

namespace Utility
{
    /// <summary>
    /// Used to provide "cheats" to the developer (i.e. me) for easier testing.
    /// Add to a scene tree to gain access to its amazing powers.
    /// </summary>
    [GlobalClass]
    public partial class Debugger : Node
    {
        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressedByEvent("debug-end-wave", @event, true))
            {
                DebugEndWave();
            }
        }

        private void DebugEndWave()
        {
            WaveManager waveManager = GetTree().GetNodesInGroup("wave-manager")[0] as WaveManager;
            // Kill all enemies
            var enemies = GetTree().GetNodesInGroup("enemies");
            foreach (EnemyNode enemy in enemies)
            {
                enemy.Die();
            }
            waveManager.DebugEndWave();
        }
    }
}
