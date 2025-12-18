using System;
using Godot;

namespace Autoloads
{
    public partial class GodotEditorDebugger : EditorDebuggerPlugin
    {
        public override void _SetupSession(int sessionId)
        {
            // Get the editor debugger session with the passed ID
            var session = GetSession(sessionId);

            // Connect the session started callback.
            session.Started += () =>
            {
                // Execute the command, using the open URL argument and launch from launch.json.
                // URL is from CodeLLDB (https://github.com/vadimcn/codelldb/blob/master/MANUAL.md#vscode-url)
                // "Attach Godot" is the name of the configuration in launch.json.
                string argString = "vscode://vadimcn.vscode-lldb/launch?name=AttachGodot";

                GD.Print(argString);
                OS.Execute(
                    path: "C:/Users/dana/AppData/Local/Programs/Microsoft VS Code/Code.exe",
                    arguments: ["--open-url", argString]
                );
            };

            // Connect the session stopped callback
            session.Stopped += () =>
            {
                GD.Print($"Session {sessionId} stopped");
            };
        }
    }
}
