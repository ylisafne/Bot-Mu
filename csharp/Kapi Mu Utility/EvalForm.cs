using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace Kapi_Mu_Utility
{
    public partial class EvalForm : Form
    {
        private static readonly string keyString = "kapisystem.com@";
        private static readonly string path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "sys_kpi_win_ldr.dat"
        );

        public EvalForm()
        {
            InitializeComponent();
        }

        private void btn_aceptar_Click(object sender, EventArgs e)
        {
            GuardaLicencia();
            IsvalidKey();
        }

        private void btn_cancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void IsvalidKey()
        {
            bool valid = false;
            try
            {
                valid = ValidarKeyTxt();
            }
            catch (Exception ex)
            {
                valid = false;
                txt_key.Text = ex.Message;
            }
            if (valid)
            {
                FrmMain.PassKey = true;
                this.Close();
                //this.Dispose();
            }
            else
            {
                MessageBox.Show("Key no válido.");
            }
        }
        private bool ValidarKeyTxt()
        {
            if (System.IO.File.Exists(path))
            {
                string contenido = System.IO.File.ReadAllText(path).Trim();
                return ComparaKey(contenido);
            }

            return false;
        }
        private bool ComparaKey(string key)
        {
            string licenciaLocal = GenerarLicencia();

            return licenciaLocal == key.Trim();
        }
        public string GenerarLicencia()
        {
            string mb = GetMotherboardId();
            string bios = GetBiosSerial();
            string machineGuid = GetMachineGuid();

            string raw = $"{mb}-{bios}-{machineGuid}-{keyString}";

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return Convert.ToBase64String(hash);
            }
        }
        private void GuardaLicencia()
        {
            try
            {
                // 1. Asegurar que el archivo NO tenga atributos bloqueantes si existe
                if (File.Exists(path))
                {
                    File.SetAttributes(path, FileAttributes.Normal);
                }

                // 2. Escribir el archivo
                File.WriteAllText(path, txt_key.Text);

                // 3. Aplicar atributos oculto + sistema después de escribir
                File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.System);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void EvalForm_Load(object sender, EventArgs e)
        {
            // carga los valores del equipo
            string mb = GetMotherboardId();
            string bios = GetBiosSerial();
            string machineGuid = GetMachineGuid();

            string raw = $"{mb}-{bios}-{machineGuid}-";

            txt_info.Text = raw;
            load_key();
            IsvalidKey();
        }
        private void load_key()
        {
            if (File.Exists(path))
            {
                string key = File.ReadLines(path).First();
                txt_key.Text = key;
            }
        }
        private string GetMachineGuid()
        {
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(
                    RegistryHive.LocalMachine,
                    RegistryView.Registry64);

                using RegistryKey? key = baseKey.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography");

                object? value = key?.GetValue("MachineGuid");

                if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                    return value.ToString()!;

                return "UNKNOWN";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error al obtener MachineGuid: {ex}");

                return "UNKNOWN";
            }
        }
        private string GetBiosSerial()
        {
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");

                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString()?.Trim() ?? "UNKNOWN";
                }
            }
            catch { }

            return "UNKNOWN";
        }
        private string GetMotherboardId()
        {
            try
            {
                ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");

                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["SerialNumber"]?.ToString()?.Trim() ?? "UNKNOWN";
                }
            }
            catch { }

            return "UNKNOWN";
        }
    }
}
