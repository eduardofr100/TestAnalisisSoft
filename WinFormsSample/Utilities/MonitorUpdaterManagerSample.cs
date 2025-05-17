using log4net;
using System.Diagnostics;

namespace WinFormsSample.Networking
{
    public class MonitorUpdaterManagerSample
    {
        #region Constantes

        private const string MonitorServiceNameKey = "monitorsk";
        
        public const string UpdaterMonitorInstallationFolder = "monSelfUpdater";

        const string MonitorUpdatesPath = @"/tmp/carpetaTest";

        public const string UpdaterMonitorFolder = "actualizaciones";
        
        public static string InstalledRollbackFilesPath = @"/tmp/carpetaTest";

        private static readonly ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Metodos

        public void UpdateMonitor(string monitorFilesLocation, string installationFolder, string version)
        {
            try
            {
                //var winServiceManager = new WindowsServiceManager();
                Log.Info("Iniciando las actualizaciones al monitor...");
                try
                {
                    Process[] processes = Process.GetProcessesByName("psample");
                    if (processes.Any())
                    {
                        Log.Info("Cerrando el monitor de actualizaciones...");
                        foreach (var proc in processes)
                        {
                            proc.Kill();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Ocurrió un error al intentar terminar el proceso del monitor de actualizaciones.", ex);
                }
                var backupPath = Path.Combine(MonitorUpdatesPath, "Backup", Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6));
                if (!Directory.Exists(backupPath))
                {
                    try
                    {
                        Directory.CreateDirectory(backupPath);
                    }
                    catch
                    {
                        backupPath = Path.Combine(Path.GetTempPath(), UpdaterMonitorFolder, "Backup", Guid.NewGuid().ToString().Replace("-", "").Substring(0, 6));
                        if (!Directory.Exists(backupPath))
                        {
                            Directory.CreateDirectory(backupPath);
                        }
                    }
                }
                var fileManager = new FileManagerSample();
                var result = fileManager.UpdateFiles(monitorFilesLocation.Trim(new char[] { '"' }), installationFolder.Trim(new char[] { '"' }), backupPath);
                bool updateError = false;
                if (!string.IsNullOrEmpty(result))
                {
                    updateError = true;
                    Log.Error(result);
                    result = null;
                }
                if (updateError)
                {
                    Log.Info("Realizando rollback de las actualizaciones al monitor...");
                    result = fileManager.UpdateFiles(backupPath, installationFolder);
                    fileManager.RemoveDirectoryContents(backupPath);
                    if (!string.IsNullOrEmpty(result))
                    {
                        //Log.Info("MonitorUpdater", result);
                    }
                    else
                    {
                        Log.Info("Terminado rollback de las actualizaciones al monitor...");
                    }
                    return;
                }
                fileManager.RemoveDirectoryContents(backupPath);
                fileManager.RemoveDirectoryContents(monitorFilesLocation.Trim(new char[] { '"' }));
                Directory.Delete(backupPath, true);
                Directory.Delete(monitorFilesLocation.Trim(new char[] { '"' }), true);
                ReleaseUpdateMonitorTask();
                Log.Info("Actualizaciones al monitor terminadas...");
            }
            catch (Exception ex)
            {
                Log.Error("Ocurrió un error durante el proceso de actualización del Monitor.", ex);
            }
        }

        private void ReleaseUpdateMonitorTask()
        {
            // elimina la tarea
            try
            {
                Log.Info("Eliminando tarea programada...");
                using (var process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks.exe",
                        Arguments = $"/Delete /TN \"{MonitorServiceNameKey}\" /F",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    };

                    process.Start();
                    process.WaitForExit(10000);

                    if (process.ExitCode == 0)
                        Log.Info("Tarea eliminada correctamente.");
                    else
                        Log.Error($"Error: {process.StandardError.ReadToEnd()}");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error al eliminar la tarea programada.", ex);
            }
        }

        #endregion
    }
}
