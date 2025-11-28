using System;
using Godot;
using Projectiles;

[GlobalClass]
public partial class Missile : Projectile
{
    public static string ScenePath => "res://nodes/weapons/projectiles/missile/missile.tscn";
    private AnimatedSprite2D _sprite;

    // Targeting variables
    private Area2D _targetingArea;
    private Node2D _currentTarget;

    public override void _Ready()
    {
        base._Ready();
        _sprite = GetNode<AnimatedSprite2D>("%Sprite");
        _targetingArea = GetNode<Area2D>("%TargetingArea");
    }
}
