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
        public const byte SYSEX_BLOB_COMMAND = 0x7C; // send a series of 7-bit resolution characters
        public const byte START_SYSEX = 0xF0; // start a MIDI Sysex message
        public const byte END_SYSEX = 0xF7; // end a MIDI Sysex message

        public static void sendSysex(this UwpFirmata firmata, byte command)
        {
            firmata.beginSysex(command);
            firmata.endSysex();
        }

        public static void sendPixelBlob(this UwpFirmata firmata, IEnumerable<byte> bytes)
        {
            firmata.write(START_SYSEX);
            firmata.write(SYSEX_BLOB_COMMAND);

            foreach (byte b in bytes)
            {
                firmata.write(b);
            }

            firmata.write(END_SYSEX);
        }

        public static void sendPixelBlob(this UwpFirmata firmata, IEnumerable<byte> bytes, int inSetsOf)
        {
            if (inSetsOf == 0)
            {
                firmata.sendPixelBlob(bytes);
                return;
            }

            bytes.InSetsOf(inSetsOf).ForEach(firmata.sendPixelBlob);
            firmata.flush();
        }
    }
}
