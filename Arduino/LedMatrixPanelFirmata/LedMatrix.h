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

#include <Adafruit_GFX.h>   // Core graphics library

#ifndef RGBmatrixPanel_h
#define RGBmatrixPanel_h
#include <RGBmatrixPanel.h> // Hardware-specific library

#endif
//#include <avr/pgmspace.h> 

// Custom SYSEX command for high-speed data transfer.  
#define SYSEX_BLOB_COMMAND 0x7C

// Custom SYSEX commands related to the LED Matrix
#define LED_RESET 0x43
#define LED_CONFIG 0x44
#define LED_PIXEL21 0x42 // 21bit color (7bits each for R,G,B)
#define LED_PIXEL7 0x45 // 7bit indexed color
#define LED_PIXEL7_PALETTE 0x47 // 7bit indexed color palette
#define LED_PIXEL1 0x46 // 1bit color (7 pixels per byte)

// If your 32x32 matrix has the SINGLE HEADER input,
// use this pinout:
#define CLK 11  // MUST be on PORTB! (Use pin 11 on Mega)
#define OE  9
#define LAT 10
#define A   A0
#define B   A1
#define C   A2
#define D   A3
// If your matrix has the DOUBLE HEADER input, use:
//#define CLK 8  // MUST be on PORTB! (Use pin 11 on Mega)
//#define LAT 9
//#define OE  10
//#define A   A3
//#define B   A2
//#define C   A1
//#define D   A0

class LedMatrix {
  public:
    LedMatrix(RGBmatrixPanel matrixPanel);
    void begin();
    void reset();
    void clear();
    void processPixelBlob(byte argc, byte *argv);
    
    int blobProcessingMode = 0;
    int currentPaletteIndex = 0;
  private:
    RGBmatrixPanel *matrix;
    int x = 32;
    int y = 32;
    int currentX = 0;
    int currentY = 0;
//    int numLEDs = x * y;
    boolean isInitialized = false;
                
//    byte red[x][y];
//    byte green[x][y];
//    byte blue[x][y];

    void pushPixel(uint8_t Red, uint8_t Green, uint8_t Blue);
//    void pushPixelFull(uint8_t Red, uint8_t Green, uint8_t Blue);
//    void pushWhite();
//    void pushBlack();
//    void setAll(uint8_t Red, uint8_t Green, uint8_t Blue);
};

#endif
