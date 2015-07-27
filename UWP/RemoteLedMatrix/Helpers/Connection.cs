namespace RemoteLedMatrix.Helpers
{

    public class Connection
    {
        public string DisplayName { get; set; }
        public object Source { get; set; }

        public ConnectionType ConnectionType { get; set; }

        public string ConnectionTypeShortName => this.ConnectionType.ToString().Replace("Serial", string.Empty);

        public Connection(string displayName, object source, ConnectionType connectionType)
        {
            this.DisplayName = displayName;
            this.Source = source;
            this.ConnectionType = connectionType;
        }
    }

    public enum ConnectionType
    {
        BluetoothSerial,
        UsbSerial
    }
}