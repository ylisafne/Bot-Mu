using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Kapi_Mu_Utility
{
    public class OCRHelper
    {
        private readonly int intervaloCompleto = 5;
        private int contadorImagenes = 0;
        private const int ANCHO_REFERENCIA = 1280;
        private const int ALTO_REFERENCIA = 720;
        private static readonly Bitmap TradeReferencia = CargarRecurso("TradeReference.png");
        public OCRHelper()
        {
            if (intervaloCompleto < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(intervaloCompleto));
        }

        public HelperResponse ValidarImagen(byte[] imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return new HelperResponse(ActionType.Error, "001 Imagen No Valida");

            contadorImagenes++;

            bool validarTodo = contadorImagenes >= intervaloCompleto;

            if (validarTodo)
                contadorImagenes = 0;

            // Trade se valida en cada imagen.
            HelperResponse result = DetectarTrade(imagen);

            if (result != null)
                return result;

            // En la imagen correspondiente, validar todo.
            if (validarTodo)
            {
                if (DetectarDesconexion(imagen))
                    return new HelperResponse(ActionType.Error, "002 Desconexion Detectada");

                if (DetectarError(imagen))
                    return new HelperResponse(ActionType.Error, "003 Error Detectado");
            }

            // No se detectó ninguna acción o error.
            return null;
        }

        private HelperResponse DetectarTrade(byte[] imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return null;
            //Valores Tomados cuando el Formulario es de 1280 x 720
            double xRelativo = 0.55;
            double yRelativo = 0.35;
            double anchoRelativo = 0.081;
            double altoRelativo = 0.055;

            byte[] recorte = RecortarImagen(
                                            imagen,
                                            xRelativo,
                                            yRelativo,
                                            anchoRelativo,
                                            altoRelativo);
            if (recorte == null)
                return null;
            //

            //


            bool resCompara = CompararImagenes( recorte,
                                                TradeReferencia,
                                                0.80);
            if (!resCompara)
                return null;
            
            // Obtener dimensiones de la imagen original.
            using (var ms = new MemoryStream(imagen))
            using (var original = new Bitmap(ms))
            {
                int x = (int)(original.Width * xRelativo);
                int y = (int)(original.Height * yRelativo);

                int w = (int)(original.Width * anchoRelativo);
                int h = (int)(original.Height * altoRelativo);

                // Centro del botón en coordenadas de la imagen.
                int centroX = x + w / 2;
                int centroY = y + h / 2;

                return new HelperResponse(ActionType.Trade, cursor: new Point(centroX, centroY));
            }
        }

        private bool DetectarDesconexion(byte[] imagen)
        {
            // TODO: Analizar la región de desconexión.


            /*
             // Guardar el recorte obtenido.
            string nombreArchivo = $"Desconexion_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            string ruta = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Output",
                nombreArchivo
            );
            File.WriteAllBytes(ruta, recorte);
            //
             
             */
            return false;
        }

        private bool DetectarError(byte[] imagen)
        {
            // TODO: Analizar la región de errores.
            return false;
        }

        //
        private byte[] RecortarImagen(
    byte[] imagen,
    double xRelativo,
    double yRelativo,
    double anchoRelativo,
    double altoRelativo)
        {
            if (imagen == null || imagen.Length == 0)
                return null;

            try
            {
                using (var ms = new MemoryStream(imagen))
                using (var original = new Bitmap(ms))
                {
                    int ancho = original.Width;
                    int alto = original.Height;

                    // Escalar cada eje respecto a la resolución base.
                    double escalaX = (double)ancho / ANCHO_REFERENCIA;
                    double escalaY = (double)alto / ALTO_REFERENCIA;

                    // Coordenadas definidas para 1280x720.
                    int x = (int)Math.Round(
                        ANCHO_REFERENCIA * xRelativo * escalaX);

                    int y = (int)Math.Round(
                        ALTO_REFERENCIA * yRelativo * escalaY);

                    int w = (int)Math.Round(
                        ANCHO_REFERENCIA * anchoRelativo * escalaX);

                    int h = (int)Math.Round(
                        ALTO_REFERENCIA * altoRelativo * escalaY);

                    // Evitar salir de los límites de la imagen.
                    x = Math.Clamp(x, 0, ancho - 1);
                    y = Math.Clamp(y, 0, alto - 1);
                    w = Math.Clamp(w, 1, ancho - x);
                    h = Math.Clamp(h, 1, alto - y);

                    using (var recorte = new Bitmap(w, h))
                    using (var g = Graphics.FromImage(recorte))
                    using (var resultado = new MemoryStream())
                    {
                        g.DrawImage(
                            original,
                            new Rectangle(0, 0, w, h),
                            new Rectangle(x, y, w, h),
                            GraphicsUnit.Pixel);

                        recorte.Save(resultado, ImageFormat.Png);

                        return resultado.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error recortando imagen: {ex.Message}");

                return null;
            }
        }

        private bool CompararImagenes( byte[] imagenBytes,
                                        Bitmap referencia,
                                        double similitudMinima = 0.90)
        {
            if (imagenBytes == null ||
                imagenBytes.Length == 0 ||
                referencia == null)
                return false;

            try
            {
                using (var ms = new MemoryStream(imagenBytes))
                using (var original = new Bitmap(ms))
                using (var normalizada = new Bitmap(
                    referencia.Width,
                    referencia.Height,
                    PixelFormat.Format24bppRgb))
                using (var plantilla = new Bitmap(
                    referencia.Width,
                    referencia.Height,
                    PixelFormat.Format24bppRgb))
                {
                    using (var g = Graphics.FromImage(normalizada))
                    {
                        g.Clear(Color.Black);

                        g.InterpolationMode =
                            System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                        g.DrawImage(
                            original,
                            new Rectangle(
                                0, 0,
                                referencia.Width,
                                referencia.Height));
                    }

                    using (var g = Graphics.FromImage(plantilla))
                    {
                        g.DrawImage(
                            referencia,
                            new Rectangle(
                                0, 0,
                                referencia.Width,
                                referencia.Height));
                    }

                    Rectangle rect = new Rectangle(
                        0, 0,
                        referencia.Width,
                        referencia.Height);

                    BitmapData datosA = normalizada.LockBits(
                        rect,
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format24bppRgb);

                    BitmapData datosB = plantilla.LockBits(
                        rect,
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format24bppRgb);

                    try
                    {
                        int ancho = referencia.Width;
                        int alto = referencia.Height;
                        int bytesFila = ancho * 3;

                        byte[] bufferA = new byte[bytesFila * alto];
                        byte[] bufferB = new byte[bytesFila * alto];

                        for (int y = 0; y < alto; y++)
                        {
                            Marshal.Copy(
                                IntPtr.Add(datosA.Scan0, y * datosA.Stride),
                                bufferA, y * bytesFila, bytesFila);

                            Marshal.Copy(
                                IntPtr.Add(datosB.Scan0, y * datosB.Stride),
                                bufferB, y * bytesFila, bytesFila);
                        }

                        int iguales = 0;
                        int totalPixeles = ancho * alto;

                        const int tolerancia = 30;

                        for (int i = 0; i < bufferA.Length; i += 3)
                        {
                            if (Math.Abs(bufferA[i] - bufferB[i]) <= tolerancia &&
                                Math.Abs(bufferA[i + 1] - bufferB[i + 1]) <= tolerancia &&
                                Math.Abs(bufferA[i + 2] - bufferB[i + 2]) <= tolerancia)
                            {
                                iguales++;
                            }
                        }

                        double similitud =
                            (double)iguales / totalPixeles;

                        System.Diagnostics.Debug.WriteLine(
                            $"Similitud Trade: {similitud:P2}");

                        return similitud >= similitudMinima;
                    }
                    finally
                    {
                        normalizada.UnlockBits(datosA);
                        plantilla.UnlockBits(datosB);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error comparando imágenes: {ex.Message}");

                return false;
            }
        }
        private static Bitmap CargarRecurso(string nombreArchivo)
        {
            var assembly = Assembly.GetExecutingAssembly();

            string nombreRecurso = null;

            foreach (string nombre in assembly.GetManifestResourceNames())
            {
                if (nombre.EndsWith(
                    nombreArchivo,
                    StringComparison.OrdinalIgnoreCase))
                {
                    nombreRecurso = nombre;
                    break;
                }
            }

            if (nombreRecurso == null)
                throw new FileNotFoundException(
                    $"No se encontró el recurso incrustado: {nombreArchivo}");

            using (Stream stream = assembly.GetManifestResourceStream(nombreRecurso))
            using (var imagen = new Bitmap(stream))
            {
                return new Bitmap(imagen);
            }
        }
    }
}
