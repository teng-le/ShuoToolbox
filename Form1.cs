using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Toolbox
{
    public class ToolItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public class CategoryItem
    {
        public string Name { get; set; } = "";
        public List<ToolItem> Tools { get; set; } = new();
    }

    public class ToolboxConfig
    {
        public List<CategoryItem> Categories { get; set; } = new();
    }

    public class HardwareCard
    {
        public string Title { get; set; } = "";
        public List<KeyValuePair<string, string>> Items { get; set; } = new();

        public HardwareCard(string title, List<KeyValuePair<string, string>> items)
        {
            Title = title;
            Items = items;
        }
    }

    // 圆角卡片容器
    public class CardPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public Color BorderColor { get; set; } = Color.FromArgb(220, 220, 220);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int CornerRadius { get; set; } = 8;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            if (rect.Width <= 0 || rect.Height <= 0) return;

            using var path = RoundedRect(rect, CornerRadius);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);
            using var pen = new Pen(BorderColor, 1);
            e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public partial class Form1 : Form
    {
        private const string HardwareInfoCategory = "硬件信息";
        private const string AppVersion = "v0.1.0";
        private const int WindowCornerRadius = 8;

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect,
            int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string configPath;

        private Panel contentHost = null!;
        private FlowLayoutPanel toolPanel = null!;
        private Panel hardwarePanel = null!;
        private TableLayoutPanel cardsLayout = null!;
        private ListBox categoryList = null!;
        private Label descLabel = null!;

        private Button? selectedButton;
        private Button? lastClickedButton;
        private DateTime lastClickTime = DateTime.MinValue;
        private ToolboxConfig config = new();
        private bool hardwareLoaded = false;

        public Form1()
        {
            InitializeComponent();
            configPath = Path.Combine(baseDir, "tools.json");

            this.Text = "工具箱";
            this.Size = new Size(880, 640);
            this.StartPosition = FormStartPosition.CenterScreen;

            contentHost = new Panel();
            contentHost.Dock = DockStyle.Fill;
            this.Controls.Add(contentHost);

            descLabel = new Label();
            descLabel.Dock = DockStyle.Bottom;
            descLabel.Height = 56;
            descLabel.BackColor = Color.FromArgb(235, 235, 235);
            descLabel.ForeColor = Color.FromArgb(60, 60, 60);
            descLabel.Font = new Font("微软雅黑", 9.5F);
            descLabel.TextAlign = ContentAlignment.MiddleLeft;
            descLabel.Padding = new Padding(16, 0, 16, 0);
            descLabel.Text = "单击工具查看说明，双击打开工具。";
            this.Controls.Add(descLabel);

            // 左侧栏容器：上方 ListBox + 底部设置按钮 + 底部版本号
            var leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Left;
            leftPanel.Width = 170;
            leftPanel.BackColor = Color.FromArgb(235, 235, 235);
            this.Controls.Add(leftPanel);

            categoryList = new ListBox();
            categoryList.Dock = DockStyle.Fill;
            categoryList.BorderStyle = BorderStyle.None;
            categoryList.Font = new Font("微软雅黑", 10.5F);
            categoryList.BackColor = Color.FromArgb(235, 235, 235);
            categoryList.SelectedIndexChanged += CategoryList_SelectedIndexChanged;
            leftPanel.Controls.Add(categoryList);

            var settingsButton = new Button();
            settingsButton.Text = "⚙";
            settingsButton.Font = new Font("Segoe UI Symbol", 12F);
            settingsButton.FlatStyle = FlatStyle.Flat;
            settingsButton.FlatAppearance.BorderSize = 0;
            settingsButton.ForeColor = Color.FromArgb(110, 110, 110);
            settingsButton.BackColor = Color.FromArgb(235, 235, 235);
            settingsButton.Dock = DockStyle.Bottom;
            settingsButton.Height = 34;
            settingsButton.Cursor = Cursors.Hand;
            settingsButton.Click += (s, e) =>
            {
                MessageBox.Show("设置功能尚未实现，敬请期待。", "设置",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            leftPanel.Controls.Add(settingsButton);

            var versionLabel = new Label();
            versionLabel.Text = AppVersion;
            versionLabel.Font = new Font("微软雅黑", 8.5F);
            versionLabel.ForeColor = Color.FromArgb(160, 160, 160);
            versionLabel.BackColor = Color.FromArgb(235, 235, 235);
            versionLabel.TextAlign = ContentAlignment.MiddleCenter;
            versionLabel.Dock = DockStyle.Bottom;
            versionLabel.Height = 22;
            leftPanel.Controls.Add(versionLabel);

            BuildToolPanel();
            BuildHardwarePanel();

            LoadConfig();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyWindowRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyWindowRegion();
        }

        private void ApplyWindowRegion()
        {
            if (!IsHandleCreated) return;

            if (WindowState == FormWindowState.Maximized)
            {
                SetWindowRgn(Handle, IntPtr.Zero, true);
                return;
            }

            var rgn = CreateRoundRectRgn(0, 0, Width + 1, Height + 1,
                WindowCornerRadius * 2, WindowCornerRadius * 2);
            if (rgn != IntPtr.Zero)
            {
                SetWindowRgn(Handle, rgn, true);
            }
        }

        private void BuildToolPanel()
        {
            toolPanel = new FlowLayoutPanel();
            toolPanel.Dock = DockStyle.Fill;
            toolPanel.Padding = new Padding(12);
            toolPanel.AutoScroll = true;
            toolPanel.BackColor = Color.FromArgb(245, 245, 245);
            toolPanel.Visible = false;
            contentHost.Controls.Add(toolPanel);
        }

        private void BuildHardwarePanel()
        {
            hardwarePanel = new Panel();
            hardwarePanel.Dock = DockStyle.Fill;
            hardwarePanel.BackColor = Color.FromArgb(245, 246, 248);
            hardwarePanel.Visible = false;

            var scrollPanel = new Panel();
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.AutoScroll = true;
            scrollPanel.Padding = new Padding(16);
            scrollPanel.BackColor = Color.FromArgb(245, 246, 248);
            hardwarePanel.Controls.Add(scrollPanel);

            cardsLayout = new TableLayoutPanel();
            cardsLayout.Dock = DockStyle.Top;
            cardsLayout.AutoSize = true;
            cardsLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            cardsLayout.ColumnCount = 1;
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cardsLayout.Padding = new Padding(0);
            cardsLayout.Margin = new Padding(0);
            scrollPanel.Controls.Add(cardsLayout);

            var loading = new Label();
            loading.Text = "正在读取硬件信息…";
            loading.Font = new Font("微软雅黑", 10F);
            loading.ForeColor = Color.FromArgb(110, 110, 110);
            loading.AutoSize = true;
            loading.Margin = new Padding(4, 8, 4, 8);
            cardsLayout.Controls.Add(loading);

            contentHost.Controls.Add(hardwarePanel);
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    MessageBox.Show("找不到 tools.json，请检查文件是否和 Toolbox.exe 放在一起。",
                        "配置缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                config = JsonSerializer.Deserialize<ToolboxConfig>(json, options) ?? new ToolboxConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show("读取 tools.json 失败：" + ex.Message,
                    "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            categoryList.Items.Clear();
            categoryList.Items.Add(HardwareInfoCategory);
            foreach (var category in config.Categories)
            {
                categoryList.Items.Add(category.Name);
            }

            if (categoryList.Items.Count > 0)
            {
                categoryList.SelectedIndex = 0;
            }
        }

        private void CategoryList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? categoryName = categoryList.SelectedItem as string;
            if (categoryName == null) return;

            if (categoryName == HardwareInfoCategory)
            {
                ShowHardwarePanel();
                return;
            }

            ShowToolPanel(categoryName);
        }

        private void ShowHardwarePanel()
        {
            toolPanel.Visible = false;
            hardwarePanel.Visible = true;
            descLabel.Visible = false;

            if (!hardwareLoaded)
            {
                hardwareLoaded = true;
                _ = LoadHardwareInfoAsync();
            }
        }

        private void ShowToolPanel(string categoryName)
        {
            hardwarePanel.Visible = false;
            toolPanel.Visible = true;
            descLabel.Visible = true;
            descLabel.Text = "单击工具查看说明，双击打开工具。";

            toolPanel.Controls.Clear();
            selectedButton = null;
            lastClickedButton = null;
            lastClickTime = DateTime.MinValue;

            CategoryItem? category = config.Categories.Find(c => c.Name == categoryName);
            if (category == null) return;

            foreach (var tool in category.Tools)
            {
                Button btn = new Button();
                btn.Text = tool.Name;
                btn.Size = new Size(130, 120);
                btn.Margin = new Padding(10);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                btn.BackColor = Color.White;
                btn.TextImageRelation = TextImageRelation.ImageAboveText;
                btn.ImageAlign = ContentAlignment.MiddleCenter;
                btn.TextAlign = ContentAlignment.BottomCenter;
                btn.Font = new Font("微软雅黑", 9.5F);

                string fullPath = ResolvePath(tool.Path);
                try
                {
                    Icon? icon = Icon.ExtractAssociatedIcon(fullPath);
                    if (icon != null)
                    {
                        btn.Image = new Bitmap(icon.ToBitmap(), new Size(48, 48));
                    }
                }
                catch { }

                string path = tool.Path;
                string name = tool.Name;
                string description = tool.Description;

                btn.MouseDown += (s, e) =>
                {
                    if (e.Button != MouseButtons.Left) return;
                    DateTime now = DateTime.Now;
                    bool isDoubleClick = (btn == lastClickedButton) &&
                                         ((now - lastClickTime).TotalMilliseconds <= SystemInformation.DoubleClickTime);
                    if (isDoubleClick)
                    {
                        lastClickedButton = null;
                        lastClickTime = DateTime.MinValue;
                        Launch(path);
                    }
                    else
                    {
                        lastClickedButton = btn;
                        lastClickTime = now;
                        SelectButton(btn);
                        ShowDescription(name, description);
                    }
                };

                toolPanel.Controls.Add(btn);
            }
        }

        private async Task LoadHardwareInfoAsync()
        {
            List<HardwareCard> cards;
            try
            {
                cards = await Task.Run(BuildHardwareCards);
            }
            catch (Exception ex)
            {
                cards = new List<HardwareCard>
                {
                    new HardwareCard("读取失败", new List<KeyValuePair<string, string>>
                    {
                        new("错误信息", ex.Message)
                    })
                };
            }

            RenderHardwareCards(cards);
        }

        private void RenderHardwareCards(List<HardwareCard> cards)
        {
            cardsLayout.SuspendLayout();
            cardsLayout.Controls.Clear();
            cardsLayout.RowStyles.Clear();
            cardsLayout.RowCount = cards.Count;

            for (int i = 0; i < cards.Count; i++)
            {
                cardsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                var card = BuildCardControl(cards[i]);
                cardsLayout.Controls.Add(card, 0, i);
            }

            cardsLayout.ResumeLayout(true);
        }

        private Control BuildCardControl(HardwareCard data)
        {
            var card = new CardPanel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White,
                BorderColor = Color.FromArgb(220, 222, 226),
                CornerRadius = 8,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var inner = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var title = new Label
            {
                Text = data.Title,
                Dock = DockStyle.Fill,
                Font = new Font("微软雅黑", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };
            inner.Controls.Add(title, 0, 0);

            var detail = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = Math.Max(1, data.Items.Count),
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int r = 0; r < data.Items.Count; r++)
            {
                detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));

                var nameLabel = new Label
                {
                    Text = data.Items[r].Key,
                    Dock = DockStyle.Fill,
                    Font = new Font("微软雅黑", 9.5F),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(0, 0, 6, 0)
                };

                var valueLabel = new Label
                {
                    Text = string.IsNullOrWhiteSpace(data.Items[r].Value) ? "（未检测到）" : data.Items[r].Value,
                    Dock = DockStyle.Fill,
                    Font = new Font("微软雅黑", 9.5F),
                    ForeColor = Color.FromArgb(30, 30, 30),
                    TextAlign = ContentAlignment.MiddleLeft,
                    AutoEllipsis = true,
                    Margin = new Padding(0)
                };

                detail.Controls.Add(nameLabel, 0, r);
                detail.Controls.Add(valueLabel, 1, r);
            }

            inner.Controls.Add(detail, 0, 1);
            card.Controls.Add(inner);
            return card;
        }

        private List<HardwareCard> BuildHardwareCards()
        {
            var cards = new List<HardwareCard>();

            cards.Add(new HardwareCard("系统概览", new List<KeyValuePair<string, string>>
            {
                new("计算机名", QuerySingle("Win32_ComputerSystem", "Name")),
                new("操作系统", QuerySingle("Win32_OperatingSystem", "Caption")),
                new("系统版本", QuerySingle("Win32_OperatingSystem", "Version")),
                new("系统架构", QuerySingle("Win32_Processor", "AddressWidth") + " 位"),
                new("运行时间", GetUptime()),
            }));

            cards.Add(new HardwareCard("处理器", new List<KeyValuePair<string, string>>
            {
                new("型号", QuerySingle("Win32_Processor", "Name")),
                new("核心 / 线程", QuerySingle("Win32_Processor", "NumberOfCores") + " 核 / " + QuerySingle("Win32_Processor", "NumberOfLogicalProcessors") + " 线程"),
                new("基准频率", QuerySingle("Win32_Processor", "MaxClockSpeed") + " MHz"),
            }));

            cards.Add(new HardwareCard("主板", new List<KeyValuePair<string, string>>
            {
                new("制造商", QuerySingle("Win32_BaseBoard", "Manufacturer")),
                new("型号", QuerySingle("Win32_BaseBoard", "Product")),
            }));

            cards.Add(new HardwareCard("内存", BuildMemoryItems()));
            cards.Add(new HardwareCard("显卡", BuildVideoItems()));
            cards.Add(new HardwareCard("显示器", BuildMonitorItems()));
            cards.Add(new HardwareCard("磁盘", BuildDiskItems()));
            cards.Add(new HardwareCard("声卡", BuildSoundItems()));
            cards.Add(new HardwareCard("网卡", BuildNetworkItems()));

            return cards;
        }

        private List<KeyValuePair<string, string>> BuildMemoryItems()
        {
            var items = new List<KeyValuePair<string, string>>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Capacity, Speed, Manufacturer FROM Win32_PhysicalMemory");
                int index = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    double gb = Convert.ToDouble(obj["Capacity"] ?? 0) / (1024.0 * 1024 * 1024);
                    string speed = obj["Speed"]?.ToString() ?? "?";
                    string maker = obj["Manufacturer"]?.ToString() ?? "";
                    items.Add(new KeyValuePair<string, string>(
                        $"内存条 {index}",
                        $"{gb:F1} GB  {speed} MHz  {maker}".Trim()));
                    index++;
                }
            }
            catch { }
            if (items.Count == 0) items.Add(new KeyValuePair<string, string>("内存", ""));
            return items;
        }

        private List<KeyValuePair<string, string>> BuildVideoItems()
        {
            var items = new List<KeyValuePair<string, string>>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, AdapterRAM FROM Win32_VideoController");
                int index = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    double vram = Convert.ToDouble(obj["AdapterRAM"] ?? 0) / (1024.0 * 1024 * 1024);
                    items.Add(new KeyValuePair<string, string>(
                        $"显卡 {index}",
                        $"{name}  （显存 {vram:F1} GB）"));
                    index++;
                }
            }
            catch { }
            if (items.Count == 0) items.Add(new KeyValuePair<string, string>("显卡", ""));
            return items;
        }

        private List<KeyValuePair<string, string>> BuildMonitorItems()
        {
            var items = new List<KeyValuePair<string, string>>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_DesktopMonitor");
                int index = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    items.Add(new KeyValuePair<string, string>($"显示器 {index}", name));
                    index++;
                }
            }
            catch { }
            if (items.Count == 0) items.Add(new KeyValuePair<string, string>("显示器", ""));
            return items;
        }

        private List<KeyValuePair<string, string>> BuildDiskItems()
        {
            var items = new List<KeyValuePair<string, string>>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Model, Size FROM Win32_DiskDrive");
                int index = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    double gb = Convert.ToDouble(obj["Size"] ?? 0) / (1024.0 * 1024 * 1024);
                    items.Add(new KeyValuePair<string, string>(
                        $"磁盘 {index}",
                        $"{obj["Model"]}  {gb:F1} GB"));
                    index++;
                }
            }
            catch { }
            if (items.Count == 0) items.Add(new KeyValuePair<string, string>("磁盘", ""));
            return items;
        }

        private List<KeyValuePair<string, string>> BuildSoundItems()
        {
            var items = new List<KeyValuePair<string, string>>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_SoundDevice");
                int index = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    items.Add(new KeyValuePair<string, string>($"声卡 {index}", name));
                    index++;
                }
            }
            catch { }
            if (items.Count == 0) items.Add(new KeyValuePair<string, string>("声卡", ""));
            return items;
        }

        private List<KeyValuePair<string, string>> BuildNetworkItems()
        {
            var items = new List<KeyValuePair<string, string>>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, MACAddress FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True");
                int index = 1;
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    string mac = obj["MACAddress"]?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    items.Add(new KeyValuePair<string, string>($"网卡 {index}", $"{name}  MAC: {mac}"));
                    index++;
                }
            }
            catch { }
            if (items.Count == 0) items.Add(new KeyValuePair<string, string>("网卡", ""));
            return items;
        }

        private string QuerySingle(string wmiClass, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    object? val = obj[property];
                    if (val != null)
                    {
                        string s = val.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                    }
                }
            }
            catch { }
            return "";
        }

        private string GetUptime()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string raw = obj["LastBootUpTime"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        DateTime boot = ManagementDateTimeConverter.ToDateTime(raw);
                        TimeSpan uptime = DateTime.Now - boot;
                        return $"{(int)uptime.TotalDays} 天 {uptime.Hours} 小时 {uptime.Minutes} 分钟";
                    }
                }
            }
            catch { }
            return "（未知）";
        }

        private void SelectButton(Button btn)
        {
            if (selectedButton != null && selectedButton != btn)
            {
                selectedButton.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                selectedButton.BackColor = Color.White;
            }
            selectedButton = btn;
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215);
            btn.BackColor = Color.FromArgb(240, 248, 255);
        }

        private void ShowDescription(string name, string description)
        {
            descLabel.Text = string.IsNullOrWhiteSpace(description)
                ? name + "：暂无说明。"
                : name + "：" + description;
        }

        private string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path)) return path;
            string toolboxPath = Path.Combine(baseDir, path);
            if (File.Exists(toolboxPath)) return toolboxPath;
            string sysPath = Path.Combine(Environment.SystemDirectory, path);
            if (File.Exists(sysPath)) return sysPath;
            return toolboxPath;
        }

        private void Launch(string path)
        {
            string fullPath = ResolvePath(path);
            if (File.Exists(fullPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = fullPath,
                    WorkingDirectory = Path.GetDirectoryName(fullPath) ?? baseDir,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("找不到工具文件：\n" + fullPath,
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}