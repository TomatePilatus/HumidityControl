using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using System;

namespace HumidityControl
{
  public sealed class Fan : IDisposable
  {
    private const int BinLifetimeMilliseconds = 250;

    private readonly PWM speed;
    private readonly InterruptPort tachometer;

    private int firstCounter = 0, secondCounter = 0;
    private DateTime firstCounterStart = DateTime.MinValue, secondCounterStart = DateTime.MinValue;

    public Fan(Cpu.PWMChannel speed, Cpu.Pin tachometer)
    {
      this.speed = new PWM(speed, 25e3 /* Hz */, 0.0 /* % */, false);
      this.tachometer = new InterruptPort(tachometer, false, ResistorModes.PullUp, InterruptModes.InterruptEdgeHigh); ;

      this.tachometer.OnInterrupt += tachometer_OnInterrupt;
    }

    public void Start()
    {
      firstCounter = 0;
      firstCounterStart = DateTime.Now;

      speed.Start();
      tachometer.EnableInterrupt();
    }

    private void tachometer_OnInterrupt(uint pin, uint state, DateTime time)
    {
      lock (this)
      {
        Debug.Assert(firstCounterStart >= secondCounterStart);

        if (time >= firstCounterStart) // time after firstCounterStart
        {
          firstCounter++;
        }
        else if (time >= secondCounterStart) // time after secondCounterStart
        {
          secondCounter++;
        } // else event has been lost due to the CPU being too busy

        UpdateBins();
      }
    }

    private void UpdateBins()
    {
      var difference = (DateTime.Now.Ticks - firstCounterStart.Ticks) / TimeSpan.TicksPerMillisecond;

      if (difference > BinLifetimeMilliseconds)
      {
        secondCounter = firstCounter;
        secondCounterStart = firstCounterStart;

        firstCounter = 0;
        firstCounterStart = DateTime.Now;

        // Debug.Print("Update: first=" + firstCounter + " " + firstCounterStart + ", second=" + secondCounter + " " + secondCounterStart);
      }
    }

    public float Speed
    {
      get
      {
        lock (this)
        {
          float result;
          if (secondCounterStart != DateTime.MinValue)
          {
            result = RotationPerMinute(firstCounter + secondCounter, secondCounterStart);
          }
          else
          {
            Debug.Assert(firstCounterStart != DateTime.MinValue);

            result = RotationPerMinute(firstCounter, firstCounterStart);
          }

          UpdateBins();

          return result;
        }
      }
    }

    private static float RotationPerMinute(int counts, DateTime counterStart)
    {
      if (counts <= 0)
      {
        return 0.0f;
      }

      long ticks = DateTime.Now.Ticks - counterStart.Ticks;

      if (ticks <= 0)
      {
        return 0.0f;
      }
      else
      {
        return counts / 2.0f / ticks * TimeSpan.TicksPerMinute;
      }
    }

    /// <summary>
    /// Fan speed between zero and one.
    /// </summary>
    public double SpeedControl
    {
      get { return speed.DutyCycle; }
      set
      {
        if (double.IsNaN(value) || value < 0.0 || 100.0 < value)
        {
          throw new ArgumentOutOfRangeException("value");
        }

        speed.DutyCycle = value;
      }
    }

    public void Dispose()
    {
      speed.Dispose();
      tachometer.Dispose();
    }
  }
}