using System;
using Godot;

namespace Interfaces
{
    /// <summary>
    /// Interface for objects that apply a color scheme
    /// </summary>
    public interface IColorScheme
    {
        void ApplyColorScheme(GradientTexture1D scheme);
    }
}
