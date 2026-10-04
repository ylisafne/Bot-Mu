using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Kapi_Mu_Utility
{
    public partial class MainWindows : Form
    {
        private MuAutomation? _automation;
        private CancellationTokenSource? _f8Cts;
        private bool _f8Anterior;
        private bool _cerrando;
        public MainWindows()
        {
            InitializeComponent();
            FormClosing += MainWindows_FormClosing;
            IniciarMonitorF8();
        }
        private AutomationSettings ObtenerConfiguracion()
        {
            var comandos = new List<string>();

            if (chk_agi.Checked)
            {
                comandos.Add(
                    $"/agi {nudAgi.Value:0}");
            }

            if (chkCmd.Checked)
            {
                comandos.Add(
                    $"/cmd {nudCmd.Value:0}");
            }

            if (chkStr.Checked)
            {
                comandos.Add(
                    $"/str {nudStr.Value:0}");
            }

            if (chkEne.Checked)
            {
                comandos.Add(
                    $"/ene {nudEne.Value:0}");
            }

            if (chkSta.Checked)
            {
                comandos.Add(
                    $"/sta {nudSta.Value:0}");
            }

            return new AutomationSettings
            {
                Comandos = comandos,

                Otros = txtOtros.Text.Trim(),

                FromSeconds = nudFrom.Value,

                ToSeconds = nudTO.Value,

                Volver =
                    chkVolver.Checked,

                X =
                    nudX.Value,

                Y =
                    nudY.Value,

                Mapa =
                    txtMapa.Text.Trim()
            };
        }

        // =========================================================
        // INICIAR
        // =========================================================

        private async void Iniciar()
        {
            if (_automation?.Activa == true)
                return;

            AutomationSettings settings =
                ObtenerConfiguracion();

            _automation =
                new MuAutomation(settings);

            _automation.Log += Automation_Log;

            btnToogle.Text =
                "Detener";

            btnToogle.Enabled =
                true;

            try
            {
                await _automation.StartAsync();
            }
            finally
            {
                if (!_cerrando)
                {
                    btnToogle.Text =
                        "Iniciar";
                }
            }
        }

        // =========================================================
        // DETENER
        // =========================================================

        private void Detener()
        {
            _automation?.Stop();

            btnToogle.Text =
                "Iniciar";
        }


        // =========================================================
        // BOTÓN TOGGLE
        // =========================================================

        private void btnToogle_Click(
            object? sender,
            EventArgs e)
        {
            if (_automation?.Activa == true)
            {
                Detener();
            }
            else
            {
                Iniciar();
            }
        }

        // =========================================================
        // LOG
        // =========================================================

        private void Automation_Log(
            string mensaje)
        {
            // Aquí no tenemos un TextBox de log todavía.
            // Por ahora usamos Debug.

            System.Diagnostics.Debug.WriteLine(
                mensaje);
        }

        private void btnWeb_Click(object sender, EventArgs e)
        {
            try
            {
                MuAutomation.LanzarChrome();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnCliente_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
            "La implementación del cliente queda pendiente " +
            "hasta indicar la ruta del ejecutable.",
            "Cliente MU",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        }


        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(
        int vKey);

        private const int VK_F8 = 0x77;

        private void IniciarMonitorF8()
        {
            _f8Cts =
                new CancellationTokenSource();

            _ = MonitorF8Async(
                _f8Cts.Token);
        }

        private async Task MonitorF8Async(
        CancellationToken cancellationToken)
        {
            while (
                !cancellationToken.IsCancellationRequested &&
                !_cerrando)
            {
                bool presionada =
                    (GetAsyncKeyState(VK_F8) & 0x8000) != 0;

                if (presionada && !_f8Anterior)
                {
                    if (_automation?.Activa == true)
                    {
                        Detener();
                    }
                    else
                    {
                        Iniciar();
                    }
                }

                _f8Anterior =
                    presionada;

                try
                {
                    await Task.Delay(
                        10,
                        cancellationToken);
                }
                catch (
                    OperationCanceledException)
                {
                    break;
                }
            }
        }

        // =========================================================
        // CERRAR FORMULARIO
        // =========================================================

        private async void MainWindows_FormClosing(
            object? sender,
            FormClosingEventArgs e)
        {
            _cerrando = true;

            _f8Cts?.Cancel();

            _automation?.Stop();

            if (_automation != null)
            {
                await _automation.DisposeAsync();

                _automation = null;
            }
        }

        
    }
}
