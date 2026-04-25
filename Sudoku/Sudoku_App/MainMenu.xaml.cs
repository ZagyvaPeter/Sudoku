using Microsoft.Maui.Controls;
using System;

namespace Sudoku_App
{
    public partial class MainMenu : ContentPage
    {
        public MainMenu()
        {
            InitializeComponent();
            CheckForSavedGame();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CheckForSavedGame();
        }

        private async void CheckForSavedGame()
        {
            bool hasSavedGame = await GameState.HasSavedGame();
            ContinueButton.IsVisible = hasSavedGame;
        }

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new MainPage(true));
        }

        private async void OnEasyClicked(object sender, EventArgs e)
        {
            await GameState.ClearSavedGame();
            await Navigation.PushAsync(new MainPage("easy"));
        }

        private async void OnMediumClicked(object sender, EventArgs e)
        {
            await GameState.ClearSavedGame();
            await Navigation.PushAsync(new MainPage("medium"));
        }

        private async void OnHardClicked(object sender, EventArgs e)
        {
            await GameState.ClearSavedGame();
            await Navigation.PushAsync(new MainPage("hard"));
        }

        private async void OnExpertClicked(object sender, EventArgs e)
        {
            await GameState.ClearSavedGame();
            await Navigation.PushAsync(new MainPage("expert"));
        }

        private async void OnMasterClicked(object sender, EventArgs e)
        {
            await GameState.ClearSavedGame();
            await Navigation.PushAsync(new MainPage("master"));
        }

        private async void OnExtremeClicked(object sender, EventArgs e)
        {
            await GameState.ClearSavedGame();
            await Navigation.PushAsync(new MainPage("extreme"));
        }
    }
}
