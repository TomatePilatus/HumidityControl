namespace HumidityControl.Bme680
{
  /// <summary>
  /// Calibration data for the <see cref="Bme680"/>.
  /// </summary>
  /// <seealso href="https://github.com/georgemathieson/bme680"/>
  public class CalibrationData
  {
    /// <summary>
    /// Gets a temperature coefficient from <see cref="Register.temp_cal_1"/>.
    /// </summary>
    public readonly ushort TCal1;

    /// <summary>
    /// Gets a temperature coefficient from <see cref="Register.temp_cal_2"/>.
    /// </summary>
    public readonly short TCal2;

    /// <summary>
    /// Gets a temperature coefficient from <see cref="Register.temp_cal_3"/>
    /// </summary>
    public readonly byte TCal3;

    /// <summary>
    /// Gets a humidity coefficient from <see cref="Register.hum_cal_1_msb"/> and <see cref="Register.hum_cal_1_lsb"/>.
    /// </summary>
    public readonly ushort HCal1;

    /// <summary>
    /// Gets a humidity coefficient from <see cref="Register.hum_cal_2_msb"/> and <see cref="Register.hum_cal_2_lsb"/>.
    /// </summary>
    public readonly ushort HCal2;

    /// <summary>
    /// Gets a humidity coefficient from <see cref="Register.hum_cal_3"/>.
    /// </summary>
    public readonly sbyte HCal3;

    /// <summary>
    /// Gets a humidity coefficient from <see cref="Register.hum_cal_4"/>.
    /// </summary>
    public readonly sbyte HCal4;

    /// <summary>
    /// Gets a humidity coefficient from <see cref="Register.hum_cal_5"/>.
    /// </summary>
    public readonly sbyte HCal5;

    /// <summary>
    /// Gets a humidity coefficient from <see cref="Register.hum_cal_6"/>.
    /// </summary>
    public readonly byte HCal6;

    /// <summary>
    /// Gets a humidity coefficient from <see cref="Register.hum_cal_7"/>.
    /// </summary>
    public readonly sbyte HCal7;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_1_lsb"/>.
    /// </summary>
    public readonly ushort PCal1;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_2_lsb"/>.
    /// </summary>
    public readonly short PCal2;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_3"/>.
    /// </summary>
    public readonly byte PCal3;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_4_lsb"/>.
    /// </summary>
    public readonly short PCal4;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_5_lsb"/>.
    /// </summary>
    public readonly short PCal5;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_6"/>.
    /// </summary>
    public readonly byte PCal6;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_7"/>.
    /// </summary>
    public readonly byte PCal7;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_8"/>.
    /// </summary>
    public readonly short PCal8;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_9"/>.
    /// </summary>
    public readonly short PCal9;

    /// <summary>
    /// Gets a pressure coefficient from <see cref="Register.pres_cal_10"/>.
    /// </summary>
    public readonly byte PCal10;

    /// <summary>
    /// Gets a gas coefficient from <see cref="Register.range_switching_error"/>.
    /// </summary>
    public readonly int GCal1;

    public static readonly double[] GArray1 = { 1.0, 1.0, 1.0, 1.0, 1.0, .99, 1.0, .992, 1.0, 1.0, .998, .995, 1.0, .99, 1.0, 1.0 };

    public static readonly double[] GArray2 = { 8e6, 4e6, 2e6, 1e6, 499500.4995, 248262.1648, 125e3, 63004.03226, 31281.28128, 15625, 7812.5, 3906.25, 1953.125, 976.5625, 488.28125, 244.140625 };

    /// <summary>
    /// Read coefficient data from device.
    /// </summary>
    /// <param name="bme680">The <see cref="Bme680"/> to read coefficient data from.</param>
    internal CalibrationData(Bme680 bme680)
    {
      // Temperature.
      TCal1 = (ushort)bme680.ReadShort(Register.temp_cal_1);
      TCal2 = bme680.ReadShort(Register.temp_cal_2);
      TCal3 = bme680.ReadByte(Register.temp_cal_3);

      // Humidity.
      HCal1 = (ushort)((bme680.ReadByte(Register.hum_cal_1_msb) << 4) | (bme680.ReadByte(Register.hum_cal_1_lsb) & /* 0b_0000_1111 */ 0x0F));
      HCal2 = (ushort)((bme680.ReadByte(Register.hum_cal_2_msb) << 4) | (bme680.ReadByte(Register.hum_cal_2_lsb) >> 4));
      HCal3 = (sbyte)bme680.ReadByte(Register.hum_cal_3);
      HCal4 = (sbyte)bme680.ReadByte(Register.hum_cal_4);
      HCal5 = (sbyte)bme680.ReadByte(Register.hum_cal_5);
      HCal6 = bme680.ReadByte(Register.hum_cal_6);
      HCal7 = (sbyte)(bme680.ReadByte(Register.hum_cal_7));

      // Pressure.
      PCal1 = (ushort)bme680.ReadShort(Register.pres_cal_1_lsb);
      PCal2 = bme680.ReadShort(Register.pres_cal_2_lsb);
      PCal3 = bme680.ReadByte(Register.pres_cal_3);
      PCal4 = bme680.ReadShort(Register.pres_cal_4_lsb);
      PCal5 = bme680.ReadShort(Register.pres_cal_5_lsb);
      PCal6 = bme680.ReadByte(Register.pres_cal_6);
      PCal7 = bme680.ReadByte(Register.pres_cal_7);
      PCal8 = bme680.ReadShort(Register.pres_cal_8_lsb);
      PCal9 = bme680.ReadShort(Register.pres_cal_9_lsb);
      PCal10 = bme680.ReadByte(Register.pres_cal_10);

      // Gas.
      // Range switching error signed 4bit
      GCal1 = ToInt32(bme680.ReadByte(Register.range_switching_error, 4, 4));
    }

    /// <summary>
    /// Converts the provided 4-bit signed integer to an integer ranging from -8 to 7.
    /// </summary>
    /// <remarks>
    /// For a <c>n</c>-bit signed integer we have:
    /// <code>
    /// (sbyte)(value &lt;&lt; n) &gt;&gt; n;
    /// </code>
    /// </remarks>
    private static int ToInt32(byte i4)
    {
      // Logic shift then arithmetic shift:
      return (sbyte)(i4 << 4) >> 4;
    }
  }
}