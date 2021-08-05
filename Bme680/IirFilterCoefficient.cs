namespace HumidityControl.Bme680
{
  /// <summary>
  /// Coefficients for the infinite impulse response (IIR) filter, applied to temperature and pressure data but not to humidity and gas data.
  /// </summary>
  public enum IirFilterSize : byte
  {
    C0 = 0,
    C1 = 1,
    C3 = 2,
    C7 = 3,
    C15 = 4,
    C31 = 5,
    C63 = 6,
    C127 = 7
  }
}