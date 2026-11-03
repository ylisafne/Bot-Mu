using System;
using System.Collections.Generic;
using System.Text;
using Tesseract;

namespace Kapi_Mu_Utility
{
    public sealed class OCRService : IDisposable
    {
        private readonly TesseractEngine motorOCR;
        private readonly SemaphoreSlim semaforo = new(1, 1);

        public OCRService(string vvPath)
        {
            motorOCR = new TesseractEngine(
                vvPath,
                "spa+eng",
                EngineMode.Default);
        }

        public async Task<T> EjecutarAsync<T>(
            Func<TesseractEngine, T> operacion,
            CancellationToken token = default)
        {
            await semaforo.WaitAsync(token);

            try
            {
                token.ThrowIfCancellationRequested();

                return await Task.Run(
                    () => operacion(motorOCR),
                    token);
            }
            finally
            {
                semaforo.Release();
            }
        }

        public void Dispose()
        {
            semaforo.Wait();

            try
            {
                motorOCR.Dispose();
            }
            finally
            {
                semaforo.Release();
                semaforo.Dispose();
            }
        }
    }
}
