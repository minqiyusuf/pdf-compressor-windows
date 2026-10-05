using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PdfCompressorRelease
{
    public class CompressParams
    {
        public int Dpi;
        public int Quality;
        public int MonoDpi;
    }

    public class Candidate
    {
        public double T;
        public string Path;
        public long Size;
        public int Dpi;
        public int Quality;
        public int MonoDpi;
    }

    public class TaskItem
    {
        public string InputPath;
        public double TargetMB;
        public string Mode;
        public string Strategy;
        public string OutputFormat;
        public string OutputPath;
        public string Status;
        public long InputBytes;
        public long OutputBytes;
    }

    public class MainForm : Form
    {
        const string PreferredGsRoot = @"D:\Program Files\gs";

        TextBox txtOutputFolder;
        string gsExe = null;
        NumericUpDown numTarget;
        ComboBox cmbMode, cmbStrategy, cmbFormat;
        DataGridView grid;
        ProgressBar progress;
        Label lblStatus;

        Button btnAdd, btnRemove, btnClear, btnStart, btnStop, btnOpenFolder, btnOutputBrowse, btnAbout;

        readonly List<TaskItem> tasks = new List<TaskItem>();
        bool running = false;
        bool stopAfterCurrent = false;

        public MainForm()
        {
            Text = "PDF Compressor 1.0";
            ClientSize = new Size(1035, 660);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1050, 700);
            Font = new Font("Microsoft YaHei UI", 9F);

            BuildUi();
            LoadSettings();
            DetectGhostscript();
        }

        void BuildUi()
        {
            Label title = new Label();
            title.Text = "PDF Compressor";
            title.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
            title.Location = new Point(22, 16);
            title.AutoSize = true;
            Controls.Add(title);

            Label sub = new Label();
            sub.Text = "完全本地处理 · 批量队列 · 目标大小控制 · 质量优先 / 大小优先";
            sub.Location = new Point(25, 51);
            sub.AutoSize = true;
            Controls.Add(sub);

            Label lblEngine = new Label();
            lblEngine.Text = "PDF 引擎：正在检测…";
            lblEngine.Name = "lblEngine";
            lblEngine.Location = new Point(24, 91);
            lblEngine.Size = new Size(700, 24);
            Controls.Add(lblEngine);

            btnAbout = AddButton("关于 / 开源组件", 830, 82, 165, 29);
            btnAbout.Click += delegate { ShowAbout(); };

            AddLabel("输出文件夹：", 24, 128);
            txtOutputFolder = AddTextBox(115, 124, 760);
            txtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            btnOutputBrowse = AddButton("选择文件夹", 885, 122, 110, 29);
            btnOutputBrowse.Click += delegate { BrowseOutputFolder(); };

            GroupBox settings = new GroupBox();
            settings.Text = "新任务设置（添加文件时写入任务列表）";
            settings.Location = new Point(24, 170);
            settings.Size = new Size(971, 115);
            Controls.Add(settings);

            Label lblTarget = new Label();
            lblTarget.Text = "目标大小：";
            lblTarget.Location = new Point(18, 35);
            lblTarget.AutoSize = true;
            settings.Controls.Add(lblTarget);

            numTarget = new NumericUpDown();
            numTarget.Location = new Point(92, 31);
            numTarget.Size = new Size(95, 25);
            numTarget.DecimalPlaces = 1;
            numTarget.Minimum = 0.5M;
            numTarget.Maximum = 10000M;
            numTarget.Value = 10M;
            numTarget.Increment = 0.5M;
            settings.Controls.Add(numTarget);

            Label mb = new Label();
            mb.Text = "MB";
            mb.Location = new Point(192, 35);
            mb.AutoSize = true;
            settings.Controls.Add(mb);

            Label lblMode = new Label();
            lblMode.Text = "压缩模式：";
            lblMode.Location = new Point(250, 35);
            lblMode.AutoSize = true;
            settings.Controls.Add(lblMode);

            cmbMode = new ComboBox();
            cmbMode.Location = new Point(324, 31);
            cmbMode.Size = new Size(165, 25);
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.Items.Add("自动最佳画质");
            cmbMode.Items.Add("古籍 / 扫描件");
            cmbMode.Items.Add("普通文档");
            cmbMode.Items.Add("极限压缩");
            cmbMode.SelectedIndex = 0;
            settings.Controls.Add(cmbMode);

            Label lblStrategy = new Label();
            lblStrategy.Text = "压缩策略：";
            lblStrategy.Location = new Point(510, 35);
            lblStrategy.AutoSize = true;
            settings.Controls.Add(lblStrategy);

            cmbStrategy = new ComboBox();
            cmbStrategy.Location = new Point(584, 31);
            cmbStrategy.Size = new Size(145, 25);
            cmbStrategy.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStrategy.Items.Add("质量优先");
            cmbStrategy.Items.Add("大小优先");
            cmbStrategy.SelectedIndex = 0;
            settings.Controls.Add(cmbStrategy);

            Label lblFormat = new Label();
            lblFormat.Text = "输出格式：";
            lblFormat.Location = new Point(748, 35);
            lblFormat.AutoSize = true;
            settings.Controls.Add(lblFormat);

            cmbFormat = new ComboBox();
            cmbFormat.Location = new Point(822, 31);
            cmbFormat.Size = new Size(125, 25);
            cmbFormat.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbFormat.Items.Add("PDF");
            cmbFormat.Items.Add("JPEG 图片");
            cmbFormat.Items.Add("PNG 图片");
            cmbFormat.SelectedIndex = 0;
            cmbFormat.SelectedIndexChanged += delegate
            {
                bool isPdf = cmbFormat.SelectedItem != null && cmbFormat.SelectedItem.ToString() == "PDF";
                numTarget.Enabled = isPdf && !running;
                cmbStrategy.Enabled = isPdf && !running;
            };
            settings.Controls.Add(cmbFormat);

            Label tip = new Label();
            tip.Text = "质量优先（默认）：古籍/扫描件保护到约 180dpi；仍超目标时停止，不继续牺牲清晰度。大小优先：必要时继续降 DPI 以追求目标 MB。";
            tip.Location = new Point(18, 65);
            tip.Size = new Size(920, 38);
            settings.Controls.Add(tip);

            btnAdd = AddButton("＋ 添加 PDF", 24, 301, 135, 34);
            btnAdd.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            btnAdd.Click += delegate { AddFiles(); };

            btnRemove = AddButton("删除选中", 168, 301, 110, 34);
            btnRemove.Click += delegate { RemoveSelected(); };

            btnClear = AddButton("清空列表", 287, 301, 110, 34);
            btnClear.Click += delegate { ClearTasks(); };

            btnStart = AddButton("▶ 开始全部任务", 672, 301, 145, 34);
            btnStart.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            btnStart.Click += delegate { StartQueue(); };

            btnStop = AddButton("当前完成后停止", 826, 301, 135, 34);
            btnStop.Enabled = false;
            btnStop.Click += delegate
            {
                stopAfterCurrent = true;
                lblStatus.Text = "状态：已请求停止，当前任务完成后暂停队列。";
            };

            btnOpenFolder = AddButton("打开输出文件夹", 970 - 145, 608, 145, 32);
            btnOpenFolder.Click += delegate { OpenOutputFolder(); };

            grid = new DataGridView();
            grid.Location = new Point(24, 347);
            grid.Size = new Size(971, 245);
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.BackgroundColor = SystemColors.Window;
            grid.Columns.Add("No", "#");
            grid.Columns.Add("File", "文件名");
            grid.Columns.Add("InputSize", "原大小");
            grid.Columns.Add("Target", "目标");
            grid.Columns.Add("Mode", "模式");
            grid.Columns.Add("Strategy", "策略");
            grid.Columns.Add("Format", "输出格式");
            grid.Columns.Add("Status", "状态");
            grid.Columns.Add("Result", "结果");

            grid.Columns["No"].Width = 42;
            grid.Columns["File"].Width = 220;
            grid.Columns["InputSize"].Width = 82;
            grid.Columns["Target"].Width = 80;
            grid.Columns["Mode"].Width = 115;
            grid.Columns["Strategy"].Width = 85;
            grid.Columns["Format"].Width = 82;
            grid.Columns["Status"].Width = 175;
            grid.Columns["Result"].Width = 82;
            Controls.Add(grid);

            progress = new ProgressBar();
            progress.Location = new Point(24, 610);
            progress.Size = new Size(600, 20);
            progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(progress);

            lblStatus = new Label();
            lblStatus.Text = "状态：等待添加任务";
            lblStatus.Location = new Point(24, 635);
            lblStatus.Size = new Size(780, 24);
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(lblStatus);

            AllowDrop = true;
            grid.AllowDrop = true;
            DragEnter += new DragEventHandler(OnDragEnter);
            DragDrop += new DragEventHandler(OnDragDrop);
            grid.DragEnter += new DragEventHandler(OnDragEnter);
            grid.DragDrop += new DragEventHandler(OnDragDrop);

            FormClosing += delegate { SaveSettings(); };
        }

        Label AddLabel(string text, int x, int y)
        {
            Label l = new Label();
            l.Text = text;
            l.Location = new Point(x, y);
            l.AutoSize = true;
            Controls.Add(l);
            return l;
        }

        TextBox AddTextBox(int x, int y, int w)
        {
            TextBox t = new TextBox();
            t.Location = new Point(x, y);
            t.Size = new Size(w, 25);
            Controls.Add(t);
            return t;
        }

        Button AddButton(string text, int x, int y, int w, int h)
        {
            Button b = new Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            Controls.Add(b);
            return b;
        }

        void DetectGhostscript()
        {
            gsExe = FindGhostscript();
            Label lbl = Controls.Find("lblEngine", true).FirstOrDefault() as Label;

            if (!String.IsNullOrEmpty(gsExe))
            {
                bool bundled = gsExe.StartsWith(AppDomain.CurrentDomain.BaseDirectory, StringComparison.OrdinalIgnoreCase);
                if (lbl != null)
                    lbl.Text = bundled
                        ? "PDF 引擎：内置 Ghostscript ✓"
                        : "PDF 引擎：Ghostscript ✓";
                lblStatus.Text = "状态：PDF 引擎已就绪";
            }
            else
            {
                if (lbl != null)
                    lbl.Text = "PDF 引擎：未找到 Ghostscript";
                lblStatus.Text = "状态：PDF 引擎不可用，请重新安装 PDF Compressor";
            }
        }

        string FindGhostscript()
        {
            string bundled = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ghostscript", "bin", "gswin64c.exe");
            if (File.Exists(bundled))
                return bundled;

            string[] roots = new string[]
            {
                PreferredGsRoot,
                @"C:\Program Files\gs",
                @"C:\Program Files (x86)\gs"
            };

            List<string> found = new List<string>();
            foreach (string root in roots)
            {
                try
                {
                    if (!Directory.Exists(root)) continue;
                    found.AddRange(Directory.GetFiles(root, "gswin64c.exe", SearchOption.AllDirectories));
                }
                catch { }
            }

            if (found.Count == 0) return null;
            return found.OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase).First();
        }

        void ShowAbout()
        {
            MessageBox.Show(
                "PDF Compressor 1.0\r\n\r\n" +
                "• 完全本地处理，不上传 PDF\r\n" +
                "• 支持批量队列、目标大小、质量优先 / 大小优先\r\n" +
                "• PDF 处理引擎：Ghostscript（Artifex Software）\r\n" +
                "• Ghostscript 随安装包作为独立运行组件提供，适用其自身开源许可。\r\n\r\n" +
                "详细许可与源码信息请查看安装目录中的 THIRD_PARTY_NOTICES.txt。",
                "关于 PDF Compressor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void BrowseOutputFolder()
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = "选择压缩结果保存位置";
                if (Directory.Exists(txtOutputFolder.Text))
                    d.SelectedPath = txtOutputFolder.Text;

                if (d.ShowDialog() == DialogResult.OK)
                    txtOutputFolder.Text = d.SelectedPath;
            }
        }

        void AddFiles()
        {
            if (running) return;

            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = "添加一个或多个 PDF";
                d.Filter = "PDF 文件|*.pdf";
                d.Multiselect = true;

                if (d.ShowDialog() != DialogResult.OK) return;

                int added = 0;
                foreach (string file in d.FileNames)
                    if (AddTaskFromFile(file)) added++;

                RefreshGrid();
                lblStatus.Text = "状态：已添加 " + added + " 个任务，共 " + tasks.Count + " 个。";
            }
        }

        bool AddTaskFromFile(string file)
        {
            if (!File.Exists(file) ||
                !String.Equals(Path.GetExtension(file), ".pdf", StringComparison.OrdinalIgnoreCase))
                return false;

            string outputRoot = txtOutputFolder.Text.Trim();
            if (String.IsNullOrEmpty(outputRoot))
            {
                outputRoot = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                txtOutputFolder.Text = outputRoot;
            }

            string format = cmbFormat.SelectedItem.ToString();
            string mode = cmbMode.SelectedItem.ToString();
            string strategy = cmbStrategy.SelectedItem.ToString();
            double target = (double)numTarget.Value;

            FileInfo fi = new FileInfo(file);
            TaskItem t = new TaskItem();
            t.InputPath = file;
            t.InputBytes = fi.Length;
            t.TargetMB = target;
            t.Mode = mode;
            t.Strategy = strategy;
            t.OutputFormat = format;
            t.Status = "等待";

            if (format == "PDF")
            {
                string tag = target.ToString("0.#", CultureInfo.InvariantCulture).Replace(".", "_");
                t.OutputPath = MakeUniqueFilePath(
                    Path.Combine(outputRoot, Path.GetFileNameWithoutExtension(file) + "_compressed_" + tag + "MB.pdf"));
            }
            else
            {
                string suffix = format.StartsWith("JPEG") ? "_JPEG" : "_PNG";
                t.OutputPath = MakeUniqueDirectoryPath(
                    Path.Combine(outputRoot, Path.GetFileNameWithoutExtension(file) + suffix));
            }

            tasks.Add(t);
            return true;
        }

        void OnDragEnter(object sender, DragEventArgs e)
        {
            if (!running && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        void OnDragDrop(object sender, DragEventArgs e)
        {
            if (running) return;
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null) return;

            int added = 0;
            foreach (string f in files)
                if (AddTaskFromFile(f)) added++;

            RefreshGrid();
            lblStatus.Text = "状态：拖入 " + added + " 个 PDF，共 " + tasks.Count + " 个任务。";
        }

        string SettingsPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PDFCompressor");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.ini");
        }

        void LoadSettings()
        {
            try
            {
                string path = SettingsPath();
                if (!File.Exists(path)) return;

                foreach (string line in File.ReadAllLines(path))
                {
                    int pos = line.IndexOf('=');
                    if (pos <= 0) continue;
                    string key = line.Substring(0, pos);
                    string value = line.Substring(pos + 1);

                    if (key == "OutputFolder" && Directory.Exists(value))
                        txtOutputFolder.Text = value;
                    else if (key == "TargetMB")
                    {
                        decimal v;
                        if (Decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                        {
                            if (v >= numTarget.Minimum && v <= numTarget.Maximum)
                                numTarget.Value = v;
                        }
                    }
                    else if (key == "Mode" && cmbMode.Items.Contains(value))
                        cmbMode.SelectedItem = value;
                    else if (key == "Strategy" && cmbStrategy.Items.Contains(value))
                        cmbStrategy.SelectedItem = value;
                    else if (key == "Format" && cmbFormat.Items.Contains(value))
                        cmbFormat.SelectedItem = value;
                }
            }
            catch { }
        }

        void SaveSettings()
        {
            try
            {
                string[] lines = new string[]
                {
                    "OutputFolder=" + txtOutputFolder.Text,
                    "TargetMB=" + numTarget.Value.ToString(CultureInfo.InvariantCulture),
                    "Mode=" + cmbMode.SelectedItem,
                    "Strategy=" + cmbStrategy.SelectedItem,
                    "Format=" + cmbFormat.SelectedItem
                };
                File.WriteAllLines(SettingsPath(), lines);
            }
            catch { }
        }

        string MakeUniqueFilePath(string path)
        {
            if (!File.Exists(path)) return path;
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int i = 2;
            while (true)
            {
                string p = Path.Combine(dir, name + "_" + i + ext);
                if (!File.Exists(p)) return p;
                i++;
            }
        }

        string MakeUniqueDirectoryPath(string path)
        {
            if (!Directory.Exists(path)) return path;
            int i = 2;
            while (true)
            {
                string p = path + "_" + i;
                if (!Directory.Exists(p)) return p;
                i++;
            }
        }

        void RemoveSelected()
        {
            if (running || grid.SelectedRows.Count == 0) return;

            List<int> indexes = new List<int>();
            foreach (DataGridViewRow row in grid.SelectedRows)
                indexes.Add(row.Index);

            indexes.Sort();
            indexes.Reverse();

            foreach (int i in indexes)
                if (i >= 0 && i < tasks.Count)
                    tasks.RemoveAt(i);

            RefreshGrid();
        }

        void ClearTasks()
        {
            if (running) return;
            tasks.Clear();
            RefreshGrid();
            progress.Value = 0;
            lblStatus.Text = "状态：任务列表已清空";
        }

        void RefreshGrid()
        {
            grid.Rows.Clear();
            for (int i = 0; i < tasks.Count; i++)
            {
                TaskItem t = tasks[i];
                string target = t.OutputFormat == "PDF"
                    ? t.TargetMB.ToString("0.#") + " MB"
                    : "—";
                string result = t.OutputBytes > 0 ? FormatMB(t.OutputBytes) : "—";

                int idx = grid.Rows.Add(
                    i + 1,
                    Path.GetFileName(t.InputPath),
                    FormatMB(t.InputBytes),
                    target,
                    t.Mode,
                    t.Strategy,
                    t.OutputFormat,
                    t.Status,
                    result
                );

                if (t.Status.StartsWith("完成"))
                    grid.Rows[idx].DefaultCellStyle.BackColor = Color.Honeydew;
                else if (t.Status.StartsWith("失败"))
                    grid.Rows[idx].DefaultCellStyle.BackColor = Color.MistyRose;
                else if (t.Status.StartsWith("处理中"))
                    grid.Rows[idx].DefaultCellStyle.BackColor = Color.LemonChiffon;
            }
        }

        void UpdateRow(int index)
        {
            if (index < 0 || index >= tasks.Count || index >= grid.Rows.Count) return;
            TaskItem t = tasks[index];

            grid.Rows[index].Cells["Status"].Value = t.Status;
            grid.Rows[index].Cells["Result"].Value = t.OutputBytes > 0 ? FormatMB(t.OutputBytes) : "—";

            if (t.Status.StartsWith("完成"))
                grid.Rows[index].DefaultCellStyle.BackColor = Color.Honeydew;
            else if (t.Status.StartsWith("失败"))
                grid.Rows[index].DefaultCellStyle.BackColor = Color.MistyRose;
            else if (t.Status.StartsWith("处理中"))
                grid.Rows[index].DefaultCellStyle.BackColor = Color.LemonChiffon;

            grid.FirstDisplayedScrollingRowIndex = Math.Max(0, index - 2);
            Application.DoEvents();
        }

        void SetRunningUi(bool isRunning)
        {
            running = isRunning;
            btnAdd.Enabled = !isRunning;
            btnRemove.Enabled = !isRunning;
            btnClear.Enabled = !isRunning;
            btnStart.Enabled = !isRunning;
            btnOutputBrowse.Enabled = !isRunning;
            txtOutputFolder.Enabled = !isRunning;
            numTarget.Enabled = !isRunning && cmbFormat.SelectedItem.ToString() == "PDF";
            cmbMode.Enabled = !isRunning;
            cmbStrategy.Enabled = !isRunning && cmbFormat.SelectedItem.ToString() == "PDF";
            cmbFormat.Enabled = !isRunning;
            btnStop.Enabled = isRunning;
            Cursor = isRunning ? Cursors.WaitCursor : Cursors.Default;
            Application.DoEvents();
        }

        void StartQueue()
        {
            if (running) return;
            if (tasks.Count == 0)
            {
                MessageBox.Show("任务列表还是空的，请先添加 PDF。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string gs = gsExe;
            if (!File.Exists(gs))
            {
                MessageBox.Show("PDF 引擎不可用，请重新安装 PDF Compressor。", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            stopAfterCurrent = false;
            SetRunningUi(true);
            progress.Value = 0;

            int processed = 0;
            int success = 0;
            int failed = 0;

            try
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    TaskItem task = tasks[i];

                    // Skip tasks already completed in this session if user presses Start again.
                    if (task.Status.StartsWith("完成"))
                    {
                        processed++;
                        success++;
                        continue;
                    }

                    task.Status = "处理中…";
                    task.OutputBytes = 0;
                    UpdateRow(i);

                    int overallStart = (int)Math.Round((processed * 100.0) / tasks.Count);
                    progress.Value = Math.Max(0, Math.Min(100, overallStart));
                    lblStatus.Text = "状态：正在处理 " + (i + 1) + "/" + tasks.Count + "  " + Path.GetFileName(task.InputPath);
                    Application.DoEvents();

                    try
                    {
                        if (!File.Exists(task.InputPath))
                            throw new Exception("源文件不存在");

                        if (task.OutputFormat == "PDF")
                        {
                            CompressPdfTask(gs, task, i, processed, tasks.Count);
                        }
                        else if (task.OutputFormat.StartsWith("JPEG"))
                        {
                            ExportImagesTask(gs, task, "jpeg", i, processed, tasks.Count);
                        }
                        else
                        {
                            ExportImagesTask(gs, task, "png16m", i, processed, tasks.Count);
                        }

                        if (!task.Status.StartsWith("完成"))
                            task.Status = "完成";
                        success++;
                    }
                    catch (Exception ex)
                    {
                        task.Status = "失败：" + ShortMessage(ex.Message);
                        failed++;
                    }

                    processed++;
                    UpdateRow(i);

                    int overall = (int)Math.Round((processed * 100.0) / tasks.Count);
                    progress.Value = Math.Max(0, Math.Min(100, overall));

                    if (stopAfterCurrent)
                    {
                        lblStatus.Text = "状态：队列已暂停。已处理 " + processed + "/" + tasks.Count + "。";
                        break;
                    }
                }
            }
            finally
            {
                SetRunningUi(false);
            }

            SaveSettings();

            if (!stopAfterCurrent)
            {
                progress.Value = 100;
                lblStatus.Text = "状态：队列完成。成功 " + success + "，失败 " + failed + "。";
                MessageBox.Show(
                    "批量任务已经处理完成。\r\n\r\n成功：" + success +
                    "\r\n失败：" + failed +
                    "\r\n\r\n结果保存在：\r\n" + txtOutputFolder.Text,
                    "任务完成",
                    MessageBoxButtons.OK,
                    failed == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }

        string ShortMessage(string s)
        {
            if (String.IsNullOrEmpty(s)) return "未知错误";
            s = s.Replace("\r", " ").Replace("\n", " ");
            return s.Length > 55 ? s.Substring(0, 55) + "…" : s;
        }

        void CompressPdfTask(string gs, TaskItem task, int rowIndex, int processed, int total)
        {
            long targetBytes = (long)(task.TargetMB * 1024.0 * 1024.0);
            string tempDir = Path.Combine(Path.GetTempPath(), "PDF_Compressor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                Candidate best = null;
                Candidate strongest = null;

                Candidate c0 = MakePdfCandidate(gs, task, tempDir, 0.0, 0);
                UpdateInnerProgress(rowIndex, processed, total, 0.18, c0);

                if (c0.Size <= targetBytes)
                {
                    best = c0;
                }
                else
                {
                    strongest = MakePdfCandidate(gs, task, tempDir, 1.0, 1);
                    UpdateInnerProgress(rowIndex, processed, total, 0.32, strongest);

                    if (strongest.Size <= targetBytes)
                    {
                        best = strongest;
                        double low = 0.0;
                        double high = 1.0;

                        for (int k = 0; k < 7; k++)
                        {
                            double t = (low + high) / 2.0;
                            Candidate c = MakePdfCandidate(gs, task, tempDir, t, k + 2);
                            UpdateInnerProgress(rowIndex, processed, total, 0.35 + 0.08 * k, c);

                            if (c.Size <= targetBytes)
                            {
                                if (best == null || c.T < best.T)
                                    best = c;
                                high = t;
                            }
                            else
                            {
                                low = t;
                            }

                            if (best != null && best.Size >= (long)(targetBytes * 0.985))
                                break;
                        }
                    }
                }

                Candidate chosen;
                bool metTarget;

                if (best != null)
                {
                    chosen = best;
                    metTarget = true;
                }
                else
                {
                    chosen = strongest;
                    metTarget = false;
                }

                if (chosen == null || !File.Exists(chosen.Path))
                    throw new Exception("没有生成可用的 PDF");

                string dir = Path.GetDirectoryName(task.OutputPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.Copy(chosen.Path, task.OutputPath, true);

                task.OutputBytes = new FileInfo(task.OutputPath).Length;

                if (!metTarget)
                {
                    if (task.Strategy == "质量优先")
                        task.Status = "完成（质量保护下最低：" + FormatMB(task.OutputBytes) + " > " +
                            task.TargetMB.ToString("0.#") + " MB）";
                    else
                        task.Status = "完成（未达到目标：" + FormatMB(task.OutputBytes) + " > " +
                            task.TargetMB.ToString("0.#") + " MB）";
                }
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        Candidate MakePdfCandidate(string gs, TaskItem task, string tempDir, double t, int serial)
        {
            CompressParams p = GetParams(task.Mode, task.Strategy, t);
            string output = Path.Combine(tempDir, "candidate_" + serial.ToString("00") + ".pdf");

            long size = RunGhostscriptPdf(gs, task.InputPath, output, p);

            Candidate c = new Candidate();
            c.T = t;
            c.Path = output;
            c.Size = size;
            c.Dpi = p.Dpi;
            c.Quality = p.Quality;
            c.MonoDpi = p.MonoDpi;
            return c;
        }

        void UpdateInnerProgress(int rowIndex, int processed, int total, double withinTask, Candidate c)
        {
            int val = (int)Math.Round(((processed + withinTask) * 100.0) / total);
            progress.Value = Math.Max(0, Math.Min(99, val));

            TaskItem task = tasks[rowIndex];
            task.Status = "处理中：彩灰 " + c.Dpi + "dpi / 黑白 " + c.MonoDpi + "dpi / Q" + c.Quality + " → " + FormatMB(c.Size);
            UpdateRow(rowIndex);
        }

        CompressParams GetParams(string mode, string strategy, double t)
        {
            int maxDpi, minDpi, maxQ, minQ, maxMonoDpi, minMonoDpi;
            double dpiT, qT, monoT;
            bool qualityFirst = strategy == "质量优先";

            // t = 0: highest quality
            // t = 1: strongest compression allowed by the selected strategy.
            //
            // QUALITY-FIRST:
            // Protects readability. For ancient/scanned books, color/gray images
            // stop around 180 dpi and monochrome text layers around 200 dpi.
            //
            // SIZE-FIRST:
            // Allows much lower DPI only when the user explicitly chooses it.
            if (mode == "古籍 / 扫描件")
            {
                maxDpi = 300;
                maxQ = 92;
                maxMonoDpi = 300;

                if (qualityFirst)
                {
                    minDpi = 180;
                    minQ = 58;
                    minMonoDpi = 200;
                }
                else
                {
                    minDpi = 48;
                    minQ = 12;
                    minMonoDpi = 70;
                }

                dpiT = Math.Pow(t, 1.45);
                qT = Math.Pow(t, 0.86);
                monoT = Math.Pow(t, 1.30);
            }
            else if (mode == "普通文档")
            {
                maxDpi = 240;
                maxQ = 88;
                maxMonoDpi = 240;

                if (qualityFirst)
                {
                    minDpi = 140;
                    minQ = 52;
                    minMonoDpi = 160;
                }
                else
                {
                    minDpi = 48;
                    minQ = 12;
                    minMonoDpi = 70;
                }

                dpiT = Math.Pow(t, 1.15);
                qT = Math.Pow(t, 0.92);
                monoT = Math.Pow(t, 1.08);
            }
            else if (mode == "极限压缩")
            {
                maxDpi = 160;
                maxQ = 62;
                maxMonoDpi = 180;

                if (qualityFirst)
                {
                    minDpi = 110;
                    minQ = 38;
                    minMonoDpi = 130;
                }
                else
                {
                    minDpi = 36;
                    minQ = 8;
                    minMonoDpi = 50;
                }

                dpiT = t;
                qT = t;
                monoT = t;
            }
            else
            {
                maxDpi = 300;
                maxQ = 90;
                maxMonoDpi = 300;

                if (qualityFirst)
                {
                    minDpi = 160;
                    minQ = 55;
                    minMonoDpi = 180;
                }
                else
                {
                    minDpi = 48;
                    minQ = 12;
                    minMonoDpi = 70;
                }

                dpiT = Math.Pow(t, 1.28);
                qT = Math.Pow(t, 0.92);
                monoT = Math.Pow(t, 1.16);
            }

            CompressParams p = new CompressParams();
            p.Dpi = (int)Math.Round(maxDpi - ((maxDpi - minDpi) * dpiT));
            p.Quality = (int)Math.Round(maxQ - ((maxQ - minQ) * qT));
            p.MonoDpi = (int)Math.Round(maxMonoDpi - ((maxMonoDpi - minMonoDpi) * monoT));

            p.Dpi = Math.Max(minDpi, Math.Min(maxDpi, p.Dpi));
            p.Quality = Math.Max(minQ, Math.Min(maxQ, p.Quality));
            p.MonoDpi = Math.Max(minMonoDpi, Math.Min(maxMonoDpi, p.MonoDpi));

            return p;
        }

        long RunGhostscriptPdf(string gs, string input, string output, CompressParams p)
        {
            if (File.Exists(output)) File.Delete(output);

            // pdfwrite guarantees PDF output. Text/vector content remains PDF content;
            // embedded raster images may be downsampled/recompressed.
            string args =
                "-dBATCH -dNOPAUSE -dQUIET " +
                "-dCompatibilityLevel=1.6 -sDEVICE=pdfwrite " +
                "-dDetectDuplicateImages=true -dCompressFonts=true -dSubsetFonts=true " +
                "-dPassThroughJPEGImages=false " +

                "-dDownsampleColorImages=true -dColorImageDownsampleType=/Bicubic " +
                "-dColorImageResolution=" + p.Dpi + " -dColorImageDownsampleThreshold=1.0 " +
                "-dAutoFilterColorImages=false -dColorImageFilter=/DCTEncode " +

                "-dDownsampleGrayImages=true -dGrayImageDownsampleType=/Bicubic " +
                "-dGrayImageResolution=" + p.Dpi + " -dGrayImageDownsampleThreshold=1.0 " +
                "-dAutoFilterGrayImages=false -dGrayImageFilter=/DCTEncode " +

                "-dJPEGQ=" + p.Quality + " " +

                "-dDownsampleMonoImages=true -dMonoImageDownsampleType=/Subsample " +
                "-dMonoImageResolution=" + p.MonoDpi + " -dMonoImageDownsampleThreshold=1.0 " +
                "-dAutoFilterMonoImages=false -dMonoImageFilter=/CCITTFaxEncode " +

                "-sOutputFile=\"" + output + "\" " +
                "\"" + input + "\"";

            RunProcess(gs, args);

            if (!File.Exists(output))
                throw new Exception("Ghostscript 未生成 PDF");

            return new FileInfo(output).Length;
        }

        void ExportImagesTask(string gs, TaskItem task, string device, int rowIndex, int processed, int total)
        {
            string folder = task.OutputPath;
            Directory.CreateDirectory(folder);

            int dpi;
            int quality;
            switch (task.Mode)
            {
                case "古籍 / 扫描件":
                    dpi = 300; quality = 90; break;
                case "普通文档":
                    dpi = 180; quality = 82; break;
                case "极限压缩":
                    dpi = 100; quality = 55; break;
                default:
                    dpi = 220; quality = 86; break;
            }

            string ext = device == "jpeg" ? "jpg" : "png";
            string outputPattern = Path.Combine(folder, "page_%04d." + ext);

            string args =
                "-dBATCH -dNOPAUSE -dQUIET " +
                "-sDEVICE=" + device + " " +
                "-r" + dpi + " " +
                (device == "jpeg" ? "-dJPEGQ=" + quality + " " : "") +
                "-sOutputFile=\"" + outputPattern + "\" " +
                "\"" + task.InputPath + "\"";

            task.Status = "处理中：逐页输出 " + ext.ToUpperInvariant() + "…";
            UpdateRow(rowIndex);

            RunProcess(gs, args);

            string[] files = Directory.GetFiles(folder, "*." + ext);
            if (files.Length == 0)
                throw new Exception("没有生成图片");

            long totalBytes = 0;
            foreach (string f in files)
                totalBytes += new FileInfo(f).Length;

            task.OutputBytes = totalBytes;

            int val = (int)Math.Round(((processed + 0.95) * 100.0) / total);
            progress.Value = Math.Max(0, Math.Min(99, val));
        }

        void RunProcess(string exe, string args)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = exe;
            psi.Arguments = args;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;

            using (Process p = Process.Start(psi))
            {
                while (!p.HasExited)
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }

                string err = p.StandardError.ReadToEnd();
                string output = p.StandardOutput.ReadToEnd();

                if (p.ExitCode != 0)
                    throw new Exception("Ghostscript 执行失败：" + err + " " + output);
            }
        }

        void OpenOutputFolder()
        {
            try
            {
                string dir = txtOutputFolder.Text.Trim();
                if (Directory.Exists(dir))
                    Process.Start("explorer.exe", "\"" + dir + "\"");
            }
            catch { }
        }

        static string FormatMB(long bytes)
        {
            return (bytes / 1024.0 / 1024.0).ToString("N2") + " MB";
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
