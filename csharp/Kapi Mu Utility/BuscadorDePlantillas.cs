using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing; // Necesario para Bitmap, Point y Rectangle
using OpenCvSharp;    // Necesario para OpenCV
using OpenCvSharp.Extensions; // Necesario para BitmapConverter

// Alias específicos para evitar colisiones de nombres
using PointOpenCV = OpenCvSharp.Point;
using SizeOpenCV = OpenCvSharp.Size;

namespace Kapi_Mu_Utility
{
    internal class BuscadorDePlantillas
    {
        /// <summary>
        /// Busca un icono (Bitmap) dentro del screenshot completo (byte[]) con alta tolerancia a cambios de tamaño y color.
        /// </summary>
        /// <param name="bytesScreenshot">El arreglo de bytes del screenshot completo de la pantalla.</param>
        /// <param name="icono">El objeto Bitmap que contiene el icono de referencia.</param>
        /// <param name="umbralSimilitud">Porcentaje de coincidencia mínima (Se recomienda entre 0.70 y 0.85).</param>
        /// <param name="puntoParaClick">Devuelve el Point (X, Y) central del icono listo para usar con el mouse.</param>
        /// <returns>True si el icono fue encontrado superando el umbral; de lo contrario, False.</returns>
        public bool BuscaImagen(byte[] bytesScreenshot, Bitmap icono, double umbralSimilitud, out System.Drawing.Point puntoParaClick)
        {
            puntoParaClick = System.Drawing.Point.Empty;

            if (bytesScreenshot == null || bytesScreenshot.Length == 0 || icono == null)
                return false;

            // 1. Cargar las imágenes iniciales
            using (Mat escenaOriginal = Cv2.ImDecode(bytesScreenshot, ImreadModes.Color))
            using (Mat plantillaOriginal = BitmapConverter.ToMat(icono))
            {
                if (escenaOriginal.Empty() || plantillaOriginal.Empty())
                    return false;

                // 2. CONVERSIÓN A ESCALA DE GRISES (Ultra Tolerante a Ruido y Color)
                using (Mat escenaGris = new Mat())
                using (Mat plantillaGris = new Mat())
                {
                    // Convertir la escena a grises controlando si tiene 3 o 4 canales
                    if (escenaOriginal.Channels() == 4)
                        Cv2.CvtColor(escenaOriginal, escenaGris, ColorConversionCodes.BGRA2GRAY);
                    else
                        Cv2.CvtColor(escenaOriginal, escenaGris, ColorConversionCodes.BGR2GRAY);

                    // Convertir la plantilla a grises controlando el canal Alfa de transparencia
                    if (plantillaOriginal.Channels() == 4)
                        Cv2.CvtColor(plantillaOriginal, plantillaGris, ColorConversionCodes.BGRA2GRAY);
                    else
                        Cv2.CvtColor(plantillaOriginal, plantillaGris, ColorConversionCodes.BGR2GRAY);

                    double mejorMaxVal = -1;
                    PointOpenCV mejorLoc = new PointOpenCV();
                    double mejorEscala = 1.0;

                    // 3. BUCLE MULTIESCALA DE ALTA PRECISIÓN
                    // Probamos del 60% al 160% del tamaño original con saltos más pequeños (0.05) para no perder el objetivo
                    for (double escala = 0.6; escala <= 1.6; escala += 0.05)
                    {
                        int nuevoAncho = (int)(plantillaGris.Width * escala);
                        int nuevoAlto = (int)(plantillaGris.Height * escala);

                        if (nuevoAncho > escenaGris.Width || nuevoAlto > escenaGris.Height)
                            continue;

                        using (Mat plantillaEscalada = new Mat())
                        using (Mat resultado = new Mat())
                        {
                            Cv2.Resize(plantillaGris, plantillaEscalada, new SizeOpenCV(nuevoAncho, nuevoAlto));

                            Cv2.MatchTemplate(escenaGris, plantillaEscalada, resultado, TemplateMatchModes.CCoeffNormed);
                            Cv2.MinMaxLoc(resultado, out _, out double maxVal, out _, out PointOpenCV maxLoc);

                            if (maxVal > mejorMaxVal)
                            {
                                mejorMaxVal = maxVal;
                                mejorLoc = maxLoc;
                                mejorEscala = escala;
                            }
                        }
                    }

                    // Log de depuración opcional: Puedes poner un punto de interrupción aquí para ver cuánto porcentaje detecta.
                    // System.Diagnostics.Debug.WriteLine($"Máxima coincidencia encontrada: {mejorMaxVal * 100:F2}%");

                    // 4. Evaluar contra el umbral
                    if (mejorMaxVal >= umbralSimilitud)
                    {
                        int anchoFinal = (int)(plantillaGris.Width * mejorEscala);
                        int altoFinal = (int)(plantillaGris.Height * mejorEscala);

                        int centroX = mejorLoc.X + (anchoFinal / 2);
                        int centroY = mejorLoc.Y + (altoFinal / 2);

                        puntoParaClick = new System.Drawing.Point(centroX, centroY);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}