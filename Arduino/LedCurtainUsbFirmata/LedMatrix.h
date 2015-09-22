/*
Copyright(c) Microsoft Open Technologies, Inc.All rights reserved.

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

#ifndef LedMatrix_h
#define LedMatrix_h

#include <SPI.h>

// Custom SYSEX command for high-speed data transfer.  
#define SYSEX_BLOB_COMMAND 0x7C

// Custom SYSEX commands related to the LED Matrix
#define LED_RESET 0x43

class LedMatrix {
  public:
    void begin();
    void reset();
    void clear();
    void processPixelBlob(byte argc, byte *argv);
    
    int blobProcessingMode = 0;
    int currentPaletteIndex = 0;
  private:
    int stripCount = 48;
    int stripLength = 48;
    int numLEDs = stripCount * stripLength;
    boolean isInitialized = false;
                
    byte redPalette[128];
    byte greenPalette[128];
    byte bluePalette[128];

    void pushPixel(uint8_t Red, uint8_t Green, uint8_t Blue);
    void pushPixelFull(uint8_t Red, uint8_t Green, uint8_t Blue);
    void pushWhite();
    void pushBlack();
    void setAll(uint8_t Red, uint8_t Green, uint8_t Blue);
};

#endif
