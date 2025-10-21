using System;
using System.Reflection;
using Enemies;
using Godot;

namespace Factories
{
    public class EnemyFactory
    {
        public static EnemyNode CreateEnemy(EnemyResource enemyResource)
        {
            EnemyNode enemy = null;

            try
            {
                // Instantiate the enemy scene.
                enemy = GD.Load<PackedScene>(enemyResource.ScenePath).Instantiate<EnemyNode>();

                if (enemy == null)
                {
                    throw new ArgumentException(
                        $"Could not instantiate scene based on the scene path of {enemyResource}!",
                        paramName: nameof(enemyResource)
                    );
                }
                else if (enemy is EnemyNode enemyNode)
                {
                    // Initialize the enemy's stats and weapon based on the passed resource
                    // GD.Print(
                    //     $"{MethodBase.GetCurrentMethod().ReflectedType}.{MethodBase.GetCurrentMethod().Name}: Building new enemy from factory. Crash damage: {enemyResource.CrashDamage} | Speed: {enemyResource.Speed}"
                    // );
                    enemyNode.Initialize(enemyResource);
                    return enemyNode;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Instantiated Enemy object is not an EnemyNode!"
                    );
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"{e.Source}: {e.Message}");
                return enemy;
            }
        }
    }
}
