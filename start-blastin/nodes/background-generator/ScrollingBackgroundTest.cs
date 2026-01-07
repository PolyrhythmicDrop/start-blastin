using System;
using Godot;
using Utility;

namespace BackgroundGenerator
{
    public partial class ScrollingBackgroundTest : Node
    {
        private BackgroundGenerator _generatorTop;
        private BackgroundGenerator _generatorCenter;
        private BackgroundGenerator _generatorBottom;

        // Materials

        private ShaderMaterial _starStuffMaterial;
        private ShaderMaterial _nebulaeMaterial;

        private Camera2D _camera;

        private float _currentStarStuffSeed;
        private float _secondSeed = 1;
        private float _currentNebulaeSeed;

        public override void _Ready()
        {
            _generatorTop = GetNode<BackgroundGenerator>("%BGG-Top");
            _generatorCenter = GetNode<BackgroundGenerator>("%BGG-Center");
            _generatorBottom = GetNode<BackgroundGenerator>("%BGG-Bottom");

            _starStuffMaterial =
                _generatorCenter.GetNode<ColorRect>("StarStuff").Material as ShaderMaterial;

            _nebulaeMaterial =
                _generatorCenter.GetNode<ColorRect>("Nebulae").Material as ShaderMaterial;

            _currentStarStuffSeed = (float)_starStuffMaterial.GetShaderParameter("seed");
            _currentNebulaeSeed = (float)_nebulaeMaterial.GetShaderParameter("seed");

            _camera = GetNode<Camera2D>("%Camera2D");
        }

        public override void _Process(double delta)
        {
            // float seedFactor = (float)delta * 0.001f;

            // // _currentNebulaeSeed = float.Lerp(
            // //     _currentNebulaeSeed,
            // //     _currentNebulaeSeed * seedFactor,
            // //     1
            // // );
            // // _currentStarStuffSeed = float.Lerp(
            // //     _currentStarStuffSeed,
            // //     _currentStarStuffSeed * seedFactor,
            // //     1
            // // );

            // // _currentStarStuffSeed += seedFactor;
            // // _currentNebulaeSeed += seedFactor;

            _currentStarStuffSeed += 0.000001f;
            _secondSeed += 0.0003f;

            SetSeeds();
        }

        private void SetSeeds()
        {
            _starStuffMaterial.SetShaderParameter("seed", _currentStarStuffSeed);
            _starStuffMaterial.SetShaderParameter("secondSeed", _currentStarStuffSeed);
            _nebulaeMaterial.SetShaderParameter("seed", _currentNebulaeSeed);
        }
    }
}
