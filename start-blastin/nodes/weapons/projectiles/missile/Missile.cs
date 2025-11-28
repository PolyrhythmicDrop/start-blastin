using System;
using Godot;
using Projectiles;

[GlobalClass]
public partial class Missile : Projectile
{
    public static string ScenePath => "res://nodes/weapons/projectiles/missile/missile.tscn";
    private AnimatedSprite2D _sprite;

    public override void _Ready()
    {
        base._Ready();
        _sprite = GetNode<AnimatedSprite2D>("%Sprite");
    }
}
