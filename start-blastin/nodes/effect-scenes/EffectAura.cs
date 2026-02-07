using System;
using Godot;
using Interfaces;

namespace Effects
{
    [Tool]
    [GlobalClass]
    public partial class EffectAura : Area2D, IListener
    {
        [Export]
        public AnimatedSprite2D AuraTexture { get; set; }

        [Export]
        public CollisionShape2D CollisionShapeNode { get; set; }

        [Export]
        public CircleShape2D CircleShape
        {
            get
            {
                if (CollisionShapeNode != null)
                {
                    return (CircleShape2D)CollisionShapeNode.Shape;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (CollisionShapeNode != null)
                {
                    CollisionShapeNode.Shape = value;
                }
            }
        }

        public Action<GodotObject> EffectEnableCallback;
        public Action<GodotObject> EffectDisableCallback;

        [Export]
        public float AuraRadius
        {
            get
            {
                if (CircleShape != null)
                {
                    return CircleShape.Radius;
                }
                else
                {
                    return 0;
                }
            }
            set => ChangeAuraShape(radius: value);
        }

        public override void _Ready()
        {
            ConnectSignals();
            if (AuraTexture != null)
            {
                AuraTexture.Play();
            }
        }

        public void ConnectSignals()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
            AreaEntered += OnAreaEntered;
            AreaExited += OnAreaExited;
        }

        public void DisconnectSignals()
        {
            BodyEntered -= OnBodyEntered;
            BodyExited -= OnBodyExited;
            AreaEntered -= OnAreaEntered;
            AreaExited -= OnAreaExited;
        }

        public void ChangeAuraShape(float? radius = null)
        {
            if (radius != null)
            {
                if (CircleShape != null)
                {
                    CircleShape.Radius = (float)radius;

                    // Get size of original texture's rect
                    SpriteFrames sprite = AuraTexture.SpriteFrames ?? null;
                    Vector2 textureSize = Vector2.One;
                    if (sprite != null)
                    {
                        textureSize = sprite.GetFrameTexture("default", 0).GetSize();
                    }

                    // Find the scale ratio based on the circle shape's radius.
                    float scaleRatio = (float)(radius * 2) / textureSize.X;

                    // Set the size of the sprite using the scale ratio
                    AuraTexture.Scale = new Vector2(scaleRatio, scaleRatio);
                }
            }
        }

        private void OnAreaEntered(Area2D area)
        {
            EffectDisableCallback(area);
        }

        private async void OnAreaExited(Area2D area)
        {
            EffectDisableCallback(area);
        }

        private void OnBodyEntered(Node2D body)
        {
            EffectEnableCallback(body);
        }

        private void OnBodyExited(Node2D body)
        {
            EffectDisableCallback(body);
        }

        public override void _ExitTree()
        {
            DisconnectSignals();
            base._ExitTree();
        }
    }
}
