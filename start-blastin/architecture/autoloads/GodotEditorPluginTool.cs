using System;
using System.Diagnostics;
using Godot;

namespace Autoloads
{
    [Tool]
    public partial class GodotEditorPluginTool : EditorPlugin
    {
        public GodotEditorDebugger debugger = new();

        public override void _EnterTree()
        {
            AddDebuggerPlugin(debugger);
        }

        public override void _ExitTree()
        {
            RemoveDebuggerPlugin(debugger);
        }
    }
}
