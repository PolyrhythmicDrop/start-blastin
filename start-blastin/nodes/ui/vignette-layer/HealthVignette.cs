using Autoloads;
using Events;
using Godot;
using Interfaces;

namespace UI
{
    [GlobalClass]
    public partial class HealthVignette : CanvasLayer, IListener
    {
        private ColorRect _vignetteRect;
        private ShaderMaterial _vignetteMaterial;
        private Tween _vignetteTween;
        private bool _vignetteEnabled = false;

        public override void _Ready()
        {
            _vignetteRect = GetNode<ColorRect>("%VignetteRect");
            if (_vignetteRect.Material is ShaderMaterial shaderMaterial)
            {
                _vignetteMaterial = shaderMaterial;
            }

            ConnectSignals();
        }

        public void ConnectSignals()
        {
            EventBus.Instance.PlayerCurrentHealthChanged += OnPlayerCurrentHealthChanged;
        }

        public void DisconnectSignals()
        {
            EventBus.Instance.PlayerCurrentHealthChanged -= OnPlayerCurrentHealthChanged;
        }

        private void OnPlayerCurrentHealthChanged(
            object source,
            PlayerCurrentHealthChangedEventArgs args
        )
        {
            if (args.Percentage <= 0.1f && !_vignetteEnabled)
            {
                EnableLowHealthVignette();
            }
            else if (args.Percentage > 0.1f && _vignetteEnabled)
            {
                DisableLowHealthVignette();
            }
        }

        private void EnableLowHealthVignette()
        {
            _vignetteEnabled = true;
            if (_vignetteTween != null && _vignetteTween.IsValid())
            {
                _vignetteTween.Kill();
            }

            float maxIntensity = 0.45f;
            float minIntensity = 0.1f;

            Callable intensityCallable = Callable.From(
                (float i) =>
                {
                    _vignetteMaterial.SetShaderParameter("intensity", i);
                }
            );

            _vignetteTween = _vignetteRect.CreateTween();
            _vignetteTween.SetLoops();
            _vignetteTween
                .TweenMethod(intensityCallable, minIntensity, maxIntensity, 0.5f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
            _vignetteTween
                .TweenMethod(intensityCallable, maxIntensity, minIntensity, 0.5f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        private void DisableLowHealthVignette()
        {
            _vignetteEnabled = false;
            if (_vignetteTween != null && _vignetteTween.IsValid())
            {
                _vignetteTween.Kill();
            }

            Callable intensityCallable = Callable.From(
                (float i) => _vignetteMaterial.SetShaderParameter("intensity", i)
            );
            float currentIntensity = (float)_vignetteMaterial.GetShaderParameter("intensity");

            _vignetteTween = _vignetteRect.CreateTween();

            _vignetteTween
                .TweenMethod(intensityCallable, currentIntensity, 0, 0.5f)
                .SetTrans(Tween.TransitionType.Sine)
                .SetEase(Tween.EaseType.In);
        }

        public override void _ExitTree()
        {
            // DisableLowHealthVignette();
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
