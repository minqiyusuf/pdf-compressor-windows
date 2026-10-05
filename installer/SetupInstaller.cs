using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PdfCompressorSetup
{
    public class SetupForm : Form
    {
        TextBox txtPath;
        CheckBox chkDesktop, chkLicense;
        Button btnInstall, btnBrowse;
        Label lblStatus;
        ProgressBar progress;

        public SetupForm()
        {
            Text = "PDF Compressor 1.0 安装";
            ClientSize = new Size(610, 385);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Font = new Font("Microsoft YaHei UI", 9F);

            Label title = new Label();
            title.Text = "PDF Compressor 1.0";
            title.Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold);
            title.Location = new Point(26, 22);
            title.AutoSize = true;
            Controls.Add(title);

            Label desc = new Label();
            desc.Text = "本地 PDF 压缩工具（包含 Ghostscript PDF 引擎）";
            desc.Location = new Point(29, 60);
            desc.AutoSize = true;
            Controls.Add(desc);

            Label pathLabel = new Label();
            pathLabel.Text = "安装位置：";
            pathLabel.Location = new Point(29, 108);
            pathLabel.AutoSize = true;
            Controls.Add(pathLabel);

            txtPath = new TextBox();
            txtPath.Location = new Point(105, 104);
            txtPath.Size = new Size(370, 25);
            txtPath.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "PDF Compressor");
            Controls.Add(txtPath);

            btnBrowse = new Button();
            btnBrowse.Text = "浏览";
            btnBrowse.Location = new Point(487, 102);
            btnBrowse.Size = new Size(88, 29);
            btnBrowse.Click += delegate { Browse(); };
            Controls.Add(btnBrowse);

            chkDesktop = new CheckBox();
            chkDesktop.Text = "创建桌面快捷方式";
            chkDesktop.Checked = true;
            chkDesktop.Location = new Point(105, 150);
            chkDesktop.AutoSize = true;
            Controls.Add(chkDesktop);

            Label licenseInfo = new Label();
            licenseInfo.Text =
                "本安装包包含 Ghostscript 作为独立运行组件。Ghostscript 由 Artifex Software 提供，\r\n" +
                "适用其自身开源许可。安装后可在程序目录查看 THIRD_PARTY_NOTICES.txt 和许可文件。";
            licenseInfo.Location = new Point(105, 184);
            licenseInfo.Size = new Size(455, 50);
            Controls.Add(licenseInfo);

            chkLicense = new CheckBox();
            chkLicense.Text = "我已了解上述第三方开源组件说明";
            chkLicense.Location = new Point(105, 238);
            chkLicense.AutoSize = true;
            chkLicense.CheckedChanged += delegate { btnInstall.Enabled = chkLicense.Checked; };
            Controls.Add(chkLicense);

            progress = new ProgressBar();
            progress.Location = new Point(105, 278);
            progress.Size = new Size(370, 20);
            Controls.Add(progress);

            lblStatus = new Label();
            lblStatus.Text = "准备安装";
            lblStatus.Location = new Point(105, 306);
            lblStatus.Size = new Size(370, 24);
            Controls.Add(lblStatus);

            btnInstall = new Button();
            btnInstall.Text = "安装";
            btnInstall.Enabled = false;
            btnInstall.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            btnInstall.Location = new Point(487, 272);
            btnInstall.Size = new Size(88, 38);
            btnInstall.Click += delegate { Install(); };
            Controls.Add(btnInstall);
        }

        void Browse()
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择 PDF Compressor 安装目录";
                if (Directory.Exists(txtPath.Text))
                    d.SelectedPath = txtPath.Text;
                if (d.ShowDialog() == DialogResult.OK)
                    txtPath.Text = Path.Combine(d.SelectedPath, "PDF Compressor");
            }
        }

        void SetBusy(bool busy)
        {
            btnInstall.Enabled = !busy && chkLicense.Checked;
            btnBrowse.Enabled = !busy;
            txtPath.Enabled = !busy;
            chkDesktop.Enabled = !busy;
            chkLicense.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            Application.DoEvents();
        }

        static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
        {
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object shortcut = shellType.InvokeMember("CreateShortcut",
                BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });

            Type shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }

        void Install()
        {
            string installDir = txtPath.Text.Trim();
            if (String.IsNullOrEmpty(installDir))
            {
                MessageBox.Show("请选择安装目录。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetBusy(true);
            string tempDir = Path.Combine(Path.GetTempPath(), "PDFCompressorSetup_" + Guid.NewGuid().ToString("N"));
            string zipPath = Path.Combine(tempDir, "payload.zip");

            try
            {
                Directory.CreateDirectory(tempDir);

                lblStatus.Text = "正在读取安装文件…";
                progress.Value = 10;
                Application.DoEvents();

                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream input = asm.GetManifestResourceStream("Payload.zip"))
                {
                    if (input == null)
                        throw new Exception("安装包缺少内置 Payload.zip。");

                    using (FileStream output = File.Create(zipPath))
                        input.CopyTo(output);
                }

                lblStatus.Text = "正在解压组件…";
                progress.Value = 30;
                Application.DoEvents();

                string extractDir = Path.Combine(tempDir, "payload");
                ZipFile.ExtractToDirectory(zipPath, extractDir);

                lblStatus.Text = "正在安装 PDF Compressor…";
                progress.Value = 55;
                Application.DoEvents();

                Directory.CreateDirectory(installDir);

                foreach (string dir in Directory.GetDirectories(extractDir, "*", SearchOption.AllDirectories))
                {
                    string rel = dir.Substring(extractDir.Length).TrimStart(Path.DirectorySeparatorChar);
                    Directory.CreateDirectory(Path.Combine(installDir, rel));
                }

                foreach (string file in Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories))
                {
                    string rel = file.Substring(extractDir.Length).TrimStart(Path.DirectorySeparatorChar);
                    string dest = Path.Combine(installDir, rel);
                    string parent = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                    File.Copy(file, dest, true);
                }

                string appExe = Path.Combine(installDir, "PDF_Compressor.exe");
                string uninstallExe = Path.Combine(installDir, "Uninstall.exe");

                if (!File.Exists(appExe))
                    throw new Exception("安装后未找到 PDF_Compressor.exe。");
                if (!File.Exists(Path.Combine(installDir, "Ghostscript", "bin", "gswin64c.exe")))
                    throw new Exception("安装后未找到内置 Ghostscript 引擎。");

                lblStatus.Text = "正在创建快捷方式…";
                progress.Value = 78;
                Application.DoEvents();

                string startMenuDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                    "PDF Compressor");
                Directory.CreateDirectory(startMenuDir);

                CreateShortcut(
                    Path.Combine(startMenuDir, "PDF Compressor.lnk"),
                    appExe, installDir, "PDF Compressor");

                if (File.Exists(uninstallExe))
                {
                    CreateShortcut(
                        Path.Combine(startMenuDir, "卸载 PDF Compressor.lnk"),
                        uninstallExe, installDir, "卸载 PDF Compressor");
                }

                if (chkDesktop.Checked)
                {
                    CreateShortcut(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                        "PDF Compressor.lnk"),
                        appExe, installDir, "PDF Compressor");
                }

                lblStatus.Text = "正在注册卸载信息…";
                progress.Value = 90;
                Application.DoEvents();

                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PDFCompressor"))
                {
                    key.SetValue("DisplayName", "PDF Compressor");
                    key.SetValue("DisplayVersion", "1.0.0");
                    key.SetValue("Publisher", "PDF Compressor");
                    key.SetValue("InstallLocation", installDir);
                    key.SetValue("DisplayIcon", appExe);
                    key.SetValue("UninstallString", "\"" + uninstallExe + "\"");
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                }

                progress.Value = 100;
                lblStatus.Text = "安装完成";

                DialogResult r = MessageBox.Show(
                    "PDF Compressor 已安装完成。\r\n\r\n是否立即启动？",
                    "安装完成",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (r == DialogResult.Yes)
                    Process.Start(appExe);

                Close();
            }
            catch (Exception ex)
            {
                progress.Value = 0;
                lblStatus.Text = "安装失败";
                MessageBox.Show(
                    "安装过程中出现错误：\r\n\r\n" + ex.Message,
                    "安装失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                SetBusy(false);
            }
            finally
            {
                try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); } catch { }
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm());
        }
    }
}
