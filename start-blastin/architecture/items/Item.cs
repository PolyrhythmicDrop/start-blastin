using System.Collections.Generic;
using Effects;
using Godot;

namespace Items
{
    public enum Rarity
    {
        Common = 10,
        Uncommon = 8,
        Rare = 4,
        Legendary = 1,
    }

    [GlobalClass]
    public abstract partial class Item : Resource
    {
        protected StringName _name;
        protected StringName _description;
        protected Rarity _rarity = Rarity.Common;
        protected List<Effect> _effects = new();
        protected Texture2D _icon;
        protected int _fluxCost;
        protected int _byteCost;
        protected bool _scrappable = true;

        public int ScrapValue => CalculateScrapValue();

        [Export]
        public StringName Name
        {
            get => _name;
            set => _name = value;
        }

        [Export(PropertyHint.MultilineText)]
        public StringName Description
        {
            get => _description;
            set => _description = value;
        }

        [Export]
        public Texture2D Icon
        {
            get => _icon;
            set => _icon = value;
        }

        [Export]
        public Rarity Rarity
        {
            get => _rarity;
            set => _rarity = value;
        }

        [ExportGroup("Pricing")]
        [Export]
        public int FluxCost
        {
            get => _fluxCost;
            set => _fluxCost = value;
        }

        [Export]
        public int ByteCost
        {
            get => _byteCost;
            set => _byteCost = value;
        }

        [ExportGroup("Availability")]
        [Export]
        public bool Scrappable
        {
            get => _scrappable;
            set => _scrappable = value;
        }

        [Export]
        public bool AppearsInShop { get; set; } = true;

        [ExportGroup("Effects")]
        [Export]
        public Godot.Collections.Array<Effect> Effects
        {
            get => [.. _effects];
            set => _effects = [.. value];
        }

        public List<Effect> GetEffectList()
        {
            return _effects;
        }

        private int CalculateScrapValue()
        {
            int combined = _fluxCost + _byteCost;
            if (combined > 0)
            {
                return combined / 2;
            }
            else
            {
                return 0;
            }
        }
    }
}
