using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace Kapi_Mu_Utility
{
    public partial class MainWindows : Form
    {
        private Mucdp? mucdp;
        public MainWindows()
        {
            InitializeComponent();
        }

        // ============================================================
        // ACTUALIZAR BOTONES
        // ============================================================

        
        // ============================================================
        // WEB
        // ============================================================
        private async void btnWeb_Click(object sender, EventArgs e)
        {
            

        }

        // ============================================================
        // BOTÓN CLIENTE
        // ============================================================

        private async void btnCliente_Click(object sender, EventArgs e)
        {
            btnCliente.Enabled = false;
            btnWeb.Enabled = false;


            mucdp ??= new Mucdp();


            await mucdp.ConectarOIniciarAsync();


            Debug.WriteLine(
                $"CONECTADO = {mucdp.EstaConectado}");


            JsonDocument resultado =
                await mucdp.EvaluateAsync(
                    "document.title");


            Debug.WriteLine(
                "TITLE:");

            Debug.WriteLine(
                resultado.RootElement.ToString());

        }

        // ============================================================
        // EJEMPLO: COMPROBAR CONEXIÓN
        // ============================================================

        

        // ============================================================
        // CERRAR FORMULARIO
        // ============================================================

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            
            base.OnFormClosing(e);
        }
    }
}
