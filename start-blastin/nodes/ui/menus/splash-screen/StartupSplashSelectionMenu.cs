using Godot;

namespace UI
{
    public partial class StartupSplashSelectionMenu : SelectionMenu
    {
        private SelectionEntry _newGameLabel;
        private SelectionEntry _optionsLabel;
        private SelectionEntry _quit;

        public SelectionEntry NewGame => _newGameLabel;
        public SelectionEntry Options => _optionsLabel;
        public SelectionEntry Quit => _quit;

        public override void _Ready()
        {
            _newGameLabel = GetNode<SelectionEntry>("%NewGame");
            _optionsLabel = GetNode<SelectionEntry>("%Options");
            _quit = GetNode<SelectionEntry>("%Quit");
            base._Ready();
        }
    }
}
