# LEDMatrix
## Universal Windows Platform
The "big brain" component of this project can run on any device that supports the Universal Windows Platform...  It has been tested on Windows Phone 10 and Windows 10 Desktop.
## Arduino
The "little brain" component of this project runs on an Arduino, and needs to have a sketch loaded to support the kind of LED hardware you're running.  Included implementations are:
- LedCurtainFirmata - This is the configuration that was shown at //build 2015, and at Maker Faire San Mateo and Maker Faire Shenzhen.  The LED "Curtain" is made up of strips of LPD8806-based RGB LEDs, cut into 48 strips of 48 "pixels" each (2304 total pixels).  They are connected into one long serial string, with pixel "zero" starting in the bottom left hand corner of the screen.
- LedMatrixPanelFirmata - This is the configuration used in the http://hackster.io post.  The LEDs are a 32x32 panel, which gets just gets driven using Adafruit's libraries.