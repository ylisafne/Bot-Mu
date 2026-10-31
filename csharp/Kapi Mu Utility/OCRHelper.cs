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
        private readonly int intervaloCompleto = 20;
        private int contadorImagenes = 0;
        private const int ANCHO_REFERENCIA = 1280;
        private const int ALTO_REFERENCIA = 720;
        private static readonly Bitmap TradeReferencia = CargarRecurso("TradeReference.png");
        private readonly OCRService servicioOCR;
        private readonly BuscadorDePlantillas searchIMG = new BuscadorDePlantillas();
        private readonly string vvPerfil;
        public OCRHelper(OCRService servicioOCR, string vvPerfil)
        {
            if (intervaloCompleto < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(intervaloCompleto));
            this.servicioOCR = servicioOCR;
            this.vvPerfil = vvPerfil;
        }

        public async Task<HelperResponse> ValidarImagen(
    byte[] imagen,
    CancellationToken token = default)
        {
            if (imagen == null || imagen.Length == 0)
                return new HelperResponse(ActionType.Error, "001 Imagen No Valida");

            token.ThrowIfCancellationRequested();

            contadorImagenes++;

            // Primero buscar Trade
            System.Diagnostics.Debug.WriteLine($"{vvPerfil} Buscando Trade");
            var resultadoTrade = DetectarTrade(imagen);

            if (resultadoTrade != null)
                return resultadoTrade;

            // Si no encontró Trade, evaluar búsqueda completa
            bool validarTodo = contadorImagenes >= intervaloCompleto;

            if (validarTodo)
            {
                System.Diagnostics.Debug.WriteLine($"{vvPerfil} ValidarTodo");
                contadorImagenes = 0;

                return await DetectarDesconexionError(imagen, token);
            }

            return new HelperResponse(ActionType.Error, "Sin resultados");
        }
        public async Task<Point?> ComparaSkill(byte[] imagen, string path)
        {
            try
            {
                byte[] recorte = GetSkill(imagen);
                
                if (recorte == null || recorte.Length == 0)
                    return null;

                string archivo = Path.Combine(path, "skill.png");

                using (Bitmap skill = new Bitmap(archivo))
                {
                    bool result = CompararImagenes(recorte, skill, 0.80);

                    if (!result)
                    {
                        using (MemoryStream ms = new MemoryStream(imagen))
                        using (Bitmap original = new Bitmap(ms))
                        {
                            int x = (int)((0.485 + 0.025 / 2) * original.Width);
                            int y = (int)((0.94 + 0.05 / 2) * original.Height);

                            return new Point(x, y);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{vvPerfil} Error comparando Skill: {ex}");

                return null;
            }
        }
        public async Task<Point?> BuscaSKill(byte[] imagen, string path)
        {
            try
            {
                double umbral = 0.80;
                string pathSkill = Path.Combine(path, "skill.png");
                //for dev Save Imagen
                string pathImg = Path.Combine(path, "screen.png");
                await File.WriteAllBytesAsync(pathImg, imagen);
                //
                using (Bitmap skill = new Bitmap(pathSkill)) {
                    if (searchIMG.BuscaImagen(imagen, skill,
                                                umbral,
                                                out Point punto))
                    {
                        // Icono encontrado
                        return punto;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"{vvPerfil} No se encontró el icono.");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{vvPerfil} Error comparando Skill: {ex}");

                return null;
            }
        }
        public async Task<bool> GuardarSkill(byte[] imagen, string path)
        {
            try
            {
                // Obtener el recorte mediante GetSkill
                byte[] recorte = GetSkill(imagen);

                if (recorte == null || recorte.Length == 0)
                    return false;

                // Guardar el recorte en disco
                string archivo = Path.Combine(path, "skill.png");

                await File.WriteAllBytesAsync(archivo, recorte);

                return File.Exists(archivo) &&
                       new FileInfo(archivo).Length == recorte.Length;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{vvPerfil} Error guardando Skill: {ex}");

                return false;
            }
        }
        private byte[] GetSkill(byte[] imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return null;

            try
            {
                double xRelativo = 0.485;
                double yRelativo = 0.94;
                double anchoRelativo = 0.025;
                double altoRelativo = 0.05;

                return RecortarImagen(
                    imagen,
                    xRelativo,
                    yRelativo,
                    anchoRelativo,
                    altoRelativo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{vvPerfil} Error obteniendo Skill: {ex}");

                return null;
            }
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

            System.Diagnostics.Debug.WriteLine($"{vvPerfil} Desconexión detectada en {punto.Value}");

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
                    $"{vvPerfil} Error recortando imagen: {ex.Message}");

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
                    $"{vvPerfil} Error comparando imágenes: {ex.Message}");

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
            if (imagen == null || imagen.Length == 0 ||
                textosBuscar == null || textosBuscar.Length == 0)
                return null;

            var textosNormalizados = textosBuscar
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(NormalizarTexto)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToArray();

            if (textosNormalizados.Length == 0)
                return null;

            return await servicioOCR.EjecutarAsync(engine =>
            {
                using var original = Pix.LoadFromMemory(imagen);
                using var gris = original.ConvertRGBToGray();

                // Segunda versión: binarización adaptativa para fondos
                // con iluminación desigual.
                using var binario =
                    gris.BinarizeOtsuAdaptiveThreshold(
                        200, 200, 10, 10, 0.10f);

                Point? BuscarEnPix(Pix pix, PageSegMode modo)
                {
                    using var page = engine.Process(pix, modo);
                    using var iter = page.GetIterator();

                    if (iter == null)
                        return null;

                    iter.Begin();

                    do
                    {
                        token.ThrowIfCancellationRequested();

                        string linea;

                        try
                        {
                            linea = iter.GetText(
                                PageIteratorLevel.TextLine);
                        }
                        catch
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(linea))
                            continue;

                        string texto = NormalizarTexto(linea);

                        if (!textosNormalizados.Any(t =>
                            texto.Contains(
                                t, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        if (iter.TryGetBoundingBox(
                            PageIteratorLevel.TextLine,
                            out var rect))
                        {
                            return new Point(
                                (rect.X1 + rect.X2) / 2,
                                (rect.Y1 + rect.Y2) / 2);
                        }
                    }
                    while (iter.Next(PageIteratorLevel.TextLine));

                    return null;
                }

                // PASADA 1: escala de grises, conservando los tonos originales.
                token.ThrowIfCancellationRequested();

                var punto = BuscarEnPix(gris, PageSegMode.Auto);

                if (punto.HasValue)
                    return punto;

                // PASADA 2: escala de grises con segmentación de línea.
                token.ThrowIfCancellationRequested();

                punto = BuscarEnPix(gris, PageSegMode.SingleLine);

                if (punto.HasValue)
                    return punto;

                // PASADA 3: binarización adaptativa.
                token.ThrowIfCancellationRequested();

                punto = BuscarEnPix(binario, PageSegMode.Auto);

                if (punto.HasValue)
                    return punto;

                // PASADA 4: binarización adaptativa como línea única.
                token.ThrowIfCancellationRequested();

                return BuscarEnPix(binario, PageSegMode.SingleLine);

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