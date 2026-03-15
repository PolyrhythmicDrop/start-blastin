using System;
using System.Threading.Tasks;
using Entities;
using Godot;
using Utility;

namespace PlayerComponents
{
    public partial class AnimationComponent : Node2D
    {
        private Player _player;
        private Node2D _spriteContainer;
        private AnimatedSprite2D _engineEffectSprite;
        private Sprite2D _engineSprite;
        private Sprite2D _bodySprite;
        private AnimatedSprite2D _phaseSprite;
        private AnimatedSprite2D _destructionSprite;
        private AnimatedSprite2D _phaseReadySprite;
        private ShaderMaterial _hitEffectShaderMat;

        private PackedScene _portalScene = GD.Load<PackedScene>("uid://c32k0p00itnlf");

        private const string MIX_RATIO_PATH = "mix_ratio";
        private const string CURRENT_FRAME_PATH = "current_frame";
        private const float DAMAGE_ANIM_DUR = 0.5f;

        public override void _Ready()
        {
            _spriteContainer = GetNode<Node2D>("%SpriteContainer");
            _engineEffectSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%EngineEffect");
            _engineSprite = _spriteContainer.GetNode<Sprite2D>("%Engine");
            _bodySprite = _spriteContainer.GetNode<Sprite2D>("%Body");
            _destructionSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%Destruction");
            _phaseSprite = _spriteContainer.GetNode<AnimatedSprite2D>("%Phase");
            _phaseReadySprite = _spriteContainer.GetNode<AnimatedSprite2D>("%PhaseReadyEffect");
            _hitEffectShaderMat = ResourceLoader.Load<ShaderMaterial>(
                "res://resources/materials/hit-effect.tres"
            );

            ConnectSignals();
        }

        public void Initialize(Player player)
        {
            _player = player;
        }

        public async Task PlayPortalAnimation()
        {
            // Hide the player
            _player.Hide();

            // Set starting variables
            Color defaultColor = _player.Modulate;
            Color trans = new(1, 1, 1, 0);
            Color white = new(1, 1, 1, 1);
            white = white.Lightened(10.0f);
            // white.OkHslL = 10;

            // Set the player's starting modulation to transparent.
            _player.Modulate = trans;
            _player.Show();

            // Wait for a process frame so GlobalPosition has time to computer after _Ready()
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            // Set up the portal node
            AnimatedSprite2D portal = _portalScene.Instantiate<AnimatedSprite2D>();
            portal.ZIndex = _player.ZIndex - 1;
            _player.GetParent().AddChild(portal);
            portal.GlobalPosition = _player.GlobalPosition;

            // Create the callables for tweening
            Callable portalOpen = Callable.From(() => portal.Play("open"));
            Callable portalLoop = Callable.From(() => portal.Play("loop"));
            Callable portalClose = Callable.From(() => portal.PlayBackwards("open"));

            _player.Scale = Vector2.Zero;

            portal.Play("open");

            await ToSignal(portal, AnimatedSprite2D.SignalName.AnimationFinished);

            portal.Play("loop");

            Tween t = CreateTween();

            // t.TweenCallback(portalLoop);
            t.SetParallel(true);
            t.TweenProperty(_player, "modulate", defaultColor, 1f);
            t.TweenProperty(_player, "scale", Vector2.One, 1f).From(Vector2.Zero);
            t.SetParallel(false);
            // t.TweenProperty(_player, "modulate", defaultColor, 2);
            t.Chain().TweenProperty(_player.Controller, "Enabled", true, 0);
            t.TweenCallback(portalClose);
            t.TweenProperty(portal, "modulate", trans, 2);

            await ToSignal(portal, AnimatedSprite2D.SignalName.AnimationFinished);
            await ToSignal(t, Tween.SignalName.Finished);

            portal.Hide();
            portal.QueueFree();
        }

        public void ConnectSignals()
        {
            _phaseReadySprite.AnimationFinished += () =>
            {
                _phaseReadySprite.Visible = false;
            };
        }

        public override void _Process(double delta)
        {
            if (!_player.State.Dying)
            {
                if (_player.Velocity != Vector2.Zero)
                {
                    _engineEffectSprite.Play("full-power");
                }
                else
                {
                    _engineEffectSprite.Play("idle");
                }
            }
        }

        public void PlayDieAnimation()
        {
            _engineEffectSprite.Hide();
            _engineSprite.Hide();
            _bodySprite.Hide();

            _destructionSprite.Visible = true;
            _destructionSprite.Play("full-explosion");
            _destructionSprite.AnimationFinished += _player.Despawn;
        }

        public void PlayDamageAnimation()
        {
            if (Material is ShaderMaterial shaderMaterial)
            {
                shaderMaterial.SetShaderParameter(MIX_RATIO_PATH, 1.0);

                Tween tween = CreateTween();
                tween.TweenMethod(
                    Callable.From(
                        (int currentFrame) =>
                            shaderMaterial.SetShaderParameter(CURRENT_FRAME_PATH, currentFrame)
                    ),
                    0,
                    30,
                    DAMAGE_ANIM_DUR
                );
                tween.TweenCallback(
                    Callable.From(() => shaderMaterial.SetShaderParameter(MIX_RATIO_PATH, 0))
                );
            }
        }

        /// <summary>
        /// Toggles the player's dodge animation on or off.
        /// </summary>
        /// <param name="on">When true, turns on the dodge animation. When false, turns it off.</param>
        public void TogglePhaseAnimation(bool on)
        {
            if (on)
            {
                _phaseSprite.Visible = true;
                _phaseSprite.Play("default");
            }
            else
            {
                _phaseSprite.Visible = false;
                _phaseSprite.Stop();
            }
        }

        public void PlayPhaseReadyEffect()
        {
            _phaseReadySprite.Visible = true;
            _phaseReadySprite.Play("default");
        }
    }
}
