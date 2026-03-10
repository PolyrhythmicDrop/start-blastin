using System;
using Godot;

namespace UI
{
    public partial class WaveCountdownNode2D : RichTextLabel
    {
        private Font _font = ResourceLoader.Load<Font>("uid://cl4qr5jjiwdn4");

        private const int FONT_SIZE = 120;

        // public string Text { get; set; } = "3";

        // public override void _Draw()
        // {
        //     DrawString(
        //         _font,
        //         Position,
        //         Text,
        //         alignment: HorizontalAlignment.Center,
        //         fontSize: FONT_SIZE,
        //         modulate: new Color(1, 1, 1, 1)
        //     );
        // }
    }
}
