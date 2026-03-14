using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UI
{
    public partial class GameOverScreen : MenuScreen
    {
        private GameOverSelectionMenu _gameOverMenu;

        private DifficultySelectionMenu _diffSelMenu;

        public override void _Ready()
        {
            _gameOverMenu = GetNode<GameOverSelectionMenu>("%GameOverSelectionMenu");
            _gameOverMenu.PivotOffsetRatio = new(0.5f, 0.5f);

            _diffSelMenu = GetNode<DifficultySelectionMenu>("%DifficultyMenu");

            _initMenu = _gameOverMenu;

            base._Ready();
        }

        protected override void AssignMenuActions()
        {
            _gameOverMenu.NewGame.SetSelectAction(async () => await SwitchMenu(_diffSelMenu));
            _gameOverMenu.MainMenu.SetSelectAction(ReturnToMainMenu);
            _gameOverMenu.Quit.SetSelectAction(() =>
            {
                GetTree().Quit();
                return Task.CompletedTask;
            });

            _diffSelMenu.SetDifficultyActions(
                StartNewGame,
                async () => await SwitchMenu(_prevMenu)
            );
        }
    }
}
