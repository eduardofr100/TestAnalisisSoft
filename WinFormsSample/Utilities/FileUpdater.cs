using log4net;
using Microsoft.Web.XmlTransform;
using System.Diagnostics;

namespace WinFormsSample.Networking
{
    public class FileUpdater
    {
        private static readonly ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region Constants

        private const string DeleteCommandExtension = ".del";

        private const string AddCommandExtension = ".add";

        private const string UpdateCommandExtension = ".upd";

        private const string XdtMergeCommandExtension = ".xmrg";

        private const string ExecuteCommandExtension = ".exc";

        private const string ExecuteCommandExtensionInitial = ".eini";

        private const string ExecuteCommandExtensionEnd = ".eend";

        private const string ExecuteCommandParamsExtension = ".params";

        private const string CannotTransformXdtMessage = "No se puede realizar la transformación del archivo .";

        #endregion

        #region Methods

        private static void BackupFile(string backupDir, string targetFolder, string originalFileName, string command)
        {
            var backupFileName = originalFileName;
            var targetRelativeDirectory = string.Empty;
            if (File.Exists(originalFileName))
            {
                if (command == DeleteCommandExtension)
                {
                    command = UpdateCommandExtension;
                    targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
                }
                if (command == ExecuteCommandExtensionInitial || command == ExecuteCommandExtension || command == ExecuteCommandExtensionEnd || command == ExecuteCommandParamsExtension)
                {
                    backupFileName = Path.Combine(Path.GetDirectoryName(originalFileName), Path.GetFileNameWithoutExtension(originalFileName));
                    targetRelativeDirectory = targetFolder;
                }
            }
            else if (command == DeleteCommandExtension)
            {
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }
                File.WriteAllText(originalFileName, string.Empty);
                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else if (command == UpdateCommandExtension)
            {
                command = DeleteCommandExtension;
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }
                File.WriteAllText(originalFileName, string.Empty);
                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else
            {
                return;
            }
            var folderToBackupFile = Path.Combine(backupDir, targetRelativeDirectory);
            if (!Directory.Exists(folderToBackupFile))
            {
                Directory.CreateDirectory(folderToBackupFile);
            }
            File.Copy(originalFileName, Path.Combine(folderToBackupFile, Path.GetFileName(backupFileName)) + command, true);
        }

        private static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public string UpdateFiles(string sourceFolder, string targetFolder, string backupDir = null)
        {
            var createBackup = !string.IsNullOrEmpty(backupDir);
            if (createBackup)
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
            }
            try
            {
                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionInitial, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir, targetRelativeDirectory), ExecuteCommandExtensionInitial);
                });
                foreach (var file in Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories).Where(s => Path.GetExtension(s) != ExecuteCommandExtensionInitial && Path.GetExtension(s) != ExecuteCommandExtensionEnd).ToList())
                {
                    var fileExtension = Path.GetExtension(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    var targetFileName = Path.Combine(targetFolder, targetRelativeDirectory ?? string.Empty, fileName ?? string.Empty);
                    switch (fileExtension.ToLower())
                    {
                        case AddCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, DeleteCommandExtension);
                            }
                            CopyFile(file, targetFileName);
                            break;
                        case XdtMergeCommandExtension:
                            if (createBackup && File.Exists(targetFileName))
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }
                            MergeXDT(file, targetFileName);
                            break;
                        case UpdateCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }
                            CopyFile(file, targetFileName);
                            break;
                        case DeleteCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, AddCommandExtension);
                            }
                            RemoveFile(targetFileName);
                            break;
                        case ExecuteCommandExtension:
                            ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtension);
                            break;
                    }
                }
                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionEnd, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtensionEnd);
                });
            }
            catch (Exception ex)
            {
                if (createBackup)
                {
                    var errorMessage = ex.ToString();
                    var rollbackError = UpdateFiles(backupDir, targetFolder);
                    return errorMessage + (string.IsNullOrEmpty(rollbackError) ? string.Empty : Environment.NewLine + "Rollback Error =>" + Environment.NewLine + rollbackError);
                }
                return ex.ToString();
            }
            return string.Empty;
        }

        private void ExecuteBats(string file, string fileName, string targetRelativeDirectory, bool createBackup, string backupDir, string extension)
        {
            //ejecuta bats
            try
            {
                //string targetDirectory = Path.Combine(targetFolder, targetRelativeDirectory ?? "");
                //Directory.CreateDirectory(targetDirectory);

                if (createBackup)
                {
                    string backupPath = Path.Combine(backupDir, Path.GetFileName(file));
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Copy(file, backupPath, true);
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{file}\"",
                    //WorkingDirectory = targetDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.OutputDataReceived += (s, e) => Log.Info(e.Data);
                    process.ErrorDataReceived += (s, e) => Log.Error(e.Data);

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new Exception($"Error al ejecutar {file}. Código: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error en ExecuteBats: {ex.Message}", ex);
                throw;
            }
        }


        private void CopyFile(string sourceFile, string targetFile)
        {
            //copia archivo de un lugar a otro
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                File.Copy(sourceFile, targetFile, true);
                Log.Info($"Archivo copiado: {targetFile}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error copiando {sourceFile} a {targetFile}", ex);
                throw;
            }
        }

        private void RemoveFile(string targetFile)
        {
            //elimina archivo
            try
            {
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                    Log.Info($"Archivo eliminado: {targetFile}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error eliminando {targetFile}", ex);
                throw;
            }
        }


        private void MergeXDT(string sourceFile, string targetFile)
        {
            if (File.Exists(targetFile))
            {
                using (var target = new XmlTransformableDocument())
                {
                    target.PreserveWhitespace = true;
                    target.Load(targetFile);
                    using (var xdt = new XmlTransformation(sourceFile))
                    {
                        if (xdt.Apply(target))
                        {
                            target.Save(targetFile);
                        }
                        else
                        {
                            throw new XmlTransformationException(string.Format(CannotTransformXdtMessage, sourceFile));
                        }
                    }
                }
            }
        }

        #endregion
    }
}

