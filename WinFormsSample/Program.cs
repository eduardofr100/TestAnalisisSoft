using log4net;

namespace WinFormsSample
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 1. Diagnóstico interno de log4net
            log4net.Util.LogLog.InternalDebugging = true;

            // 2. Verificación física del archivo de configuración
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            if (!File.Exists(configPath))
            {
                MessageBox.Show($"Archivo de configuración no encontrado en:\n{configPath}");
                return;
            }

            // 3. Configuración explícita
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(configPath));

            // 4. Test inmediato
            var log = LogManager.GetLogger(typeof(Program));
            log.Error("PRUEBA INICIAL - ESTE MENSAJE DEBE APARECER");

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}