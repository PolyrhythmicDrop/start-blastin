using System.Collections.Generic;
using Godot;

namespace Enemies.Spawners
{
    public class SpawnPool : List<SpawnData>
    {
        public SpawnPool(Godot.Collections.Array<SpawnData> godotArray)
        {
            foreach (SpawnData data in godotArray)
            {
                Add(data);
            }
        }

        public SpawnPool() { }

        public Godot.Collections.Array<SpawnData> ConvertToGodotArray()
        {
            return new(this);
        }
    }
}
