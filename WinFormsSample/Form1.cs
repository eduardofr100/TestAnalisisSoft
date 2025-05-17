using log4net;
using System.IO.Compression;
using WinFormsSample.Networking;

namespace WinFormsSample
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnArchivo_Click(object sender, EventArgs e)
        {
            MonitorUpdaterManagerSample monitorUpdaterManagerSample = new MonitorUpdaterManagerSample();

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // Configurar el di�logo
                openFileDialog.Filter = "ZIP Files (*.zip)|*.zip"; // Filtro para ZIP
                openFileDialog.Title = "Seleccionar archivo ZIP de actualizaci�n";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string zipFilePath = openFileDialog.FileName;
                    string extractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    try
                    {
                        // Extraer el ZIP a una carpeta temporal
                        ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                        // Obtener la carpeta de instalaci�n del monitor (ejemplo)
                        string installationFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            MonitorUpdaterManagerSample.UpdaterMonitorInstallationFolder
                        );

                        // Llamar a la actualizaci�n
                        monitorUpdaterManagerSample.UpdateMonitor(extractPath, installationFolder, "1.0.0");
                        MessageBox.Show("Actualizaci�n aplicada correctamente.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al leer el archivo: {ex.Message}", "Error");
                    }
                    finally
                    {
                        // Limpiar: Eliminar la carpeta temporal
                        if (Directory.Exists(extractPath))
                        {
                            Directory.Delete(extractPath, true);
                        }
                    }
                }
            }
        }
    }
}
