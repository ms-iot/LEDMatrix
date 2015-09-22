// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix.Helpers
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Interface for retrieving bytes from a bitmap memory buffer
    /// </summary>
    [ComImport]
    [Guid("5b0d3235-4dba-4d44-865e-8f1d0e4fd04d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }
}
