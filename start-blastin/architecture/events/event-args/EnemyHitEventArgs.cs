using System;
using Enemies;

namespace Events
{
    public class EnemyHitEventArgs : EventArgs
    {
        /// <summary>
        /// ID of the player that hit the enemy.
        /// </summary>
        public int PlayerId { get; }

        /// <summary>
        /// Enemy that was hit.
        /// </summary>
        public EnemyNode Enemy { get; }

        public EnemyHitEventArgs(int playerId, EnemyNode enemy)
        {
            PlayerId = playerId;
            Enemy = enemy;
        }
    }
}
