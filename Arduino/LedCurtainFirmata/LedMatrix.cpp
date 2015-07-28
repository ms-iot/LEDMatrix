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

#include "LedMatrix.h"

// Initializes the SPI bus used to communicate with the LED Matrix 
void LedMatrix::begin()
{
  if (!isInitialized)
  {
    SPI.begin();
    SPI.setBitOrder(MSBFIRST);
    SPI.setDataMode(SPI_MODE0);
    SPI.setClockDivider(SPI_CLOCK_DIV8);
    isInitialized = true;
  }
}

// Sends a specific number of zeros, which resets the addressing of the LEDs.  Need one zero sent per every 32 LEDs.
void LedMatrix::reset()
{
  for (int i = ((numLEDs + 31) / 32); i > 0; i--)
  {
    SPI.transfer(0);
  }
}

// Pushes an RGB value out to the current pixel.  Value must be already adjusted
// to a 128-255 range.
void LedMatrix::pushPixel(uint8_t red, uint8_t green, uint8_t blue)
{
  SPI.transfer(green);
  SPI.transfer(red);
  SPI.transfer(blue);
}

// Pushes an RGB value out to the current pixel.  Accepts values ranging from
// 0-255, and compresses to 128-255 as expected by the LED.
void LedMatrix::pushPixelFull(uint8_t red, uint8_t green, uint8_t blue)
{
  SPI.transfer((green >> 1) | 0x80);
  SPI.transfer((red >> 1) | 0x80);
  SPI.transfer((blue >> 1) | 0x80);
}

// Pushes "white" to the current pixel ("perceived" intensity is roughly the
// same as an individual color full on.
void LedMatrix::pushWhite()
{
  pushPixel(169,169,169);
}

// Pushes black to the current pixel
void LedMatrix::pushBlack()
{
  // 128 == 0 | 0x80
  pushPixel(128,128,128);
}

// Pushes black to all pixels in the LED Matrix
void LedMatrix::clear()
{
  reset();

  for (int stripNum = 0; stripNum < stripCount; stripNum++)
  {
    for (int pixelNum = 0; pixelNum < stripLength; pixelNum++)
    {
      pushPixel(128, 128, 128);
    }
  }
  
  reset();
}

// Function used to process pixels handed over as a blob of 7-bit bytes (first bit is reserved for the
// SYSEX protocol).
void LedMatrix::processPixelBlob(byte argc, byte *argv)
{
  // Must have at least 3 bytes passed in to be a valid 21bit pixel
  if (argc >= 3)
  {
    int pixelCount = argc/3;
    
    for (int i = 0; i < pixelCount; i++)
    {
      int startPos = i*3;
      pushPixel(argv[startPos] | 0x80, argv[startPos + 1] | 0x80, argv[startPos + 2] | 0x80);
    }
  }
}

