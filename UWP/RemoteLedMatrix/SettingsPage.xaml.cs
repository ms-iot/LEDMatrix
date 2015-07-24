/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

namespace RemoteLedMatrix
{
    using RemoteLedMatrix.Helpers;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    using Microsoft.Maker.Serial;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            App.CurrentAppSettings = (AppSettings) App.Current.Resources["CurrentAppSettings"];
            int index = App.CurrentAppSettings.ConnectionIndex;

            this.InitializeComponent();

            App.CurrentAppSettings.ConnectionIndex = Math.Max(0, index);
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
                var selectedConnection = this.connectList.SelectedItem as Connection;
                var result = await MainPage.Instance.Connect(selectedConnection);
                if (result)
                {
                    await Task.Delay(2000);
                    if (this.Frame.CanGoBack)
                    {
                        try
                        {
                            this.Frame.GoBack();
                        }
                        catch (Exception)
                        {
                            //ignore no back
                        }
                    }
                }
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.PopulateList();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentAppSettings.PreviousConnectionName = string.Empty;

            if (MainPage.Instance.currentConnection != null)
            {
                MainPage.Instance.Disconnect();
            }
        }
        
        private void AlwaysRunning_Toggled(object sender, RoutedEventArgs e)
        {
            MainPage.Instance.CheckAlwaysRunning();
        }

        private void NavBack(object sender, RoutedEventArgs e)
        {
            this.Frame.GoBack();
        }
    }
}
