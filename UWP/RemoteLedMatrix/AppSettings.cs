// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using RemoteLedMatrix.Helpers;
    using Windows.Storage;

    /// <summary>
    /// Enumeration of possible states for a connection
    /// </summary>
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
        private bool isListening = false;
        private ApplicationDataContainer localSettings;
        private Connections connectionList;
        private Connection selectedConnection;
        private string[] connectionStateText;

        private static AppSettings instance;

        public AppSettings()
        {
            if (instance == null)
            {
                instance = this;
            }

            this.connectionStateText = Enum.GetNames(typeof(ConnectionState)).ToArray();
            this.DeviceNames = new List<string> { "Bluetooth", "ServerClient" };

            try
            {
                this.localSettings = ApplicationData.Current.LocalSettings;

                this.connectionList = new Connections();
                this.CurrentConnectionState = (int)ConnectionState.NotConnected;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception while using LocalSettings: " + e.ToString());
                throw;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool AutoConnect
        {
            get { return this.GetValueOrDefault(true); }
            set { this.AddOrUpdateValue(value); }
        }

        public bool IsListening
        {
            get
            {
                return this.isListening;
            }

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
            get { return this.GetValueOrDefault(string.Empty); }
            set { this.AddOrUpdateValue(value); }
        }

        public int ConnectionIndex
        {
            get
            {
                return this.GetValueOrDefault(0);
            }

            set
            {
                this.AddOrUpdateValue(value);
                this.OnPropertyChanged("BluetoothVisible");
                this.OnPropertyChanged("NetworkVisible");
            }
        }

        public Connections ConnectionList
        {
            get
            {
                return this.connectionList;
            }

            set
            {
                this.connectionList = value;
                this.OnPropertyChanged("ConnectionList");
                this.OnPropertyChanged("SelectedConnection");
                this.OnPropertyChanged("ListVisible");
                this.OnPropertyChanged("NoListVisible");
            }
        }

        public Connection SelectedConnection
        {
            get
            {
                return this.selectedConnection;
            }

            set
            {
                this.selectedConnection = value;
                this.OnPropertyChanged("SelectedConnection");
                this.OnPropertyChanged("ConnectionList");
            }
        }

        public int CurrentConnectionState
        {
            get
            {
                return this.GetValueOrDefault((int)ConnectionState.NotConnected);
            }

            set
            {
                this.AddOrUpdateValue(value);
                this.OnPropertyChanged("CurrentConnectionStateText");
                this.OnPropertyChanged("CurrentConnectionStateGlyph");
            }
        }

        public string CurrentConnectionStateText => ((ConnectionState)this.CurrentConnectionState).ToString();

        public string CurrentConnectionStateGlyph
        {
            get
            {
                switch (this.CurrentConnectionState)
                {
                    case (int)ConnectionState.Connected:
                        return "\uE8FB";
                    case (int)ConnectionState.Connecting:
                        return "\uE895";
                    case (int)ConnectionState.CouldNotConnect:
                        return "\uE8D0";
                    case (int)ConnectionState.Disconnecting:
                        return "\uE895";
                    case (int)ConnectionState.NotConnected:
                        return "\uE711";
                }

                return string.Empty;
            }
        }

        public List<string> DeviceNames { get; set; }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

        public void ReportChanged(string key)
        {
            this.OnPropertyChanged(key);
        }
    }
}
