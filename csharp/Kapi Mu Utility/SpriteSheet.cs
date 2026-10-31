using System;
using System.Drawing;
using System.Drawing.Imaging;

public class SpriteSheet : IDisposable
{
    private readonly Bitmap sheet;

    public SpriteSheet(string ruta)
    {
        sheet = new Bitmap(ruta);
    }

    public Bitmap ObtenerIcono(
        int x,
        int y,
        int ancho,
        int alto)
    {
        Rectangle area = new Rectangle(
            x, y, ancho, alto);

        return sheet.Clone(
            area,
            PixelFormat.Format32bppArgb);
    }

    public void Dispose()
    {
        sheet.Dispose();
    }
}