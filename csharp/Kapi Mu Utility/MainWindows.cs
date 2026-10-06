using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Kapi_Mu_Utility;

public partial class MainWindows : Form
{
    private MuAutomation? _automation;

    private CancellationTokenSource? _f8Cts;

    private bool _f8Anterior;

    private bool _clienteConectado;

    private bool _preparandoCliente;

    private bool _cerrando;

    public MainWindows()
    {
        InitializeComponent();

        btnToogle.Enabled = false;
        btnToogle.Text = "Iniciar";

        btnCliente.Click += btnCliente_Click;
        btnWeb.Click += btnWeb_Click;
        btnToogle.Click += btnToogle_Click;

        FormClosing += MainWindows_FormClosing;

        IniciarMonitorF8();
    }

    private AutomationSettings ObtenerConfiguracion()
    {
        var configuracion = new AutomationSettings();

        if (chk_agi.Checked)
        {
            configuracion.Comandos.Add(
                $"/agi {nudAgi.Value:0}");
        }

        if (chkCmd.Checked)
        {
            configuracion.Comandos.Add(
                $"/cmd {nudCmd.Value:0}");
        }

        if (chkStr.Checked)
        {
            configuracion.Comandos.Add(
                $"/str {nudStr.Value:0}");
        }

        if (chkEne.Checked)
        {
            configuracion.Comandos.Add(
                $"/ene {nudEne.Value:0}");
        }

        if (chkSta.Checked)
        {
            configuracion.Comandos.Add(
                $"/sta {nudSta.Value:0}");
        }

        configuracion.Otros =
            txtOtros.Text.Trim();

        configuracion.FromSeconds =
            nudFrom.Value;

        configuracion.ToSeconds =
            nudTO.Value;

        configuracion.Volver =
            chkVolver.Checked;

        configuracion.clickDerecho = chkRightClick.Checked;

        configuracion.X =
            nudX.Value;

        configuracion.Y =
            nudY.Value;

        configuracion.Mapa =
            txtMapa.Text.Trim();

        return configuracion;
    }

    private async void btnCliente_Click(
        object? sender,
        EventArgs e)
    {
        if (_preparandoCliente)
            return;

        if (_automation?.Activa == true)
            return;

        _preparandoCliente = true;

        btnCliente.Enabled = false;
        btnToogle.Enabled = false;

        _clienteConectado = false;

        try
        {
            if (_automation != null)
            {
                await _automation.DisposeAsync();

                _automation = null;
            }

            _automation =
                new MuAutomation(
                    ObtenerConfiguracion());

            _automation.Log +=
                Automation_Log;

            _automation.ConexionCambiada +=
                Automation_ConexionCambiada;

            bool conectado =
                await _automation.PrepararClienteAsync();

            if (!conectado)
            {
                _clienteConectado = false;

                btnToogle.Enabled = false;

                return;
            }

            _clienteConectado = true;

            btnToogle.Enabled = true;
        }
        catch (OperationCanceledException)
        {
            _clienteConectado = false;
            btnToogle.Enabled = false;
        }
        catch (Exception ex)
        {
            _clienteConectado = false;
            btnToogle.Enabled = false;

            MessageBox.Show(
                this,
                ex.Message,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _preparandoCliente = false;

            if (!_cerrando)
            {
                btnCliente.Enabled = true;
            }
        }
    }

    private void btnWeb_Click(
        object? sender,
        EventArgs e)
    {
        try
        {
            LanzarChrome();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void LanzarChrome()
    {
        const string chromeExe =
            @"C:\Program Files\Google\Chrome\Application\chrome.exe";

        const int debugPort = 9222;

        const string profile =
            @"C:\ChromeDebug";

        const string url =
            "https://play.mumagdalenas.com/";

        if (!File.Exists(chromeExe))
        {
            throw new FileNotFoundException(
                "No se encontró Google Chrome.",
                chromeExe);
        }

        var psi =
            new ProcessStartInfo
            {
                FileName = chromeExe,
                UseShellExecute = false,
                CreateNoWindow = true
            };

        psi.ArgumentList.Add(
            $"--remote-debugging-port={debugPort}");

        psi.ArgumentList.Add(
            "--remote-allow-origins=*");

        psi.ArgumentList.Add(
            $"--user-data-dir={profile}");

        psi.ArgumentList.Add(url);

        Process.Start(psi);
    }

    private void btnToogle_Click(
        object? sender,
        EventArgs e)
    {
        if (!_clienteConectado)
            return;

        if (_automation?.Activa == true)
        {
            Detener();
        }
        else
        {
            Iniciar();
        }
    }

    private async void Iniciar()
    {
        if (!_clienteConectado)
            return;

        if (_automation == null)
            return;

        if (_automation.Activa)
            return;

        AutomationSettings configuracion =
            ObtenerConfiguracion();

        _automation.ActualizarConfiguracion(
            configuracion);

        Debug.WriteLine(
            "========== INICIAR ==========");

        foreach (string comando
                 in configuracion.Comandos)
        {
            Debug.WriteLine(
                $"COMANDO: [{comando}]");
        }

        Debug.WriteLine(
            $"OTROS: [{configuracion.Otros}]");

        btnToogle.Text = "Detener";

        try
        {
            await _automation.StartAsync();
        }
        finally
        {
            if (!_cerrando)
            {
                btnToogle.Text = "Iniciar";
            }
        }
    }

    private void Detener()
    {
        _automation?.Stop();

        btnToogle.Text =
            "Iniciar";
    }

    private void Automation_ConexionCambiada(
        bool conectado)
    {
        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(() =>
                    Automation_ConexionCambiada(
                        conectado));
            }
            catch
            {
            }

            return;
        }

        _clienteConectado =
            conectado;

        btnToogle.Enabled =
            conectado;

        if (!conectado)
        {
            btnToogle.Text =
                "Iniciar";
        }
    }

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
            !_cerrando &&
            !cancellationToken.IsCancellationRequested)
        {
            bool presionada =
                (GetAsyncKeyState(VK_F8) &
                 0x8000) != 0;

            if (presionada &&
                !_f8Anterior)
            {
                if (_clienteConectado)
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

                try
                {
                    await Task.Delay(
                        300,
                        cancellationToken);
                }
                catch
                {
                    break;
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
            catch
            {
                break;
            }
        }
    }

    private void Automation_Log(
        string mensaje)
    {
        Debug.WriteLine(
            mensaje);
    }

    private async void MainWindows_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        if (_cerrando)
            return;

        _cerrando = true;

        try
        {
            _f8Cts?.Cancel();
        }
        catch
        {
        }

        _automation?.Stop();

        if (_automation != null)
        {
            await _automation.DisposeAsync();

            _automation = null;
        }
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(
        int vKey);

    private const int VK_F8 = 0x77;
}
