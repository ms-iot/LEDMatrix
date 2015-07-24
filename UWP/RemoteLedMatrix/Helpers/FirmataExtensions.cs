using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maker.Firmata;

namespace RemoteLedMatrix.Helpers
{
    public static class FirmataExtensions
    {
        public static void sendSysex(this UwpFirmata firmata, byte command)
        {
            firmata.beginSysex(command);
            firmata.endSysex();
        }

        public static void sendBlob(this UwpFirmata firmata, IEnumerable<byte> bytes)
        {
            firmata.beginBlob();

            foreach (byte b in bytes)
            {
                firmata.appendBlob(b);
            }

            firmata.endBlob();
        }

        public static void sendBlob(this UwpFirmata firmata, IEnumerable<byte> bytes, int inSetsOf)
        {
            if (inSetsOf == 0)
            {
                firmata.sendBlob(bytes);
                return;
            }

            bytes.InSetsOf(inSetsOf).ForEach(firmata.sendBlob);
        }
    }
}
