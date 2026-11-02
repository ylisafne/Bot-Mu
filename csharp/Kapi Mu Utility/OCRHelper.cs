using System;
using System;
using System.Drawing;
using System.Drawing;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks;
using Tesseract;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

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
            bool validarTodo = false; //contadorImagenes >= intervaloCompleto;

            if (validarTodo)
            {
                System.Diagnostics.Debug.WriteLine($"{vvPerfil} ValidarTodo");
                contadorImagenes = 0;

                return await DetectarDesconexionError(imagen, token);
            }

            return new HelperResponse(ActionType.Error, "Sin resultados");
        }

        //
        public async Task<Point?> ObtenerPosicionPersonajeAsync(
    byte[] imagen,
    string characterName,
    CancellationToken token = default)
        {
            if (imagen == null || imagen.Length == 0 ||
                string.IsNullOrWhiteSpace(characterName))
                return null;

            double anchoRelativo = 0.08;
            double altoRelativo = 0.03;

            // Obtener dimensiones originales de la imagen.
            using var ms = new MemoryStream(imagen);
            using var bitmap = new Bitmap(ms);

            double anchoImagen = bitmap.Width;
            double altoImagen = bitmap.Height;

            // Coordenadas relativas de los personajes.
            var posiciones = new[]
                {
                    (X: 0.210, Y: 0.423),
                    (X: 0.344, Y: 0.418),
                    (X: 0.473, Y: 0.418),
                    (X: 0.600, Y: 0.420),
                    (X: 0.730, Y: 0.428)
                };

            foreach (var posicion in posiciones)
            {
                token.ThrowIfCancellationRequested();

                byte[] recorte = RecortarImagen(
                    imagen,
                    posicion.X,
                    posicion.Y,
                    anchoRelativo,
                    altoRelativo);

                bool encontrado = await ExistePalabraEnImagenAsync(
                    recorte,
                    characterName,
                    token);

                if (encontrado)
                {
                    // Centro del recorte.
                    double x = (posicion.X + anchoRelativo / 2) * anchoImagen;
                    double y = (posicion.Y + altoRelativo / 2) * altoImagen;

                    // Desplazamiento de 50 píxeles hacia la derecha.
                    y += 200;

                    return new Point(
                        (int)Math.Round(x),
                        (int)Math.Round(y));
                }
            }

            return null;
        }
        public async Task<Point?> ObtenerPosicionPersonajeAsync_Basic(
            byte[] imagen,
            string characterName,
            CancellationToken token = default)
        {
            if (imagen == null || imagen.Length == 0 || string.IsNullOrEmpty(characterName))
                return null;
            byte[] recorte;
            bool result;

            double anchoRelativo = 0.08;
            double altoRelativo = 0.03;

            // Personaje 1
            double xRelativo_1 = 0.210;
            double yRelativo_1 = 0.423;
            recorte = RecortarImagen(imagen, xRelativo_1, yRelativo_1, anchoRelativo, altoRelativo);
            result = await ExistePalabraEnImagenAsync(recorte, characterName);





            string vv_File_1 = Path.Combine(AppContext.BaseDirectory, "output", "recorte_1.png");
            File.WriteAllBytes(vv_File_1, recorte);

            // Personaje 2
            double xRelativo_2 = 0.344;
            double yRelativo_2 = 0.418;
            recorte = RecortarImagen(imagen, xRelativo_2, yRelativo_2, anchoRelativo, altoRelativo);
            result = await ExistePalabraEnImagenAsync(recorte, characterName);

            string vv_File_2 = Path.Combine(AppContext.BaseDirectory, "output", "recorte_2.png");
            File.WriteAllBytes(vv_File_2, recorte);

            // Personaje 3
            double xRelativo_3 = 0.473;
            double yRelativo_3 = 0.418;
            recorte = RecortarImagen(imagen, xRelativo_3, yRelativo_3, anchoRelativo, altoRelativo);
            result = await ExistePalabraEnImagenAsync(recorte, characterName);

            string vv_File_3 = Path.Combine(AppContext.BaseDirectory, "output", "recorte_3.png");
            File.WriteAllBytes(vv_File_3, recorte);

            // Personaje 4
            double xRelativo_4 = 0.600;
            double yRelativo_4 = 0.420;
            recorte = RecortarImagen(imagen, xRelativo_4, yRelativo_4, anchoRelativo, altoRelativo);
            result = await ExistePalabraEnImagenAsync(recorte, characterName);

            string vv_File_4 = Path.Combine(AppContext.BaseDirectory, "output", "recorte_4.png");
            File.WriteAllBytes(vv_File_4, recorte);

            // Personaje 5
            double xRelativo_5 = 0.730;
            double yRelativo_5 = 0.428;
            recorte = RecortarImagen(imagen, xRelativo_5, yRelativo_5, anchoRelativo, altoRelativo);
            result = await ExistePalabraEnImagenAsync(recorte, characterName);

            string vv_File_5 = Path.Combine(AppContext.BaseDirectory, "output", "recorte_5.png");
            File.WriteAllBytes(vv_File_5, recorte);


            return null;


        }
        public async Task<bool> ExistePalabraEnImagenAsync( byte[] imagen,
                                                            string palabra,
                                                            CancellationToken token = default)
        {
            if (imagen == null || imagen.Length == 0 ||
                string.IsNullOrWhiteSpace(palabra))
                return false;

            string textoOCR = await servicioOCR.EjecutarAsync(engine =>
            {
                using var pix = Pix.LoadFromMemory(imagen);
                using var pagina = engine.Process(
                    pix,
                    PageSegMode.SingleLine);

                return pagina.GetText() ?? string.Empty;

            }, token);

            if (string.IsNullOrWhiteSpace(textoOCR))
                return false;

            string Normalizar(string texto)
            {
                texto = texto.ToUpperInvariant()
                    .Normalize(NormalizationForm.FormD);

                var resultado = new StringBuilder();

                foreach (char c in texto)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(c) ==
                        UnicodeCategory.NonSpacingMark)
                        continue;

                    if (char.IsLetterOrDigit(c))
                        resultado.Append(c);
                }

                return resultado.ToString();
            }

            string texto = Normalizar(textoOCR);
            string objetivo = Normalizar(palabra);

            if (string.IsNullOrEmpty(texto) ||
                string.IsNullOrEmpty(objetivo))
                return false;

            // Coincidencia exacta normalizada.
            if (texto.Contains(objetivo))
                return true;

            // Equivalencias frecuentes de OCR.
            string CorregirOCR(string valor)
            {
                return valor
                    .Replace('0', 'O')
                    .Replace('1', 'I')
                    .Replace('5', 'S')
                    .Replace('8', 'B');
            }

            texto = CorregirOCR(texto);
            objetivo = CorregirOCR(objetivo);

            if (texto.Contains(objetivo))
                return true;

            // Distancia Levenshtein.
            int Distancia(string a, string b)
            {
                int[,] matriz = new int[a.Length + 1, b.Length + 1];

                for (int i = 0; i <= a.Length; i++)
                    matriz[i, 0] = i;

                for (int j = 0; j <= b.Length; j++)
                    matriz[0, j] = j;

                for (int i = 1; i <= a.Length; i++)
                {
                    for (int j = 1; j <= b.Length; j++)
                    {
                        int costo = a[i - 1] == b[j - 1] ? 0 : 1;

                        matriz[i, j] = Math.Min(
                            Math.Min(
                                matriz[i - 1, j] + 1,
                                matriz[i, j - 1] + 1),
                            matriz[i - 1, j - 1] + costo);
                    }
                }

                return matriz[a.Length, b.Length];
            }

            // Comparar palabras completas.
            foreach (Match coincidencia in Regex.Matches(
                texto,
                @"[\p{L}\p{N}]+"))
            {
                string encontrada = coincidencia.Value;

                if (Math.Abs(encontrada.Length - objetivo.Length) <= 1 &&
                    Distancia(encontrada, objetivo) <= 1)
                    return true;
            }

            return false;
        }
        public HelperResponse DetectarSeleccionPjTest(byte[] imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return null;

            double xRelativo = 0.205;
            double yRelativo = 0.425;
            double anchoRelativo = 0.5;
            double altoRelativo = 0.5;

            string eval = "X"; // X o Y

            if (eval.ToUpper() == "X") {
                for (double x = xRelativo; x < 1; x += 0.01)
                {

                    byte[] recorte = RecortarImagen(
                        imagen,
                        x,
                        yRelativo,
                        anchoRelativo,
                        altoRelativo
                    );

                    if (recorte == null)
                        continue;

                    string carpetaEjecucion = AppContext.BaseDirectory;
                    string carpetaOutput = Path.Combine(carpetaEjecucion, "output");

                    string nombreArchivo = $"img_{x:F3}_{yRelativo:F3}.png";
                    string rutaCompleta = Path.Combine(carpetaOutput, nombreArchivo);

                    File.WriteAllBytes(rutaCompleta, recorte);
                }
                return null;
            }
            if (eval.ToUpper() == "Y"){
                for (double y = yRelativo; y < 1; y += 0.01)
                {

                    byte[] recorte = RecortarImagen(
                        imagen,
                        xRelativo,
                        y,
                        anchoRelativo,
                        altoRelativo
                    );

                    if (recorte == null)
                        continue;

                    string carpetaEjecucion = AppContext.BaseDirectory;
                    string carpetaOutput = Path.Combine(carpetaEjecucion, "output");

                    string nombreArchivo = $"img_{xRelativo:F3}_{y:F3}.png";
                    string rutaCompleta = Path.Combine(carpetaOutput, nombreArchivo);

                    File.WriteAllBytes(rutaCompleta, recorte);
                }
                return null;
            }
            return null;
        }
        public HelperResponse DetectarSeleccionPj(byte[] imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return null;

            double xRelativo = 0.908;
            double yRelativo = 0.892;
            double anchoRelativo = 0.075;
            double altoRelativo = 0.035;

            Bitmap OjReference = CargarRecurso("PjReference.png");

            byte[] recorte = RecortarImagen(imagen, xRelativo, yRelativo, anchoRelativo, altoRelativo);
            if (recorte == null)
                return null;

            //string vvFile = Path.Combine(AppContext.BaseDirectory, "output", "Compara.png");
            //File.WriteAllBytes(vvFile, recorte);

            bool resCompara = CompararImagenes(recorte, OjReference, 0.95);
            if (!resCompara)
                return null;

            using (var ms = new MemoryStream(imagen))
            using (var original = new Bitmap(ms))
            {
                int x = (int)(original.Width * xRelativo);
                int y = (int)(original.Height * yRelativo);

                int w = (int)(original.Width * anchoRelativo);
                int h = (int)(original.Height * altoRelativo);

                int mitadAncho = w / 2;

                int centroX = x + mitadAncho / 2;
                int centroY = y + h / 2;

                return new HelperResponse(ActionType.SelecionPj, "Seleccion de Personaje", cursor: new System.Drawing.Point(centroX, centroY));
            }
        }

        /// <summary>
        /// Binariza la imagen usando memoria directa (unsafe). 
        /// Es hasta 20 veces más rápido que GetPixel/SetPixel convencional.
        /// </summary>

        public async Task<System.Drawing.Point?> ComparaSkill(byte[] imagen, string path)
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

                            return new System.Drawing.Point(x, y);
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
        public async Task<System.Drawing.Point?> BuscaSKill(byte[] imagen, string path)
        {
            try
            {
                double umbral = 0.80;
                string pathSkill = Path.Combine(path, "skill.png");
                //for dev Save Imagen
                //string pathImg = Path.Combine(path, "screen.png");
                //await File.WriteAllBytesAsync(pathImg, imagen);
                //
                using (Bitmap skill = new Bitmap(pathSkill)) {
                    if (searchIMG.BuscaImagen(imagen, skill,
                                                umbral,
                                                out System.Drawing.Point punto))
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

                return new HelperResponse(ActionType.Trade, "Trade Detectado", cursor: new System.Drawing.Point(centroX, centroY));
            }
        }

        private async Task<HelperResponse> DetectarDesconexionError(
            byte[] imagen,
            CancellationToken token)
        {
            System.Drawing.Point? punto = await BuscarTextoAsync(
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

        private bool CompararImagenesProm(
            byte[] imagenBytes,
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
                using (var imagenA = new Bitmap(
                    original,
                    referencia.Width,
                    referencia.Height))
                using (var imagenB = new Bitmap(
                    referencia.Width,
                    referencia.Height))
                {
                    using (Graphics g = Graphics.FromImage(imagenB))
                    {
                        g.DrawImage(
                            referencia,
                            0, 0,
                            referencia.Width,
                            referencia.Height);
                    }

                    double sumaDiferencias = 0;
                    int totalPixeles = referencia.Width * referencia.Height;

                    for (int y = 0; y < referencia.Height; y++)
                    {
                        for (int x = 0; x < referencia.Width; x++)
                        {
                            Color a = imagenA.GetPixel(x, y);
                            Color b = imagenB.GetPixel(x, y);

                            double diferencia =
                                (Math.Abs(a.R - b.R) +
                                 Math.Abs(a.G - b.G) +
                                 Math.Abs(a.B - b.B)) / 3.0;

                            sumaDiferencias += diferencia;
                        }
                    }

                    double diferenciaPromedio =
                        sumaDiferencias / totalPixeles;

                    double similitud =
                        1.0 - (diferenciaPromedio / 255.0);

                    System.Diagnostics.Debug.WriteLine(
                        $"Similitud: {similitud:P2}");

                    return similitud >= similitudMinima;
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
        private async Task<System.Drawing.Point?> BuscarTextoAsync(byte[] imagen,
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

                System.Drawing. Point? BuscarEnPix(Pix pix, PageSegMode modo)
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
                            return new System.Drawing.Point(
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