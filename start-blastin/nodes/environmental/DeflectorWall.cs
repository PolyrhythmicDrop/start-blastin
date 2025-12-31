using System;
using Godot;
using Interfaces;

public partial class DeflectorWall : StaticBody2D, IDeflector
{
    public bool DeflectActive { get; set; } = false;
}
