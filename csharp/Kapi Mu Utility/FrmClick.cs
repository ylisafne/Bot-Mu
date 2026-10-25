using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Kapi_Mu_Utility
{
    public partial class FrmClick : Form
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(
            uint dwFlags,
            uint dx,
            uint dy,
            uint dwData,
            UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        private const int VK_F8 = 0x77;

        private System.Windows.Forms.Timer timerProceso;
        private System.Windows.Forms.Timer timerF8;

        private bool activo = false;
        private bool soltando = false;
        private bool f8Anterior = false;

        private DateTime proximaLiberacion;

        public FrmClick()
        {
            InitializeComponent();

            // Timer para controlar el clic derecho.
            timerProceso = new System.Windows.Forms.Timer();
            timerProceso.Interval = 1000;
            timerProceso.Tick += TimerProceso_Tick;

            // Timer para detectar F8 globalmente.
            timerF8 = new System.Windows.Forms.Timer();
            timerF8.Interval = 50;
            timerF8.Tick += TimerF8_Tick;

            timerF8.Start();

            btnToogle.Text = "Iniciar (F8)";
        }

        private void btnToogle_Click(object sender, EventArgs e)
        {
            AlternarEstado();
        }

        private void TimerF8_Tick(object sender, EventArgs e)
        {
            bool f8Presionado =
                (GetAsyncKeyState(VK_F8) & 0x8000) != 0;

            // Detecta el inicio de una pulsación de F8.
            if (f8Presionado && !f8Anterior)
            {
                AlternarEstado();
            }

            f8Anterior = f8Presionado;
        }

        private void AlternarEstado()
        {
            if (activo)
            {
                Detener();
            }
            else
            {
                Iniciar();
            }
        }

        private void Iniciar()
        {
            if (activo)
                return;

            activo = true;
            soltando = false;

            // Primera liberación dentro de 4 minutos.
            proximaLiberacion = DateTime.Now.AddMinutes(4);

            // Presionar el botón derecho.
            mouse_event(
                MOUSEEVENTF_RIGHTDOWN,
                0, 0, 0, UIntPtr.Zero);

            timerProceso.Interval = 1000;
            timerProceso.Start();

            btnToogle.Text = "Detener (F8)";
        }

        private void TimerProceso_Tick(object sender, EventArgs e)
        {
            if (!activo)
                return;

            if (!soltando && DateTime.Now >= proximaLiberacion)
            {
                // Soltar el clic derecho.
                mouse_event(
                    MOUSEEVENTF_RIGHTUP,
                    0, 0, 0, UIntPtr.Zero);

                soltando = true;

                // Esperar 2 segundos.
                timerProceso.Stop();
                timerProceso.Interval = 2000;
                timerProceso.Start();
            }
            else if (soltando)
            {
                // Volver a presionar el clic derecho.
                mouse_event(
                    MOUSEEVENTF_RIGHTDOWN,
                    0, 0, 0, UIntPtr.Zero);

                soltando = false;

                // Contar otros 4 minutos.
                proximaLiberacion = DateTime.Now.AddMinutes(4);

                timerProceso.Stop();
                timerProceso.Interval = 1000;
                timerProceso.Start();
            }
        }

        private void Detener()
        {
            activo = false;
            soltando = false;

            timerProceso.Stop();

            // Garantizar que el botón derecho quede liberado.
            mouse_event(
                MOUSEEVENTF_RIGHTUP,
                0, 0, 0, UIntPtr.Zero);

            btnToogle.Text = "Iniciar (F8)";
        }

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            Detener();

            timerF8.Stop();

            timerProceso.Dispose();
            timerF8.Dispose();

            base.OnFormClosing(e);
        }
    }
}