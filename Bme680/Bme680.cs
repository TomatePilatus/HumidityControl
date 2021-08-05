using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System;
using System.IO;

namespace HumidityControl.Bme680
{
  /// <summary>
  /// Represents a BME680 gas, temperature, humidity and pressure sensor.
  /// </summary>
  public class Bme680
  {
    /// <summary>
    /// Default I2C bus address.
    /// </summary>
    public const byte DefaultI2CAddress = 0x76;

    /// <summary>
    /// Secondary I2C bus address.
    /// </summary>
    public const byte SecondaryI2CAddress = 0x77;

    /// <summary>
    /// Default timeout for I2C command executions in milliseconds.
    /// </summary>
    public const int DefaultTimeout = 100;

    /// <summary>
    /// The expected chip ID of the BME68x product family.
    /// </summary>
    public const byte ExpectedChipId = 0x61;

    /// <summary>
    /// Maximum number of attempts for reading a register value from the device.
    /// </summary>
    public const int MaxConnectionAttempts = 3;

    /// <summary>
    /// The communications channel to a device on an I2C bus.
    /// </summary>
    private readonly I2CDevice device;

    private readonly byte[] readBuffer = new byte[1], writeBuffer1 = new byte[1], writeBuffer2 = new byte[2];

    /// <summary>
    /// Calibration data specific to the device.
    /// </summary>
    public readonly CalibrationData CalibrationData;

    /// <summary>
    /// Initialize a new instance of the <see cref="Bme680"/> class.
    /// </summary>
    /// <param name="i2cDevice">The <see cref="I2cDevice"/> to create with.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="IOException"/>
    public Bme680(I2CDevice i2cDevice)
    {
      if (i2cDevice == null)
      {
        throw new ArgumentNullException("i2cDevice");
      }

      device = i2cDevice;

      // Ensure a valid device address has been set.
      int deviceAddress = i2cDevice.Config.Address;
      if (deviceAddress < DefaultI2CAddress || SecondaryI2CAddress < deviceAddress)
      {
        throw new ArgumentOutOfRangeException("i2cDevice", "Device address is out of range.");
      }

      // Ensure the device exists on the I2C bus.
      byte readChipId = ReadByte(Register.Id);
      if (readChipId != ExpectedChipId)
      {
        throw new IOException("Unable to find a chip with the right id.");
      }

      CalibrationData = new CalibrationData(this);
    }

    public byte ChipId
    {
      get { return ReadByte(Register.Id, 0, 7); }
    }

    public PowerMode PowerMode
    {
      get { return (PowerMode)ReadByte(Register.Ctrl_meas, 0, 2); }
      set { WriteByte(Register.Ctrl_meas, (byte)value, 0, 2); }
    }

    public TimeSpan MeasurementDuration
    {
      get
      {
        int cycles = CycleNumber(TemperatureOversampling) + CycleNumber(PressureOversampling) + CycleNumber(HumidityOversampling);
        long microseconds = cycles * 1963
          // TPH switching duration:
          + 477 * 4
          // Gas measurement duration:
          + 477 * 5;

        return TimeSpan.FromTicks(microseconds * 10);
      }
    }

    public bool RunGas
    {
      get { return ReadByte(Register.Ctrl_gas_1, 4, 1) == 1; }
      set { WriteByte(Register.Ctrl_gas_1, (byte)(value ? 1 : 0), 4, 1); }
    }

    /// <summary>
    /// Read a value indicating whether or not new sensor data is available.
    /// </summary>
    public bool HasNewData
    {
      get { return ReadByte(Register.meas_status_0, 7, 1) == 1; }
    }

    /// <summary>
    /// The temperature oversampling.
    /// </summary>
    public Oversampling TemperatureOversampling
    {
      get { return (Oversampling)ReadByte(Register.Ctrl_meas, 5, 3); }
      set { WriteByte(Register.Ctrl_meas, (byte)value, 5, 3); }
    }

    /// <summary>
    /// The humidity oversampling.
    /// </summary>
    public Oversampling HumidityOversampling
    {
      get { return (Oversampling)ReadByte(Register.Ctrl_hum, 0, 3); }
      set { WriteByte(Register.Ctrl_hum, (byte)value, 0, 3); }
    }

    /// <summary>
    /// The pressure oversampling.
    /// </summary>
    public Oversampling PressureOversampling
    {
      get { return (Oversampling)ReadByte(Register.Ctrl_meas, 2, 3); }
      set { WriteByte(Register.Ctrl_meas, (byte)value, 2, 3); }
    }

    public IirFilterSize IirFilterSize
    {
      get { return (IirFilterSize)ReadByte(Register.Config, 2, 3); }
      set { WriteByte(Register.Config, (byte)value, 2, 3); }
    }

