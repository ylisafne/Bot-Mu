namespace Kapi_Mu_Utility
{
    internal static class Program
    {
        // true = múltiples instancias
        // false = solamente una instancia
        private static bool FLG = true;

        private static Mutex mutex;
        private static EventWaitHandle eventoActivar;

        [STAThread]
        static void Main()
        {
            bool nuevaInstancia = true;

            if (!FLG)
            {
                mutex = new Mutex(
                    true,
                    @"Local\Kapi_Mu_Utility_Mutex",
                    out nuevaInstancia
                );

                if (!nuevaInstancia)
                {
                    using (EventWaitHandle evento = new EventWaitHandle(
                        false,
                        EventResetMode.AutoReset,
                        @"Local\Kapi_Mu_Utility_Activar"))
                    {
                        evento.Set();
                    }

                    mutex.Dispose();
                    return;
                }
            }

            try
            {
                ApplicationConfiguration.Initialize();

                FrmMain frm = new FrmMain();

                if (!FLG)
                {
                    EventWaitHandle eventoActivar = new EventWaitHandle(
                        false,
                        EventResetMode.AutoReset,
                        @"Local\Kapi_Mu_Utility_Activar"
                    );

                    var hiloActivacion = new Thread(() =>
                    {
                        while (true)
                        {
                            eventoActivar.WaitOne();

                            if (frm.IsDisposed)
                                break;

                            if (frm.IsHandleCreated)
                            {
                                frm.BeginInvoke(new Action(() =>
                                {
                                    if (frm.WindowState ==
                                        FormWindowState.Minimized)
                                    {
                                        frm.WindowState =
                                            FormWindowState.Normal;
                                    }

                                    frm.Show();
                                    frm.Activate();
                                    frm.BringToFront();

                                    SetForegroundWindow(frm.Handle);
                                }));
                            }
                        }

                        eventoActivar.Dispose();
                    });

                    hiloActivacion.IsBackground = true;
                    hiloActivacion.Start();
                }

                Application.Run(frm);
            }
            finally
            {
                if (!FLG && mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}