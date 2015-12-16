// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix.Helpers
{
    using Windows.Devices.Enumeration;

    /// <summary>
    /// Enumeration of different types of firmata connection supported
    /// </summary>
    public enum ConnectionType
    {
        BluetoothSerial,
        UsbSerial
    }

    /// <summary>
    /// Class representing a serial connection that can be used by firmata
    /// </summary>
    public class Connection
    {
        public Connection(string displayName, object source, ConnectionType connectionType)
        {
            this.DisplayName = displayName;
            this.Source = source as DeviceInformation;
            this.ConnectionType = connectionType;
        }

        public string DisplayName { get; set; }

        public DeviceInformation Source { get; set; }

        public ConnectionType ConnectionType { get; set; }

        public string ConnectionTypeShortName => this.ConnectionType.ToString().Replace("Serial", string.Empty);
    }
}