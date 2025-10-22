using System.Collections.Generic;
using Effects;
using Godot;

namespace Items
{
    public abstract partial class Item : Resource
    {
        protected List<Effect> _effects = new();

        [Export]
        public Godot.Collections.Array<Effect> Effects
        {
            get => new(_effects);
            set => _effects = [.. value];
        }
    }
}
