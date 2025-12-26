using System;
using System.Collections.Generic;
using Godot;

namespace WaveManagement
{
    public enum SpawnerLocation
    {
        Top,
        Left,
        Right,
        Bottom,
    }

    [GlobalClass]
    public partial class SpawnerConfig : Resource
    {
        private SpawnerLocation _location = SpawnerLocation.Top;

        private Godot.Collections.Array<SpawnerScaler> _scalersGD;

        private List<SpawnerScaler> _scalers;

        public List<SpawnerScaler> Scalers => _scalers;

        [Export]
        public SpawnerLocation Location
        {
            get => _location;
            set => _location = value;
        }

        [Export]
        public Godot.Collections.Array<SpawnerScaler> ScalersGD
        {
            get => _scalersGD;
            set
            {
                _scalers = [.. value];
                _scalersGD = value;
            }
        }
    }
}
