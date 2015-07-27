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

LedMatrix::LedMatrix(void) 
{
}

// Initializes the SPI bus used to communicate with the LED Matrix 
void LedMatrix::begin()
{
  matrix->begin();
}

// Pushes an RGB value out to the current pixel.  Value must be already adjusted
// to a 128-255 range.
void LedMatrix::pushPixel(uint8_t red, uint8_t green, uint8_t blue)
{
  matrix->drawPixel(currentX, currentY, matrix->Color888(red, green, blue, true));
  
  currentX = currentX + 1;
  if (currentX >= xMax)
  {
    currentX = 0;
    currentY = currentY + 1;
    if (currentY >= yMax)
    {
      currentY = 0;
    }
  }
}

// Pushes black to all pixels in the LED Matrix
void LedMatrix::clear()
{
  for (int yc = 0; yc < yMax; yc++)
  {
    for (int xc = 0; xc < xMax; xc++)
    {
      pushPixel(255, 0, 0);
    }
  }
}

// Function used to process pixels handed over as a blob of 7-bit bytes (first bit is reserved for the
// SYSEX protocol).
void LedMatrix::processPixelBlob(uint8_t argc, uint8_t *argv)
{
  if (argc >= 3)
  {
    int pixelCount = argc/3;
    
    for (int i = 0; i < pixelCount; i++)
    {
      int startPos = i*3;
      pushPixel(argv[startPos], argv[startPos + 1], argv[startPos + 2]);
    }
  }
}

