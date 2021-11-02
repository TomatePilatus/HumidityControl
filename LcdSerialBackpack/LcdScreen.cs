using Microsoft.SPOT;
using System;
using System.IO;
using System.Text;

namespace HumidityControl.LcdSerialBackpack
{
  public sealed class LcdScreen
  {
    private const byte StartingCommand = 0x7C;

    /// <summary>
    /// Gets the screen width in pixel.
    /// </summary>
    public const int ScreenWidth = 128;

    /// <summary>
    /// Gets the screen height in pixel.
    /// </summary>
    public const int ScreenHeight = 64;

    /// <summary>
    /// Gets the height in pixel of a char.
    /// </summary>
    public const int CharHeight = 8;

    /// <summary>
    /// Gets the width in pixel of a char.
    /// </summary>
    public const int CharWidth = 6;

    /// <summary>
    /// Index of last column.
    /// </summary>
    public const int LastColumn = 20;

    /// <summary>
    /// Index of last row.
    /// </summary>
    public const int LastRow = 7;

    private readonly Stream serial;

    private readonly byte[] writeBuffer = new byte[3];

    /// <summary>
    /// Creates a new instance of <see cref="LcdScreen"/> class.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// serial is null.
    /// </exception>
    public LcdScreen(Stream serial)
    {
      if (serial == null)
      {
        throw new ArgumentNullException("serial");
      }

      this.serial = serial;
    }

    private void write(byte b0)
    {
      Debug.Assert(writeBuffer.Length >= 1, "Wrong buffer size.");

      writeBuffer[0] = b0;

      serial.Write(writeBuffer, 0, 1);
    }

    private void write(byte b0, byte b1)
    {
      Debug.Assert(writeBuffer.Length >= 2, "Wrong buffer size.");

      writeBuffer[0] = b0;
      writeBuffer[1] = b1;

      serial.Write(writeBuffer, 0, 2);
    }

    private void write(byte b0, byte b1, byte b2)
    {
      Debug.Assert(writeBuffer.Length >= 3, "Wrong buffer size.");

      writeBuffer[0] = b0;
      writeBuffer[1] = b1;
      writeBuffer[2] = b2;

      serial.Write(writeBuffer, 0, 3);
    }

