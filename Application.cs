using HumidityControl.LcdSerialBackpack;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System;
using System.IO.Ports;
using System.Threading;

namespace HumidityControl
{
  public sealed class Application : IDisposable
  {
    // Half screen
    private const int half = 11;

    // Devices:
    private readonly AnalogInput trimmer = new AnalogInput(AnalogChannels.ANALOG_PIN_A0);

    private readonly Fan fan = new Fan(PWMChannels.PWM_PIN_D9, Pins.GPIO_PIN_D8);

    private readonly SerialPort screenPort = new SerialPort(SerialPorts.COM2, (int)BaudRates.Baud115200, Parity.None, 8, StopBits.One);
    private readonly LcdScreen screen;

    private readonly I2CDevice i2c = new I2CDevice(new I2CDevice.Configuration(Bme680.Bme680.SecondaryI2CAddress, 100 /* kHz */));
    private readonly Bme680.Bme680 bme680;

    // User interface:
    private readonly Label temperatureLabel, humidityLabel, pressureLabel, fanPowerLabel, fanSpeedLabel;

    public Application()
    {
      screen = new LcdScreen(screenPort);
      bme680 = new Bme680.Bme680(i2c);

      temperatureLabel = new Label(screen, LcdScreen.LastColumn - 1, 6, Label.TextAlign.Right);
      humidityLabel = new Label(screen, LcdScreen.LastColumn - 1, 4, Label.TextAlign.Right);
      pressureLabel = new Label(screen, LcdScreen.LastColumn - 3, 2, Label.TextAlign.Right);
      fanPowerLabel = new Label(screen, LcdScreen.LastColumn - 1, 1, Label.TextAlign.Right);
      fanSpeedLabel = new Label(screen, LcdScreen.LastColumn - 3, 0, Label.TextAlign.Right);
    }

    public void Run()
    {
      // Screen setup
      screenPort.Open();

      // Fan setup
      fan.Start();

      // BME680 setup
      bme680.HumidityOversampling = Bme680.Oversampling.x2;
      bme680.TemperatureOversampling = Bme680.Oversampling.x8;
      bme680.PressureOversampling = Bme680.Oversampling.x4;
      bme680.IirFilterSize = Bme680.IirFilterSize.C3;

      // Draw screen:
      screen.SetBacklightDutyCycle(100);
      screen.ClearScreen();

      screen.TextAt(half, 7).Draw("Temp.");
      screen.TextAt(LcdScreen.LastColumn, 6).Draw("C");
      screen.TextAt(half, 5).Draw("Rel. hum.");
      screen.TextAt(LcdScreen.LastColumn, 4).Draw("%");
      screen.TextAt(half, 3).Draw("Pressure");
      screen.TextAt(LcdScreen.LastColumn - 2, 2).Draw("hPa");
      screen.TextAt(half, 1).Draw("Fan:");
      screen.TextAt(LcdScreen.LastColumn, 1).Draw("%");
      screen.TextAt(LcdScreen.LastColumn - 2, 0).Draw("RPM");

      // Await screen start up
      Thread.Sleep(500);

      // Read sensor and print data to screen
      while (true)
      {
        try
        {
          loop();
        }
        catch (Exception e)
        {
          Debug.Print("Exception caught: " + e);

          Thread.Sleep(1000);
        }
      }
    }

    private void loop()
    {
      bme680.PowerMode = Bme680.PowerMode.Forced;
      Thread.Sleep(Program.Clamp(bme680.MeasurementDuration.Milliseconds, 400, 1000));

      if (bme680.HasNewData)
      {
        Debug.Print("Righteous update.");

        temperatureLabel.Text = bme680.Temperature.ToString("F3");
        humidityLabel.Text = bme680.Humidity.ToString("F3");
        pressureLabel.Text = (bme680.Pressure / 100.0).ToString("F1");
      }
      else
      {
        Debug.Print("Sterile update.");
      }

      double fanControl = FanControl;

      // Sets fan speed through PWM
      fan.SpeedControl = fanControl;

      fanPowerLabel.Text = (fanControl * 100.0).ToString("F0");
      fanSpeedLabel.Text = fan.Speed.ToString("F1");

      temperatureLabel.Draw();
      humidityLabel.Draw();
      pressureLabel.Draw();
      temperatureLabel.Draw();
      fanPowerLabel.Draw();
      fanSpeedLabel.Draw();
    }

    /// <summary>
    /// Reads the fan speed indicated by the user through a trimmer. Values range between zero and one.
    /// </summary>
    private double FanControl
    {
      get
      {
        double fanControl = trimmer.Read();

        return double.IsNaN(fanControl) ? 0.0 : Program.Clamp(fanControl, 0.0, 1.0);
      }
    }

    public void Dispose()
    {
      trimmer.Dispose();
      fan.Dispose();
      screenPort.Dispose();
      i2c.Dispose();
    }
  }
}