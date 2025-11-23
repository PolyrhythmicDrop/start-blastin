using System;
using Autoloads;
using Enemies;
using Entities;
using Godot;
using Services;
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
        private PlayerService _service;

        public override void _Ready()
        {
            _service = ServiceManager.Instance?.GetService<PlayerService>();
        }

        public override void _Input(InputEvent @event)
        {
            if (Input.IsActionJustPressedByEvent("debug-end-wave", @event, true))
            {
                DebugEndWave();
            }
            if (Input.IsActionJustPressedByEvent("debug-add-flux", @event, true))
            {
                AddFlux();
            }
            if (Input.IsActionJustPressedByEvent("debug-add-bytes", @event, true))
            {
                AddBytes();
            }
            if (Input.IsActionJustPressedByEvent("debug-remove-flux", @event, true))
            {
                RemoveFlux();
            }
            if (Input.IsActionJustPressedByEvent("debug-remove-bytes", @event, true))
            {
                RemoveBytes();
            }
            if (Input.IsActionJustPressedByEvent("debug-heal-player", @event, true))
            {
                HealPlayer();
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

        private void AddFlux()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Flux += 100;
        }

        private void AddBytes()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Bytes += 100;
        }

        private void RemoveFlux()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Flux -= 100;
        }

        private void RemoveBytes()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Bytes -= 100;
        }

        private void HealPlayer()
        {
            Player playerOne = _service.GetPlayer(1);
            playerOne.Heal(playerOne.MaxHealth);
        }
    }
}