    private void write(params byte[] bytes)
    {
      serial.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Clears the screen of all written pixels.
    /// If you're operating in normal mode, all pixels are reset.
    /// If you're operating in reverse mode, all pixels are set (white background).
    /// </summary>
    public LcdScreen ClearScreen()
    {
      write(StartingCommand, 0x00);

      return this;
    }

    /// <summary>
    /// Runs demonstration code. This is in the firmware just as an
    /// example of what the display can do.
    /// </summary>
    public LcdScreen RunDemo()
    {
      write(StartingCommand, 0x04);

      return this;
    }

    /// <summary>
    /// Toggles between white on blue display and blue on white display.
    /// Setting the reverse mode causes the screen to immediately clear with the new background. 
    /// </summary>
    public LcdScreen ToggleReverseMode()
    {
      write(StartingCommand, 0x12);

      return this;
    }

    /// <summary>
    /// Allows or disallows the SparkFun logo to be displayed at power up. 
    /// The splash screen serves two purposes.
    /// One is obviously to put our mark on the product, but the second is to allow
    /// a short time at power up where the display can
    /// be recovered from errant baud rate changes (see
    /// Baud Rate for more info). Disabling the splash screen
    /// suppresses the logo, but the delay remains active.
    /// </summary>
    public LcdScreen ToggleSplashScreen()
    {
      write(StartingCommand, 0x13);

      return this;
    }

    /// <summary>
    /// Sets the backlight duty cycle.
    /// Setting the value to zero turns the back light off,
    /// setting it at 100 or above turns it full on,
    /// and intermediate values set it somewhere inbetween.
    /// </summary>
    /// <param name="intensity">A number from 0 to 100.</param>
    public LcdScreen SetBacklightDutyCycle(int intensity)
    {
      if (intensity < 0 || 100 < intensity)
      {
        throw new ArgumentOutOfRangeException("intensity", "Intensity must be between 0 and 100.");
      }

      write(StartingCommand, 0x02, (byte)intensity);

      return this;
    }

    /// <summary>
    /// Sets the current baud rate.
    /// </summary>
    /// <remarks>
    /// The default baud rate is 115,200bps, but the backpack can be set to a
    /// variety of communication speeds.
    /// </remarks>
    /// <param name="baudRate">The new baud rate.</param>
    public LcdScreen SetBaudRate(ScreenBaudRates baudRate)
    {
      write(StartingCommand, 0x07, (byte)baudRate);

      return this;
    }

    /// <summary>
    /// Sets the specified pixel.
    /// </summary>
    /// <param name="x">The x coordinate, between 0 and 160.</param>
    /// <param name="y">The y coordinate, between 0 and 128.</param>
    /// <param name="draw">Whether to set or reset it; viceversa in reverse mode.</param>
    public LcdScreen SetPixel(int x, int y, bool draw = true)
    {
      if (x < 0 || ScreenWidth < x)
      {
        throw new ArgumentOutOfRangeException("x", "The X coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (y < 0 || ScreenHeight < y)
      {
        throw new ArgumentOutOfRangeException("y", "The Y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      write(StartingCommand, 0x10, (byte)x, (byte)y, (byte)(draw ? 1 : 0));

      return this;
    }

    /// <summary>
    /// Draws a line.
    /// </summary>
    /// <param name="Set">Whether to draw or erase it; viceversa in reverse mode.</param>
    public LcdScreen DrawLine(int x1, int y1, int x2, int y2, bool draw = true)
    {
      if (x1 < 0 || ScreenWidth < x1)
      {
        throw new ArgumentOutOfRangeException("x1", "The x coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (y1 < 0 || ScreenHeight < y1)
      {
        throw new ArgumentOutOfRangeException("y1", "The Y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      if (x2 < 0 || ScreenWidth < x2)
      {
        throw new ArgumentOutOfRangeException("x2", "The x coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (y2 < 0 || ScreenHeight < y2)
      {
        throw new ArgumentOutOfRangeException("y2", "The Y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      write(StartingCommand, 0x0C, (byte)x1, (byte)y1, (byte)x2, (byte)y2, (byte)(draw ? 1 : 0));

      return this;
    }

    /// <summary>
    /// Draws a circle.
    /// </summary>
    /// <param name="Set">Whether to draw or erase it; viceversa in reverse mode.</param>
    public LcdScreen DrawCircle(int centreX, int centreY, int radius, bool draw = true)
    {
      if (centreX < 0 || ScreenWidth < centreX)
      {
        throw new ArgumentOutOfRangeException("centreX", "The X coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (centreY < 0 || ScreenHeight < centreY)
      {
        throw new ArgumentOutOfRangeException("centreY", "The Y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      if (radius < 0)
      {
        throw new ArgumentException("radius", "The radius of the circle cannot be negative.");
      }

      write(StartingCommand, 0x03, (byte)centreX, (byte)centreY, (byte)radius, (byte)(draw ? 1 : 0));

      return this;
    }

    /// <summary>
    /// Draws a box.
    /// </summary>
    /// <param name="draw">Whether to draw or erase it; viceversa in reverse mode.</param>
    public LcdScreen DrawBox(int x1, int y1, int x2, int y2, bool draw = true)
    {
      if (x1 < 0 || ScreenWidth < x1)
      {
        throw new ArgumentOutOfRangeException("x1", "The x coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (y1 < 0 || ScreenHeight < y1)
      {
        throw new ArgumentOutOfRangeException("y1", "The Y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      if (x2 < 0 || ScreenWidth < x2)
      {
        throw new ArgumentOutOfRangeException("x2", "The x coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (y2 < 0 || ScreenHeight < y2)
      {
        throw new ArgumentOutOfRangeException("y2", "The Y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      write(StartingCommand, 0x0F, (byte)x1, (byte)y1, (byte)x2, (byte)y2, (byte)(draw ? 0 : 1));

      return this;
    }

    /// <summary>
    /// Erases the specified box.
    /// This is just like the draw box
    /// command, except the contents of the box are erased
    /// to the background color.
    /// </summary>
    public LcdScreen EraseBox(int x1, int y1, int x2, int y2)
    {
      if (x1 < 0 || ScreenWidth < x1)
      {
        throw new ArgumentOutOfRangeException("x1", "The x coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (y1 < 0 || ScreenHeight < y1)
      {
        throw new ArgumentOutOfRangeException("y1", "The y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      if (x2 < 0 || ScreenWidth < x2)
      {
        throw new ArgumentOutOfRangeException("x2", "The x coordinate must be between 0 and " + LcdScreen.ScreenWidth);
      }

      if (y2 < 0 || ScreenHeight < y2)
      {
        throw new ArgumentOutOfRangeException("y2", "The y coordinate must be between 0 and " + LcdScreen.ScreenHeight);
      }

      write(StartingCommand, 0x05, (byte)x1, (byte)y1, (byte)x2, (byte)y2);

      return this;
    }

    public LcdScreen TextAt(int column, int row)
    {
      return TextAtPixel(TextX(column), TextY(row));
    }

    public LcdScreen TextAtPixel(int x, int y)
    {
      /*
       * The x and y reference coordinates (x_offset and y_offset in the
       * source code) are used by the text generator to place text at
       * specific locations on the screen. The coordinates refer to the
       * upper left most pixel in the character space.
       */

      // Sets the x coordinate
      write(StartingCommand, 0x18, (byte)x);

      // Sets the y coordinate
      write(StartingCommand, 0x19, (byte)y);

      return this;
    }

    public LcdScreen Draw(char c)
    {
      Debug.Assert(c != '|', "Text cannot contain the '|' (0x7C) character.");

      write((byte)c);

      return this;
    }

    public LcdScreen Draw(string text)
    {
      Debug.Assert(text.IndexOf('|') < 0, "Text cannot contain the '|' (0x7C) character.");

      var bytes = Encoding.UTF8.GetBytes(text);
      serial.Write(bytes, 0, bytes.Length);

      return this;
    }

    /// <summary>
    /// Represents the possible baud rates for the LCD screen.
    /// </summary>
    public enum ScreenBaudRates : byte
    {
      BaudRate4800 = (byte)'1',
      BaudRate9600 = (byte)'2',
      BaudRate19200 = (byte)'3',
      BaudRate38400 = (byte)'4',
      BaudRate57600 = (byte)'5',
      BaudRate115200 = (byte)'6'
    }

    public static int TextX(int column)
    {
      if (column < 0 || LastColumn < column)
      {
        throw new ArgumentException("column", "The column must be between 0 and " + LastColumn + ".");
      }

      return column * CharWidth;
    }

    public static int TextY(int row)
    {
      if (row < 0 || LastRow < row)
      {
        throw new ArgumentException("row", "The row must be between 0 and " + LastRow + ".");
      }

      return (row + 1) * CharHeight - 1;
    }
  }
}