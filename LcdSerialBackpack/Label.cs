using System;

namespace HumidityControl.LcdSerialBackpack
{
  public sealed class Label
  {
    private readonly LcdScreen screen;
    private readonly int x, y;
    private readonly TextAlign align;
    private string text, lastText = string.Empty;

    public Label(LcdScreen screen, int column, int row, TextAlign align = TextAlign.Left)
    {
      if (screen == null)
      {
        throw new ArgumentNullException("screen");
      }

      this.screen = screen;
      this.x = LcdScreen.TextX(column);
      this.y = LcdScreen.TextY(row);
      this.align = align;
    }

    public string Text
    {
      get { return text; }
      set { text = value != null ? value : string.Empty; }
    }

    public void Draw()
    {
      if (Object.Equals(text, lastText))
      {
        // Nothing changed
        return;
      }

      switch (align)
      {
        case TextAlign.Right:
          screen.TextAtPixel(x - Math.Max(text.Length, lastText.Length) * LcdScreen.CharWidth, y);

          if (lastText.Length > text.Length)
          {
            // Clears old text whenever it is longer.
            int clear = lastText.Length - Text.Length;

            for (int i = 0; i < clear; i++)
            {
              screen.Draw(' ');
            }
          }

          screen.Draw(Text);

          break;

        case TextAlign.Left:
        default:
          screen.TextAtPixel(x, y).Draw(Text);

          if (lastText.Length > Text.Length)
          {
            // Clears old text whenever it is longer.
            int clear = lastText.Length - Text.Length;

            for (int i = 0; i < clear; i++)
            {
              screen.Draw(' ');
            }
          }

          break;
      }


      lastText = Text == null ? string.Empty : Text;
    }

    public enum TextAlign
    {
      Left, Right
    }
  }
}