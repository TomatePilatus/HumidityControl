using System;
using Microsoft.SPOT;

namespace HumidityControl
{
  [Serializable]
  public sealed class ApplicationBootInformation
  {
    public int FanSpeed { get; set; }

    public ApplicationBootInformation(int FanSpeed)
    {
      if (FanSpeed < 0 || 100 < FanSpeed)
      {
        throw new ArgumentOutOfRangeException("FanSpeed");
      }

      this.FanSpeed = FanSpeed;
    }
  }
}
