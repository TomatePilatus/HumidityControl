using Microsoft.SPOT;
using System;
using System.Globalization;

namespace HumidityControl
{
  public sealed class Program
  {
    // static OutputPort Led = new OutputPort(Pins.ONBOARD_LED, false);
    public static void Main()
    {
      ResourceUtility.SetCurrentUICulture(new CultureInfo("en-US"));

      Application app = new Application();

      app.Run();
    }

    internal static double Clamp(double value, double min, double max)
    {
      if (double.IsNaN(value))
      {
        throw new ArgumentException("NaN", "value");
      }

      return value < min ? min : (value > max ? max : value);
    }

    internal static int Clamp(int value, int min, int max)
    {
      return value < min ? min : (value > max ? max : value);
    }
  }
}