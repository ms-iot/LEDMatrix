// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix.Helpers
{
    using System.Collections.Generic;
    using Microsoft.Maker.Firmata;

    /// <summary>
    /// Extensions to the firmata client to make sending large amounts of data easier
    /// </summary>
    public static class FirmataExtensions
    {
        public const byte SYSEX_BLOB_COMMAND = 0x7C; // send a series of 7-bit resolution characters

        /// <summary>
        /// Sends a blob of pixel data to the client
        /// </summary>
        /// <param name="firmata">Firmata client to send the command to</param>
        /// <param name="bytes">Pixel data to send</param>
        public static void sendPixelBlob(this UwpFirmata firmata, IEnumerable<byte> bytes)
        {
            firmata.write((byte)Command.START_SYSEX);
            firmata.write(SYSEX_BLOB_COMMAND);

            foreach (byte b in bytes)
            {
                firmata.write(b);
            }

            firmata.write((byte)Command.END_SYSEX);
        }

        /// <summary>
        /// Sends a blob of pixel data to the client, broken up into arbitraily sized chunks
        /// </summary>
        /// <param name="firmata">Firmata client to send the command to</param>
        /// <param name="bytes">Pixel data to send</param>
        /// <param name="inSetsOf">How many bytes to send in a single firmata command</param>
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
