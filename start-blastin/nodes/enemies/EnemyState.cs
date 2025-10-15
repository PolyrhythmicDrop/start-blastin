using System;

namespace Enemies
{
    public enum EnemyMoveState
    {
        Idle,
        Moving,
    }

    public enum EnemyFireState
    {
        Hold,
        Fire,
    }

    public record EnemyState(EnemyMoveState MoveState, EnemyFireState FireState);
}
