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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using RemoteLedMatrix.Helpers;

namespace RemoteLedMatrix
{
    public enum ConnectionState
    {
        NotConnected = 0,
        Connecting = 1,
        Connected = 2,
        CouldNotConnect = 3,
        Disconnecting = 4
    }

    public class AppSettings : INotifyPropertyChanged
    {
        private Windows.Storage.ApplicationDataContainer localSettings;
        private Connections connectionList;

        public event PropertyChangedEventHandler PropertyChanged;
        private string[] ConnectionStateText;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static AppSettings Instance;

        private bool isListening = false;

        public AppSettings()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            this.ConnectionStateText = Enum.GetNames(typeof (ConnectionState)).ToArray();
            this.DeviceNames = new List<string> { "Bluetooth", "ServerClient" };

            try
            {
                this.localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

                this.connectionList = new Connections();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while using LocalSettings: " + e.ToString());
                throw;
            }
        }

        public bool AddOrUpdateValue(Object value, [CallerMemberName] string Key = null)
        {
            bool valueChanged = false;

            if (this.localSettings.Values.ContainsKey(Key))
            {
                if (this.localSettings.Values[Key] != value)
                {
                    this.localSettings.Values[Key] = value;
                    valueChanged = true;
                }
            }
            else
            {
                this.localSettings.Values.Add(Key, value);
                valueChanged = true;
            }

            return valueChanged;
        }

        public T GetValueOrDefault<T>(T defaultValue, [CallerMemberName] string key = null)
        {
            T value;

            // If the key exists, retrieve the value.
            if (this.localSettings.Values.ContainsKey(key))
            {
                value = (T)this.localSettings.Values[key];
            }
            else
            {
                value = defaultValue;
            }

            return value;
        }

        public bool Remove(Object value, [CallerMemberName] string key = null)
        {
            if (this.localSettings.Values.ContainsKey(key))
            {
                this.localSettings.DeleteContainer(key);
                return true;
            }

            return false;
        }

        public bool AutoConnect
        {
            get { return this.GetValueOrDefault(true); }
            set { this.AddOrUpdateValue(value); }
        }

        public bool IsListening
        {
            get { return this.isListening; }
            set
            {
                this.isListening = value;
                this.OnPropertyChanged("IsListening");
            }
        }

        public bool AlwaysRunning
        {
            get { return this.GetValueOrDefault(true); }
            set { this.AddOrUpdateValue(value); }
        }

        public bool NoListVisible => !this.ListVisible;

        public bool ListVisible => this.ConnectionList != null && this.ConnectionList.Any();

        public string PreviousConnectionName
        {
            get { return this.GetValueOrDefault(""); }
            set { this.AddOrUpdateValue(value); }
        }

        public int ConnectionIndex
        {
            get { return this.GetValueOrDefault(0); }
            set
            {
                this.AddOrUpdateValue(value);
                this.OnPropertyChanged("BluetoothVisible");
                this.OnPropertyChanged("NetworkVisible");
            }
        }

        public Connections ConnectionList
        {
            get { return this.connectionList; }
            set
            {
                this.connectionList = value;
                this.OnPropertyChanged("ConnectionList");
                this.OnPropertyChanged("ListVisible");
                this.OnPropertyChanged("NoListVisible");
            }
        }

        public int CurrentConnectionState
        {
            get { return this.GetValueOrDefault((int) ConnectionState.NotConnected); }
            set {
                this.AddOrUpdateValue(value);
                this.OnPropertyChanged("CurrentConnectionStateText");
            }
        }

        public string CurrentConnectionStateText => ((ConnectionState)this.CurrentConnectionState).ToString();

        public List<string> DeviceNames { get; set; }

        public void ReportChanged(string key)
        {
            this.OnPropertyChanged(key);
        }
    }
}
