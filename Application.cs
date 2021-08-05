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
  public class Application : IDisposable
  {
    // Devices:
    private readonly InterruptPort upButton = new InterruptPort(Pins.GPIO_PIN_A1, true, ResistorModes.PullUp, InterruptModes.InterruptEdgeHigh),
      downButton = new InterruptPort(Pins.GPIO_PIN_D7, true, ResistorModes.PullUp, InterruptModes.InterruptEdgeHigh),
      rightButton = new InterruptPort(Pins.GPIO_PIN_D6, true, ResistorModes.PullUp, InterruptModes.InterruptEdgeHigh),
      leftButton = new InterruptPort(Pins.GPIO_PIN_D8, true, ResistorModes.PullUp, InterruptModes.InterruptEdgeHigh);

    private readonly PWM fan = new PWM(PWMChannels.PWM_PIN_D5, 25e3 /* Hz */, 0.0 /* % */, false);
    private readonly SerialPort screenPort = new SerialPort(SerialPorts.COM2, (int)BaudRate.Baudrate115200, Parity.None, 8, StopBits.One);
    private readonly LcdScreen screen;

    private readonly I2CDevice i2c = new I2CDevice(new I2CDevice.Configuration(Bme680.Bme680.SecondaryI2CAddress, 100 /* kHz */));
    private readonly Bme680.Bme680 bme680;

    // Persisted data
    private static readonly ExtendedWeakReference bootInfoReference = ExtendedWeakReference.RecoverOrCreate(typeof(ApplicationBootInformation), 0,
      ExtendedWeakReference.c_SurvivePowerdown | ExtendedWeakReference.c_SurviveBoot);

    static Application()
    {
      bootInfoReference.Priority = (int)ExtendedWeakReference.PriorityLevel.Important;

      if (bootInfoReference.Target == null)
      {
        Debug.Print("Empty boot info. Reference.IsAlive=" + bootInfoReference.IsAlive);

        bootInfoReference.Target = new ApplicationBootInformation(0);
        bootInfoReference.PushBackIntoRecoverList();
      }
      else
      {
        Debug.Print("Loaded boot info. Reference.IsAlive=" + bootInfoReference.IsAlive);
      }
    }

    // State:
    private FanMode fanState = FanMode.Off;
    private int userFanSpeed = -1;

    private string screenTemperature, screenHumidity, screenPressure;
    private FanMode screenFanState = FanMode.Off;
    private int screenFanSpeed = -1;

    public Application()
    {
      screenPort.Open();
      screen = new LcdScreen(screenPort);
      bme680 = new Bme680.Bme680(i2c);

      upButton.OnInterrupt += UpButton_OnClick;
      downButton.OnInterrupt += DownButton_OnClick;
      leftButton.OnInterrupt += LeftButton_OnClick;
      rightButton.OnInterrupt += RightButton_OnClick;
    }

    public void Run()
    {
      // Initial fan speed:
      UserFanSpeed = ((ApplicationBootInformation)bootInfoReference.Target).FanSpeed;

      // BME680 setup
      bme680.HumidityOversampling = Bme680.Oversampling.x2;
      bme680.TemperatureOversampling = Bme680.Oversampling.x8;
      bme680.PressureOversampling = Bme680.Oversampling.x4;
      bme680.IirFilterSize = Bme680.IirFilterSize.C3;

      // Draw screen:
      screen.SetBacklightDutyCycle(100);
      screen.ClearScreen();

      screen.TextAt(0, 7).Draw("Temperature:");
      screen.TextAt(18, 6).Draw("C");
      screen.TextAt(0, 5).Draw("Relative humidity:");
      screen.TextAt(18, 4).Draw("%");
      screen.TextAt(0, 3).Draw("Pressure:");
      screen.TextAt(18, 2).Draw("hPa");
      screen.TextAt(0, 1).Draw("Fan control:");

      DrawFanSpeedControl();

      upButton.EnableInterrupt();
      downButton.EnableInterrupt();

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
      Thread.Sleep(System.Math.Max(bme680.MeasurementDuration.Milliseconds, 700));

      if (!bme680.HasNewData)
      {
        Debug.Print("Sterile update.");
        return;
      }
      else
      {
        Debug.Print("Righteous update.");
      }

      const int right = 17;

      string temperature = bme680.Temperature.ToString("F2"), humidity = bme680.Humidity.ToString("F2"), pressure = (bme680.Pressure / 100.0).ToString("F2");

      if (!Object.Equals(temperature, screenTemperature))
      {
        screen.TextAt(right - temperature.Length, 6).Draw(temperature);
        screenTemperature = temperature;
      }

      if (!Object.Equals(humidity, screenHumidity))
      {
        screen.TextAt(right - humidity.Length, 4).Draw(humidity);
        screenHumidity = humidity;
      }

      if (!Object.Equals(pressure, screenPressure))
      {
        screen.TextAt(right - pressure.Length, 2).Draw(pressure);
        screenPressure = pressure;
      }

      DrawFanSpeedControl();
    }

    private void DrawFanSpeedControl()
    {
      if (screenFanSpeed == UserFanSpeed && screenFanState == fanState)
      {
        return;
      }

      screen.TextAt(2, 0).Draw(" High  ").Draw(fillBefore(UserFanSpeed.ToString(), 3)).Draw("%  Off ");

      switch (fanState)
      {
        case FanMode.High:
          screen.DrawBox(LcdScreen.TextX(2), 0, LcdScreen.TextX(8), LcdScreen.TextY(1));

          break;

        case FanMode.Custom:
          screen.DrawBox(LcdScreen.TextX(8), 0, LcdScreen.TextX(14), LcdScreen.TextY(1));

          break;

        case FanMode.Off:
          screen.DrawBox(LcdScreen.TextX(14), 0, LcdScreen.TextX(19), LcdScreen.TextY(1));

          break;
      }

      screenFanSpeed = UserFanSpeed;
      screenFanState = fanState;
    }

    #region Fan control
    private int UserFanSpeed
    {
      get { return userFanSpeed; }
      set
      {
        var clamped = value > 100 ? 100 : (value < 0 ? 0 : value);

        if (clamped != userFanSpeed)
        {
          userFanSpeed = clamped;
          FanSpeed = clamped;

          bootInfoReference.Target = new ApplicationBootInformation(clamped);
          bootInfoReference.PushBackIntoRecoverList();
        }
      }
    }

    private const double MaxDutyCycle = 0.4;
    private int FanSpeed
    {
      get
      {
        int speed = 100 - (int)System.Math.Round(fan.DutyCycle / MaxDutyCycle * 100.0);

        Debug.Assert(0 <= speed && speed <= 100);

        return speed;
      }
      set
      {
        if (value <= 0)
        {
          fan.DutyCycle = 1.0;
        }
        else
        {
          fan.DutyCycle = (100 - value) / 100.0 * MaxDutyCycle;
        }

        Debug.Print("Fan speed=" + value + ", fan duty cycle=" + fan.DutyCycle);
      }
    }

    private FanMode FanState
    {
      get { return fanState; }
      set
      {
        fanState = value;

        switch (value)
        {
          case FanMode.High:
            FanSpeed = 100;

            break;

          case FanMode.Custom:
            FanSpeed = UserFanSpeed;

            break;

          case FanMode.Off:
            FanSpeed = 0;

            break;

          default:
            throw new ArgumentException("value");
        }
      }
    }
    #endregion

    #region Buttons
    private void UpButton_OnClick(uint pin, uint value, DateTime time)
    {
      Debug.Print("UpButton pin=" + pin + ", value=" + value);

      if (fanState != FanMode.Custom || UserFanSpeed >= 100)
      {
        return;
      }

      UserFanSpeed += 5;

      Debug.Print("Fan speed=" + FanSpeed + ", user fan speed=" + UserFanSpeed + ", DutyCycle=" + fan.DutyCycle);
    }

    private void DownButton_OnClick(uint pin, uint value, DateTime time)
    {
      Debug.Print("DownButton pin=" + pin + ", value=" + value);

      if (fanState != FanMode.Custom || UserFanSpeed <= 0)
      {
        return;
      }

      UserFanSpeed -= 5;

      Debug.Print("Fan speed=" + FanSpeed + ", user fan speed=" + UserFanSpeed + ", DutyCycle=" + fan.DutyCycle);
    }

    private void LeftButton_OnClick(uint pin, uint value, DateTime time)
    {
      Debug.Print("LeftButton pin=" + pin + ", value=" + value);

      switch (FanState)
      {
        case FanMode.Off:
          FanState = FanMode.Custom;

          break;

        case FanMode.Custom:
          FanState = FanMode.High;

          break;

        case FanMode.High:
          FanState = FanMode.Off;

          break;

        default:
          throw new InvalidOperationException("Invalid fan state.");
      }
    }

    private void RightButton_OnClick(uint pin, uint value, DateTime time)
    {
      Debug.Print("RightButton pin=" + pin + ", value=" + value);

      switch (FanState)
      {
        case FanMode.High:
          FanState = FanMode.Custom;

          break;

        case FanMode.Custom:
          FanState = FanMode.Off;

          break;

        case FanMode.Off:
          FanState = FanMode.High;

          break;

        default:
          throw new InvalidOperationException("Invalid fan state.");
      }
    }
    #endregion

    public void Dispose()
    {
      upButton.Dispose();
      downButton.Dispose();
      rightButton.Dispose();
      leftButton.Dispose();

      fan.Dispose();
      screenPort.Dispose();
    }

    private static string fillBefore(string body, int length, char symbol = ' ')
    {
      Debug.Assert(length >= 0, "Negative length.");

      if (body.Length >= length)
      {
        return body;
      }

      StringBuilder builder = new StringBuilder(length);

      for (int i = body.Length; i < length; i++)
      {
        builder.Append(symbol);
      }

      builder.Append(body);

      return builder.ToString();
    }
  }
}