    /// <summary>
    /// Gets the temperature in degree Celsius (℃).
    /// </summary>
    public double Temperature
    {
      get
      {
        // Read temperature data.
        byte lsb = ReadByte(Register.temp_lsb);
        byte msb = ReadByte(Register.temp_msb);
        byte xlsb = ReadByte(Register.temp_xlsb);

        // Convert to a 32bit integer.
        var adcTemperature = (msb << 12) + (lsb << 4) + (xlsb >> 4);

        // Calculate the temperature.
        var var1 = ((adcTemperature / 16384.0) - (CalibrationData.TCal1 / 1024.0)) * CalibrationData.TCal2;
        var var2 = pow2((adcTemperature / 131072.0) - (CalibrationData.TCal1 / 8192.0)) * CalibrationData.TCal3 * 16.0;

        return (var1 + var2) / 5120.0;
      }
    }

    /// <summary>
    /// Gets the humidity in %rH (percentage relative humidity).
    /// </summary>
    public double Humidity
    {
      get
      {
        // Read humidity data.
        byte msb = ReadByte(Register.hum_msb);
        byte lsb = ReadByte(Register.hum_lsb);

        // Convert to a 32bit integer.
        var adcHumidity = (msb << 8) + lsb;

        var temperature = Temperature;

        // Calculate the humidity.
        var var1 = adcHumidity - ((CalibrationData.HCal1 * 16.0) + ((CalibrationData.HCal3 / 2.0) * temperature));
        var var2 = var1 * ((CalibrationData.HCal2 / 262144.0) * (1.0 + ((CalibrationData.HCal4 / 16384.0) * temperature)
            + ((CalibrationData.HCal5 / 1048576.0) * temperature * temperature)));
        var var3 = CalibrationData.HCal6 / 16384.0;
        var var4 = CalibrationData.HCal7 / 2097152.0;
        var calculatedHumidity = var2 + ((var3 + (var4 * temperature)) * var2 * var2);

        return calculatedHumidity > 100.0 ? 100.0 : (calculatedHumidity < 0.0 ? 0.0 : calculatedHumidity);
      }
    }

    /// <summary>
    /// Get the pressure in pascal (Pa).
    /// </summary>
    public double Pressure
    {
      get
      {
        // Read pressure data.
        byte lsb = ReadByte(Register.pres_lsb); //0x20
        byte msb = ReadByte(Register.pres_msb); //0x1F
        byte xlsb = ReadByte(Register.pres_xlsb); //0x21

        // Convert to a 32bit integer.
        int adcPressure = (msb << 12) + (lsb << 4) + (xlsb >> 4);

        // Calculate the pressure.
        double var1 = (Temperature * 5120.0 / 2.0) - 64000.0;
        double var2 = var1 * var1 * (CalibrationData.PCal6 / 131072.0);
        var2 += (var1 * CalibrationData.PCal5 * 2.0);
        var2 = (var2 / 4.0) + (CalibrationData.PCal4 * 65536.0);
        var1 = ((CalibrationData.PCal3 * var1 * var1 / 16384.0) + (CalibrationData.PCal2 * var1)) / 524288.0;
        var1 = (1.0 + (var1 / 32768.0)) * CalibrationData.PCal1;

        // Avoid exception caused by division by zero.
        if (var1 == 0.0)
        {
          return 0.0;
        }
        else
        {
          var calculatedPressure = (1048576.0 - adcPressure - (var2 / 4096.0)) * 6250.0 / var1;
          var1 = CalibrationData.PCal9 * calculatedPressure * calculatedPressure / 2147483648.0;
          var2 = calculatedPressure * (CalibrationData.PCal8 / 32768.0);

          var var3 = pow3(calculatedPressure / 256.0) * (CalibrationData.PCal10 / 131072.0);

          return calculatedPressure + (var1 + var2 + var3 + (CalibrationData.PCal7 * 128.0)) / 16.0;
        }
      }
    }

    /// <summary>
    /// Compensated gas resistance output data in Ohm (Ω).
    /// </summary>
    public double GasResistance
    {
      get
      {
        // Read gas data.
        byte lsb = ReadByte(Register.gas_r_lsb),
          msb = ReadByte(Register.gas_r_msb, 6, 2),
          gas_range = ReadByte(Register.gas_r_msb, 0, 4);

        int gas_adc = (msb << 2) + lsb;

        var var1 = (1340.0 + 5.0 * CalibrationData.GCal1) * CalibrationData.GArray1[gas_range];

        return var1 * CalibrationData.GArray2[gas_range] / (gas_adc - 512.0 + var1);
      }
    }

    /// <summary>
    /// Reset the device.
    /// </summary>
    public void Reset()
    {
      Write((byte)Register.Reset, /* reset value*/ 0xB6);
    }

    #region I2C data writing and reading
    private void Write(byte byte1)
    {
      writeBuffer1[0] = byte1;

      Write(writeBuffer1);
    }

