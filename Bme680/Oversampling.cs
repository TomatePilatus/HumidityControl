namespace HumidityControl.Bme680
{
  /// <summary>
  /// Oversampling settings used to control noise reduction.
  /// </summary>
  /// <seealso href="https://github.com/georgemathieson/bme680"/>
  public enum Oversampling : byte
  {
    /// <summary>
    /// Skipped (output set to 0x8000).
    /// </summary>
    Skipped = 0, // 0b000,

    /// <summary>
    /// Oversampling x 1.
    /// </summary>
    x1 = 1, // 0b001,

    /// <summary>
    /// Oversampling x 2.
    /// </summary>
    x2 = 2, // 0b010,

    /// <summary>
    /// Oversampling x 4.
    /// </summary>
    x4 = 3, // 0b011,

    /// <summary>
    /// Oversampling x 8.
    /// </summary>
    x8 = 4, //0b100,

    /// <summary>
    /// Oversampling x 16.
    /// </summary>
    x16 = 5 // 0b101
  }
}