using System;
using Entities;
using Godot;

public partial class PlayerController : Node
{
    private Player _player;
    private float _xDir;
    private float _yDir;
    private bool _firing;

    public float xDirection => _xDir;
    public float yDirection => _yDir;
    public bool Firing => _firing;

    public void Initialize(Player player)
    {
        _player = player;
    }

    public override void _Process(double delta)
    {
        SetMovementDirection();
        SetFiring();
    }

    public void SetMovementDirection()
    {
        _xDir = Input.GetAxis("move-left", "move-right");
        _yDir = Input.GetAxis("move-up", "move-down");
    }

    public void SetFiring()
    {
        if (Input.IsActionPressed("fire"))
        {
            _firing = true;
        }
        else
        {
            _firing = false;
        }
    }
}
