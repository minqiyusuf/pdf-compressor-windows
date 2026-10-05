using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PdfCompressorUninstall
{
    static class Program
    {
        static string InstallDir
        {
            get { return AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar); }
        }

        static void DeleteShortcut(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            DialogResult r = MessageBox.Show(
                "确定要卸载 PDF Compressor 吗？\r\n\r\n你的 PDF 文件和输出文件不会被删除。",
                "卸载 PDF Compressor",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (r != DialogResult.Yes) return;

            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string startMenu = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                    "PDF Compressor");

                DeleteShortcut(Path.Combine(desktop, "PDF Compressor.lnk"));
                try { if (Directory.Exists(startMenu)) Directory.Delete(startMenu, true); } catch { }

                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PDFCompressor", false);
                }
                catch { }

                string batch = Path.Combine(Path.GetTempPath(),
                    "pdf_compressor_remove_" + Guid.NewGuid().ToString("N") + ".cmd");
                string dir = InstallDir;

                File.WriteAllText(batch,
                    "@echo off\r\n" +
                    "timeout /t 2 /nobreak >nul\r\n" +
                    "rmdir /s /q \"" + dir + "\"\r\n" +
                    "del /q \"%~f0\"\r\n");

                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c \"" + batch + "\"");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process.Start(psi);

                MessageBox.Show("PDF Compressor 已卸载。", "完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("卸载过程中出现问题：\r\n" + ex.Message,
                    "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
