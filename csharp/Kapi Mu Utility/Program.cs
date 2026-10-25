using System;
using System.Threading;
using System.Windows.Forms;

namespace Kapi_Mu_Utility
{
    internal static class Program
    {
        private static Mutex mutex;
        private static EventWaitHandle eventoActivar;

        [STAThread]
        static void Main()
        {
            bool nuevaInstancia;

            mutex = new Mutex(
                true,
                @"Local\Kapi_Mu_Utility_Mutex",
                out nuevaInstancia
            );

            eventoActivar = new EventWaitHandle(
                false,
                EventResetMode.AutoReset,
                @"Local\Kapi_Mu_Utility_Activar"
            );

            if (!nuevaInstancia)
            {
                eventoActivar.Set();
                mutex.Dispose();
                eventoActivar.Dispose();
                return;
            }

            try
            {
                ApplicationConfiguration.Initialize();

                FrmMain frm = new FrmMain();

                var hiloActivacion = new Thread(() =>
                {
                    while (true)
                    {
                        eventoActivar.WaitOne();

                        if (frm.IsDisposed)
                            break;

                        frm.BeginInvoke(new Action(() =>
                        {
                            if (frm.WindowState == FormWindowState.Minimized)
                                frm.WindowState = FormWindowState.Normal;

                            frm.Show();
                            frm.Activate();
                            frm.BringToFront();

                            SetForegroundWindow(frm);
                        }));
                    }
                });

                hiloActivacion.IsBackground = true;
                hiloActivacion.Start();

                Application.Run(frm);
            }
            finally
            {
                mutex.ReleaseMutex();
                mutex.Dispose();
                eventoActivar.Dispose();
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private static void SetForegroundWindow(Form frm)
        {
            SetForegroundWindow(frm.Handle);
        }
    }
}