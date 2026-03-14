using System;
using System.Threading.Tasks;
using Godot;
using WaveManagement;

namespace UI
{
    public partial class GameOverSelectionMenu : SelectionMenu
    {
        private SelectionEntry _newGame;
        private SelectionEntry _mainMenu;
        private SelectionEntry _quit;

        public SelectionEntry NewGame => _newGame;
        public SelectionEntry MainMenu => _mainMenu;
        public SelectionEntry Quit => _quit;

        public override void _Ready()
        {
            _newGame = GetNode<SelectionEntry>("%NewGame");
            _mainMenu = GetNode<SelectionEntry>("%MainMenu");
            _quit = GetNode<SelectionEntry>("%Quit");
            base._Ready();
        }
    }
}
