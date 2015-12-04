// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix
{
    using RemoteLedMatrix.Helpers;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// Page for all the configurable settings, as well as managing the firmata serial connection
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            App.CurrentAppSettings = (AppSettings)Application.Current.Resources["CurrentAppSettings"];

            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage.Instance.IsInSettings = true;
            MainPage.Instance.PopulateList();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            MainPage.Instance.IsInSettings = false;
            base.OnNavigatingFrom(e);
        }

        private async void Reconnect_Click(object sender, RoutedEventArgs e)
        {
            if (this.connectList.SelectedItem != null)
            {
                App.CurrentAppSettings.SelectedConnection = this.connectList.SelectedItem as Connection;
                await MainPage.Instance.Connect(App.CurrentAppSettings.SelectedConnection);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.PopulateList();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentAppSettings.PreviousConnectionName = string.Empty;

            if (MainPage.Instance.CurrentConnection != null)
            {
                MainPage.Instance.Disconnect();
            }
        }

        private void AlwaysRunning_Toggled(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.CheckAlwaysRunning();
        }

        private void SettingsToggle_Unclick(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}
