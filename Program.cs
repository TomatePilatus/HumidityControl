using HumidityControl.LcdSerialBackpack;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace HumidityControl
{
  public class Program
  {
    // static OutputPort Led = new OutputPort(Pins.ONBOARD_LED, false);
    public static void Main()
    {
      Application app = new Application();

      app.Run();
    }
  }
}