using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using Tesseract;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace Kapi_Mu_Utility
{
    public class OCRHelper
    {
        private readonly int intervaloCompleto = 5;
        private int contadorImagenes = 0;
        private const int ANCHO_REFERENCIA = 1280;
        private const int ALTO_REFERENCIA = 720;
        private static readonly Bitmap TradeReferencia = CargarRecurso("TradeReference.png");
        private readonly OCRService servicioOCR;
        public OCRHelper(OCRService servicioOCR)
        {
            if (intervaloCompleto < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(intervaloCompleto));
            this.servicioOCR = servicioOCR;
        }

        public async Task<HelperResponse> ValidarImagen(
            byte[] imagen,
            CancellationToken token = default)
        {
            if (imagen == null || imagen.Length == 0)
                return new HelperResponse(ActionType.Error, "001 Imagen No Valida");

            token.ThrowIfCancellationRequested();

            contadorImagenes++;

            bool validarTodo = contadorImagenes >= intervaloCompleto;

            if (validarTodo)
            {
                System.Diagnostics.Debug.WriteLine($"ValidarTodo");
                contadorImagenes = 0;
                return await DetectarDesconexionError(imagen, token);
            }
            else {
                System.Diagnostics.Debug.WriteLine($"TradeEvent");
                return DetectarTrade(imagen);
                
            }
        }

        public bool ValidarJuego(byte[] imagen) {
            if (imagen == null || imagen.Length == 0)
                return false;
            double xRelativo = 0.81;
            double yRelativo = 0.02;
            double anchoRelativo = 0.045;
            double altoRelativo = 0.07917;
            byte[] recorte = RecortarImagen(imagen, xRelativo, yRelativo, anchoRelativo, altoRelativo);
            if (recorte == null)
                return false;

            //
            string carpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            Directory.CreateDirectory(carpeta);
            File.WriteAllBytes(Path.Combine(carpeta, $"Desconexion_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png"), recorte);


            //

            return false;
        }

        private HelperResponse DetectarTrade(byte[] imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return null;

            double xRelativo = 0.55;
            double yRelativo = 0.35;
            double anchoRelativo = 0.081;
            double altoRelativo = 0.055;

            byte[] recorte = RecortarImagen(imagen, xRelativo, yRelativo, anchoRelativo, altoRelativo);
            if (recorte == null)
                return null;

            bool resCompara = CompararImagenes(recorte, TradeReferencia, 0.80);
            if (!resCompara)
                return null;

            using (var ms = new MemoryStream(imagen))
            using (var original = new Bitmap(ms))
            {
                int x = (int)(original.Width * xRelativo);
                int y = (int)(original.Height * yRelativo);

                int w = (int)(original.Width * anchoRelativo);
                int h = (int)(original.Height * altoRelativo);

                int centroX = x + w / 2;
                int centroY = y + h / 2;

                return new HelperResponse(ActionType.Trade, "Trade Detectado", cursor: new Point(centroX, centroY));
            }
        }

        private async Task<HelperResponse> DetectarDesconexionError(
            byte[] imagen,
            CancellationToken token)
        {
            Point? punto = await BuscarTextoAsync(
                imagen,
                token,
                "Volver a entrar",
                "Reload");

            // RETORNO DIRECTO: Si no encuentra el texto, retorna null sin validar localmente.
            if (!punto.HasValue)
                return null;

            System.Diagnostics.Debug.WriteLine($"Desconexión detectada en {punto.Value}");

            return new HelperResponse(
                ActionType.Reconexion,
                "Desconexion Detectada",
                cursor: punto.Value);
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

                        recorte.Save(resultado, System.Drawing.Imaging.ImageFormat.Png);

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

        private bool CompararImagenes(byte[] imagenBytes,
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
        #region OCR
        private async Task<Point?> BuscarTextoAsync(
        byte[] imagen,
        CancellationToken token,
        params string[] textosBuscar)
        {
            if (imagen == null || imagen.Length == 0 || textosBuscar == null || textosBuscar.Length == 0)
                return null;

            var textosNormalizados = textosBuscar
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(NormalizarTexto)
                .ToArray();

            if (textosNormalizados.Length == 0)
                return null;

            return await servicioOCR.EjecutarAsync(engine =>
            {
                using var pixOriginal = Pix.LoadFromMemory(imagen);
                using var pixGray = pixOriginal.ConvertRGBToGray();

                // SOLUCIÓN AL CAMBIO DE MAPAS: Forzar binarización limpia (Blanco y Negro puro)
                // Otsu adaptativo calcula el umbral perfecto sin importar si el fondo del mapa es claro u oscuro.
                using var pix = pixGray.BinarizeOtsuAdaptiveThreshold(200, 200, 10, 10, 0.10f);

                // Usamos SingleBlock o Auto. Si la pantalla tiene mucha interfaz, SingleBlock fuerza a buscar bloques sólidos de texto
                using var page = engine.Process(pix, PageSegMode.Auto);
                using var iter = page.GetIterator();

                if (iter == null) return null;

                iter.Begin();

                do
                {
                    token.ThrowIfCancellationRequested();

                    // Usamos un bloque try-catch interno o validación de seguridad porque GetText en TextLine 
                    // a veces falla si el iterador encuentra "basura" visual en los fondos de ciertos mapas.
                    string linea = null;
                    try
                    {
                        linea = iter.GetText(PageIteratorLevel.TextLine);
                    }
                    catch { continue; }

                    if (string.IsNullOrWhiteSpace(linea))
                        continue;

                    string texto = NormalizarTexto(linea);

                    // Cambiado a coincidencia flexible (por si el OCR lee caracteres extraños pegados al texto)
                    if (textosNormalizados.Any(t => texto.Contains(t)))
                    {
                        if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
                        {
                            return new Point(
                                (rect.X1 + rect.X2) / 2,
                                (rect.Y1 + rect.Y2) / 2);
                        }
                    }
                }
                while (iter.Next(PageIteratorLevel.TextLine));

                return (Point?)null;

            }, token);
        }

        private static string NormalizarTexto(string texto)
        {
            return string.Join(
                " ",
                texto.ToLowerInvariant()
                     .Split(
                         (char[])null,
                         StringSplitOptions.RemoveEmptyEntries));
        }
        #endregion
    }
}