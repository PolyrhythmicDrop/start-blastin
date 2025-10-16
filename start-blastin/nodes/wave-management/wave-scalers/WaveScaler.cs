using Godot;

namespace WaveManagement
{
    [GlobalClass]
    public abstract partial class WaveScaler : Resource
    {
        protected int _minWave = 1;
        protected int _maxWave = -1;

        [ExportCategory("Wave Thresholds")]
        [Export]
        public int MinWave
        {
            get => _minWave;
            set => _minWave = value;
        }

        [Export]
        public int MaxWave
        {
            get => _maxWave;
            set => _maxWave = value;
        }

        public virtual void ApplyDifficultyModifier(float difficultyMod) { }
    }
}