    private void Write(byte byte1, byte byte2)
    {
      writeBuffer2[0] = byte1;
      writeBuffer2[1] = byte2;

      Write(writeBuffer2);
    }

    private void Write(params byte[] message)
    {
      int attempt = 0;
      while (device.Execute(new I2CDevice.I2CTransaction[] { I2CDevice.CreateWriteTransaction(message) }, DefaultTimeout) < message.Length)
      {
        if (++attempt >= MaxConnectionAttempts)
        {
          throw new IOException("Can not reach the device.");
        }
      }
    }

    /// <summary>
    /// Read 8 bits from a given <see cref="Register"/>.
    /// </summary>
    /// <param name="register">The <see cref="Register"/> to read from.</param>
    /// <returns>Value from register.</returns>
    /// <remarks>
    /// Cast to an <see cref="sbyte"/> if you want to read a signed value.
    /// </remarks>
    public byte ReadByte(Register register)
    {
      writeBuffer1[0] = (byte)register;
      readBuffer[0] = 0;

      WriteRead(device, writeBuffer1, readBuffer);

      return readBuffer[0];
    }

    public byte ReadByte(Register register, int offset, int length)
    {
      Debug.Assert(offset >= 0 && length > 0 && offset + length <= 8);

      uint read = ReadByte(register);

      if (offset + length < 8)
      {
        return (byte)((read >> offset) & BitMask(length));
      }
      else
      {
        return (byte)(read >> offset);
      }
    }

    public void WriteByte(Register register, byte value, int offset, int length)
    {
      Debug.Assert(offset >= 0 && length > 0 && offset + length <= 8);

      uint mask = BitMask(length), current = ReadByte(register);

      // Erase current value
      current &= ~(mask << offset);

      // Write new value
      current |= (value & mask) << offset;

      Write((byte)register, (byte)current);
    }

    /// <summary>
    /// Read 16 bits from a given <see cref="Register"/> LSB first.
    /// </summary>
    /// <param name="register">The <see cref="Register"/> to read from.</param>
    /// <returns>Value from register.</returns>
    internal short ReadShort(Register register)
    {
      writeBuffer1[0] = (byte)register;

      /*
       * Instantiating a new array each time has no relevant performance drawbacks,
       * since it is only used for reading the calibration data at the beginning.
       */
      var buffer = new byte[2];

      WriteRead(device, writeBuffer1, buffer);

      return ToInt16(buffer);
    }

    /// <summary>
    /// Read and write <em>fully</em> the provided buffers.
    /// </summary>
    private static void WriteRead(I2CDevice device, byte[] writeBuffer, byte[] readBuffer)
    {
      var actions = new I2CDevice.I2CTransaction[]
      {
        I2CDevice.CreateWriteTransaction(writeBuffer),
        I2CDevice.CreateReadTransaction(readBuffer)
      };

      int attempt = 0, expectedLength = writeBuffer.Length + readBuffer.Length;
      while (device.Execute(actions, DefaultTimeout) < expectedLength)
      {
        if (++attempt >= MaxConnectionAttempts)
        {
          throw new IOException("Can not reach the device.");
        }
      }
    }
    #endregion

    private static double pow2(double x)
    {
      return x * x;
    }

    private static double pow3(double x)
    {
      return x * x * x;
    }

    /// <summary>
    /// Converts the first two bytes at the provided index to a signed 16-bit integer.
    /// </summary>
    private static short ToInt16(byte[] value, int index = 0)
    {
      return (short)(value[0 + index] << 0 | value[1 + index] << 8);
    }

    private static byte BitMask(int lenght)
    {
      switch (lenght)
      {
        case 0:
          // 0000_0000
          return 0x00;

        case 1:
          // 0000_0001
          return 0x01;

        case 2:
          // 0000_0011
          return 0x03;

        case 3:
          // 0000_0111
          return 0x07;

        case 4:
          // 0000_1111
          return 0x0F;

        case 5:
          // 0001_1111
          return 0x1F;

        case 6:
          // 0011_1111
          return 0x3F;

        case 7:
          // 0111_1111
          return 0x7F;

        case 8:
          // 1111_1111
          return 0xFF;

        default:
          throw new ArgumentOutOfRangeException("length", "Length must be between 0 and 8 for bytes.");
      }
    }

    /// <summary>
    /// Returns the number of measurment cycles for the provided oversampling.
    /// </summary>
    private static int CycleNumber(Oversampling oversampling)
    {
      switch (oversampling)
      {
        case Oversampling.Skipped:
          return 0;

        case Oversampling.x1:
          return 1;

        case Oversampling.x2:
          return 2;

        case Oversampling.x4:
          return 4;

        case Oversampling.x8:
          return 8;

        case Oversampling.x16:
          return 16;

        default:
          throw new ArgumentException("oversampling");
      }
    }
  }
}