using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Toolbox
{
    public partial class Form1 : Form
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        private const string HardwareInfoCategory = "硬件信息";
        private const string AppVersion = "v0.5.3";
        private const int TitleBarHeight = 36;
        private const int WindowCornerRadius = 6;
        private const int ResizeBorder = 8;

        private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string configPath;

        private Panel contentHost = null!;
        private FlowLayoutPanel toolPanel = null!;
        private Panel hardwarePanel = null!;
        private Panel hardwareToolbar = null!;
        private Panel hardwareScrollPanel = null!;
        private TableLayoutPanel cardsLayout = null!;
        private Label hardwareTitleLabel = null!;
        private Panel leftPanel = null!;
        private ListBox categoryList = null!;
        private Label descLabel = null!;
        private Label versionLabel = null!;

        private Button? exportButton;
        private Button? refreshButton;
        private List<HardwareCard>? currentHardwareCards;

        private bool isGjModeActive = false;
        private Panel gjDashboardPanel = null!;
        private Panel gjFlashToolPanel = null!;
        private Panel gjScrcpyPanel = null!;
        
        private CardPanel welcomeCard = null!;
        private CardPanel flashCard = null!;
        private CardPanel scrcpyCard = null!;

        private Button? settingsButton;
        private Button? minimizeButton;
        private Button? maximizeButton;
        private Button? closeButton;
        private Button? phoneModeButton;

        private Button? selectedButton;
        private Button? lastClickedButton;
        private DateTime lastClickTime = DateTime.MinValue;
        private ToolboxConfig config = new();
        private bool hardwareLoaded = false;

        private NotifyIcon? trayIcon;
        private ContextMenuStrip? trayMenu;
        private bool isExit = false;
        private AppSettings currentSettings;

        private bool IsDarkMode => currentSettings.Theme == "Dark";

        public Form1()
        {
            currentSettings = AppSettings.Load();
            InitializeComponent();
            configPath = Path.Combine(baseDir, "tools.json");

            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "硕のtoolbox";
            this.Size = new Size(880, 640);
            this.MinimumSize = new Size(720, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(0, TitleBarHeight, 0, 0);
            this.DoubleBuffered = true;

            contentHost = new Panel { Dock = DockStyle.Fill, BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248) };
            this.Controls.Add(contentHost);

            descLabel = new Label { Dock = DockStyle.Bottom, Height = 56, BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235), ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(60, 60, 60), Font = new Font("微软雅黑", 9.5F), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 16, 0), Text = "单击工具查看说明，双击打开工具。" };
            this.Controls.Add(descLabel);

            leftPanel = new Panel { Dock = DockStyle.Left, Width = 170, BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235) };
            this.Controls.Add(leftPanel);

            categoryList = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("微软雅黑", 10.5F), BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235), ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(60, 60, 60) };
            categoryList.SelectedIndexChanged += CategoryList_SelectedIndexChanged;
            leftPanel.Controls.Add(categoryList);

            phoneModeButton = new Button { Text = "📱 切换搞机模式", Dock = DockStyle.Bottom, Height = 44, FlatStyle = FlatStyle.Flat, BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(225, 225, 225), ForeColor = IsDarkMode ? Color.FromArgb(0, 153, 255) : Color.FromArgb(0, 102, 204), Font = new Font("微软雅黑", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            phoneModeButton.FlatAppearance.BorderSize = 0;
            phoneModeButton.Click += PhoneModeButton_Click;
            leftPanel.Controls.Add(phoneModeButton);

            versionLabel = new Label { Text = AppVersion, Font = new Font("微软雅黑", 8.5F), ForeColor = IsDarkMode ? Color.FromArgb(160, 160, 160) : Color.FromArgb(140, 140, 140), BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Bottom, Height = 24 };
            leftPanel.Controls.Add(versionLabel);

            BuildTitleBarButtons();
            BuildToolPanel();
            BuildHardwarePanel();
            BuildGjModePanels();

            if (currentSettings.StartMinimized) InitializeTray();
            LoadConfig();
        }

        private void BuildGjModePanels()
        {
            Color bgColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248);
            Color cardBg = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White;
            Color cardBorder = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(220, 222, 226);
            Color titleColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(50, 50, 50);

            gjDashboardPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgColor, Visible = false };
            welcomeCard = new CardPanel { Size = new Size(500, 180), Location = new Point(30, 30), BackColor = cardBg, BorderColor = cardBorder, CornerRadius = 8 };
            var title = new Label { Text = "📱 搞机控制台", Font = new Font("微软雅黑", 16F, FontStyle.Bold), ForeColor = titleColor, BackColor = Color.Transparent, AutoSize = true, Location = new Point(20, 20) };
            welcomeCard.Controls.Add(title);
            var tip = new Label { Text = "当前处于沙盒环境，请从左侧选择功能模块。\n离线驱动与资源包已随主程序放置在对应目录下。", Font = new Font("微软雅黑", 10F), ForeColor = Color.Gray, BackColor = Color.Transparent, AutoSize = true, Location = new Point(20, 70) };
            welcomeCard.Controls.Add(tip);
            gjDashboardPanel.Controls.Add(welcomeCard);

            gjFlashToolPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgColor, Visible = false };
            flashCard = new CardPanel { Size = new Size(500, 200), Location = new Point(30, 30), BackColor = cardBg, BorderColor = cardBorder, CornerRadius = 8 };
            var flashTitle = new Label { Text = "🧰 刷机匣 (GeekFlashTool)", Font = new Font("微软雅黑", 14F, FontStyle.Bold), ForeColor = titleColor, BackColor = Color.Transparent, AutoSize = true, Location = new Point(20, 20) };
            flashCard.Controls.Add(flashTitle);
            var flashDesc = new Label { Text = "深度刷机与底层救砖工具。\n需要管理员权限，启动时会请求提权。", Font = new Font("微软雅黑", 9.5F), ForeColor = Color.Gray, BackColor = Color.Transparent, AutoSize = true, Location = new Point(20, 60) };
            flashCard.Controls.Add(flashDesc);
            var btnFlash = new Button { Text = "启动刷机匣", Size = new Size(160, 40), Location = new Point(20, 120), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(230, 80, 80), ForeColor = Color.White, Font = new Font("微软雅黑", 10F) };
            btnFlash.FlatAppearance.BorderSize = 0;
            btnFlash.Click += (s, e) => LaunchToolByPath("Tools\\GeekFlashTool\\GeekFlashTool.exe", "Tools\\GeekFlashTool", true);
            flashCard.Controls.Add(btnFlash);
            gjFlashToolPanel.Controls.Add(flashCard);

            gjScrcpyPanel = new Panel { Dock = DockStyle.Fill, BackColor = bgColor, Visible = false };
            scrcpyCard = new CardPanel { Size = new Size(500, 200), Location = new Point(30, 30), BackColor = cardBg, BorderColor = cardBorder, CornerRadius = 8 };
            var scrcpyTitle = new Label { Text = "📲 QtScrcpy 投屏", Font = new Font("微软雅黑", 14F, FontStyle.Bold), ForeColor = titleColor, BackColor = Color.Transparent, AutoSize = true, Location = new Point(20, 20) };
            scrcpyCard.Controls.Add(scrcpyTitle);
            var scrcpyDesc = new Label { Text = "安卓设备投屏与控制，内置 adb。\n请确保设备已开启 USB 调试。", Font = new Font("微软雅黑", 9.5F), ForeColor = Color.Gray, BackColor = Color.Transparent, AutoSize = true, Location = new Point(20, 60) };
            scrcpyCard.Controls.Add(scrcpyDesc);
            var btnScrcpy = new Button { Text = "启动投屏", Size = new Size(160, 40), Location = new Point(20, 120), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, Font = new Font("微软雅黑", 10F) };
            btnScrcpy.FlatAppearance.BorderSize = 0;
            btnScrcpy.Click += (s, e) => LaunchToolByPath("Tools\\QtScrcpy\\QtScrcpy.exe", "Tools\\QtScrcpy");
            scrcpyCard.Controls.Add(btnScrcpy);
            gjScrcpyPanel.Controls.Add(scrcpyCard);

            contentHost.Controls.Add(gjDashboardPanel);
            contentHost.Controls.Add(gjFlashToolPanel);
            contentHost.Controls.Add(gjScrcpyPanel);
        }

        private void PhoneModeButton_Click(object? sender, EventArgs e)
        {
            isGjModeActive = !isGjModeActive;

            if (isGjModeActive)
            {
                phoneModeButton!.Text = "🔙 退出搞机模式";
                toolPanel.Visible = false;
                hardwarePanel.Visible = false;
                descLabel.Visible = false;

                categoryList.Items.Clear();
                categoryList.Items.Add("📊 仪表盘");
                categoryList.Items.Add("🧰 刷机匣");
                categoryList.Items.Add("📲 投屏");
                categoryList.SelectedIndex = 0;
            }
            else
            {
                phoneModeButton!.Text = "📱 切换搞机模式";
                HideAllGjPanels();

                categoryList.Items.Clear();
                categoryList.Items.Add(HardwareInfoCategory);
                foreach (var category in config.Categories) categoryList.Items.Add(category.Name);
                categoryList.SelectedIndex = 0;
            }
        }

        private void CategoryList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            string? selected = categoryList.SelectedItem as string;
            if (selected == null) return;

            if (isGjModeActive)
            {
                HideAllGjPanels();
                switch (selected)
                {
                    case "📊 仪表盘": gjDashboardPanel.Visible = true; break;
                    case "🧰 刷机匣": gjFlashToolPanel.Visible = true; break;
                    case "📲 投屏": gjScrcpyPanel.Visible = true; break;
                }
                return;
            }

            if (selected == HardwareInfoCategory) { ShowHardwarePanel(); return; }
            ShowToolPanel(selected);
        }

        private void HideAllGjPanels()
        {
            gjDashboardPanel.Visible = false;
            gjFlashToolPanel.Visible = false;
            gjScrcpyPanel.Visible = false;
        }

        private void LaunchToolByPath(string relativePath, string relativeWorkDir, bool requireAdmin = false)
        {
            string fullPath = ResolvePath(relativePath);
            string workDir = ResolvePath(relativeWorkDir);

            if (!File.Exists(fullPath))
            {
                MessageBox.Show("找不到工具文件，请确保文件存在：\n" + fullPath, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fullPath,
                    WorkingDirectory = workDir,
                    UseShellExecute = true,
                    Verb = requireAdmin ? "runas" : ""
                };
                Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("显示主界面", null, (s, e) => ShowMainForm());
            trayMenu.Items.Add("退出", null, (s, e) => ExitApplication());
            trayIcon = new NotifyIcon { Text = "硕のtoolbox", Icon = SystemIcons.Application, ContextMenuStrip = trayMenu, Visible = true };
            try { trayIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            trayIcon.DoubleClick += (s, e) => ShowMainForm();
        }
        private void ShowMainForm() { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); }
        private void ExitApplication() { isExit = true; if (trayIcon != null) trayIcon.Visible = false; Application.Exit(); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (isExit || e.CloseReason == CloseReason.WindowsShutDown) { base.OnFormClosing(e); return; }
            if (e.CloseReason == CloseReason.UserClosing)
            {
                if (trayIcon != null) { e.Cancel = true; this.Hide(); trayIcon.ShowBalloonTip(2000, "硕のtoolbox", "程序已最小化到托盘后台运行", ToolTipIcon.Info); }
                else { isExit = true; Application.Exit(); }
            }
            base.OnFormClosing(e);
        }

        private void BuildTitleBarButtons()
        {
            closeButton = CreateTitleBarButton("✕", true);
            closeButton.Click += (s, e) => Close();
            maximizeButton = CreateTitleBarButton("□");
            maximizeButton.Click += (s, e) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            minimizeButton = CreateTitleBarButton("—");
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
            settingsButton = CreateTitleBarButton("⚙");
            settingsButton.Click += (s, e) => 
            { 
                using var f = new SettingsForm(); 
                if (f.ShowDialog(this) == DialogResult.OK) 
                {
                    currentSettings = AppSettings.Load();
                    ApplyTheme();
                }
            };
            this.Controls.Add(closeButton); this.Controls.Add(maximizeButton); this.Controls.Add(minimizeButton); this.Controls.Add(settingsButton);
            RepositionTitleBarButtons();
        }

        private Button CreateTitleBarButton(string text, bool isClose = false)
        {
            var btn = new Button { Text = text, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Symbol", 10F), Size = new Size(40, TitleBarHeight), Cursor = Cursors.Hand, TabStop = false, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btn.FlatAppearance.BorderSize = 0; 
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent; 
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            
            btn.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 250, 250);
            btn.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(80, 80, 80);

            btn.MouseEnter += (s, e) => 
            { 
                btn.BackColor = isClose ? Color.FromArgb(232, 17, 35) : (IsDarkMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230)); 
                btn.ForeColor = isClose ? Color.White : (IsDarkMode ? Color.White : Color.FromArgb(60, 60, 60)); 
            };
            btn.MouseLeave += (s, e) => 
            { 
                btn.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 250, 250); 
                btn.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(80, 80, 80); 
            };
            return btn;
        }

        private void RepositionTitleBarButtons()
        {
            if (closeButton == null || maximizeButton == null || minimizeButton == null || settingsButton == null) return;
            int right = ClientSize.Width;
            closeButton.Location = new Point(right - 40, 0); maximizeButton.Location = new Point(right - 80, 0);
            minimizeButton.Location = new Point(right - 120, 0); settingsButton.Location = new Point(right - 160, 0);
        }

        protected override void OnLoad(EventArgs e) { base.OnLoad(e); MaximizedBounds = Screen.FromControl(this).WorkingArea; RepositionTitleBarButtons(); }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Normal && (Width < MinimumSize.Width || Height < MinimumSize.Height))
            {
                Width = Math.Max(Width, MinimumSize.Width);
                Height = Math.Max(Height, MinimumSize.Height);
            }
            ApplyWindowRegion(); RepositionTitleBarButtons();
            if (maximizeButton != null) maximizeButton.Text = WindowState == FormWindowState.Maximized ? "❐" : "□";
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); ApplyWindowRegion(); }

        private void ApplyWindowRegion()
        {
            if (!IsHandleCreated) return;
            if (WindowState == FormWindowState.Maximized) { NativeMethods.SetWindowRgn(Handle, IntPtr.Zero, true); return; }
            var rgn = NativeMethods.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, WindowCornerRadius * 2, WindowCornerRadius * 2);
            if (rgn != IntPtr.Zero) NativeMethods.SetWindowRgn(Handle, rgn, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var brush = new SolidBrush(IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 250, 250));
            e.Graphics.FillRectangle(brush, 0, 0, ClientSize.Width, TitleBarHeight);
            using var font = new Font("微软雅黑", 11F, FontStyle.Bold);
            using var textBrush = new SolidBrush(IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(50, 50, 50));
            var textRect = new Rectangle(16, 0, ClientSize.Width - 200, TitleBarHeight);
            var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
            e.Graphics.DrawString("硕のtoolbox", font, textBrush, textRect, sf);
            using var pen = new Pen(IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(220, 220, 220), 1);
            e.Graphics.DrawLine(pen, 0, TitleBarHeight - 1, ClientSize.Width, TitleBarHeight - 1);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084; const int HTCLIENT = 1; const int HTCAPTION = 2;
            const int HTLEFT = 10; const int HTRIGHT = 11; const int HTTOP = 12; const int HTTOPLEFT = 13;
            const int HTTOPRIGHT = 14; const int HTBOTTOM = 15; const int HTBOTTOMLEFT = 16; const int HTBOTTOMRIGHT = 17;
            base.WndProc(ref m);
            if (m.Msg != WM_NCHITTEST) return;
            int raw = m.LParam.ToInt32(); int sx = (short)(raw & 0xFFFF); int sy = (short)((raw >> 16) & 0xFFFF);
            Point pos = PointToClient(new Point(sx, sy));
            if (WindowState == FormWindowState.Maximized) { m.Result = pos.Y <= TitleBarHeight ? (IntPtr)HTCAPTION : (IntPtr)HTCLIENT; return; }
            int w = ClientSize.Width; int h = ClientSize.Height; int b = ResizeBorder;
            if (pos.X <= b && pos.Y <= b) { m.Result = (IntPtr)HTTOPLEFT; return; }
            if (pos.X >= w - b && pos.Y <= b) { m.Result = (IntPtr)HTTOPRIGHT; return; }
            if (pos.X <= b && pos.Y >= h - b) { m.Result = (IntPtr)HTBOTTOMLEFT; return; }
            if (pos.X >= w - b && pos.Y >= h - b) { m.Result = (IntPtr)HTBOTTOMRIGHT; return; }
            if (pos.X <= b) { m.Result = (IntPtr)HTLEFT; return; }
            if (pos.X >= w - b) { m.Result = (IntPtr)HTRIGHT; return; }
            if (pos.Y <= b) { m.Result = (IntPtr)HTTOP; return; }
            if (pos.Y >= h - b) { m.Result = (IntPtr)HTBOTTOM; return; }
            if (pos.Y <= TitleBarHeight) { m.Result = (IntPtr)HTCAPTION; return; }
            m.Result = (IntPtr)HTCLIENT;
        }

        private void BuildToolPanel()
        {
            toolPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), AutoScroll = true, BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 245, 245), Visible = false };
            contentHost.Controls.Add(toolPanel);
        }

        private void BuildHardwarePanel()
        {
            hardwarePanel = new Panel { Dock = DockStyle.Fill, BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248), Visible = false };

            hardwareToolbar = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 251, 252) };
            hardwareTitleLabel = new Label { Text = "🖥 硬件信息概览", Font = new Font("微软雅黑", 13F, FontStyle.Bold), ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(50, 50, 50), AutoSize = true, Location = new Point(20, 30) };
            hardwareToolbar.Controls.Add(hardwareTitleLabel);

            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, Dock = DockStyle.Right, Width = 130, Padding = new Padding(0, 10, 12, 8), BackColor = Color.Transparent };
            refreshButton = new Button { Text = "🔄 刷新", Size = new Size(110, 30), FlatStyle = FlatStyle.Flat, BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White, ForeColor = IsDarkMode ? Color.White : Color.FromArgb(60, 60, 60), Font = new Font("微软雅黑", 9.5F), Cursor = Cursors.Hand };
            refreshButton.FlatAppearance.BorderColor = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(200, 200, 200);
            refreshButton.Click += RefreshHardwareBtn_Click;
            exportButton = new Button { Text = "📄 导出为 TXT", Size = new Size(110, 30), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, Font = new Font("微软雅黑", 9.5F), Cursor = Cursors.Hand, Enabled = false };
            exportButton.FlatAppearance.BorderSize = 0;
            exportButton.Click += ExportHardwareBtn_Click;
            btnPanel.Controls.Add(refreshButton);
            btnPanel.Controls.Add(exportButton);
            hardwareToolbar.Controls.Add(btnPanel);

            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(220, 222, 226) };
            hardwareToolbar.Controls.Add(sep);

            hardwareScrollPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16), BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248) };
            
            // 【修复】如果开启了垂直同步，通过反射强行开启双缓冲
            if (currentSettings.EnableVSync)
            {
                typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(hardwareScrollPanel, true, null);
            }

            cardsLayout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Padding = new Padding(0), Margin = new Padding(0) };
            cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            hardwareScrollPanel.Controls.Add(cardsLayout);
            var loading = new Label { Text = "正在读取硬件信息…", Font = new Font("微软雅黑", 10F), ForeColor = IsDarkMode ? Color.FromArgb(160, 160, 160) : Color.FromArgb(110, 110, 110), AutoSize = true, Margin = new Padding(4, 8, 4, 8) };
            cardsLayout.Controls.Add(loading);

            hardwarePanel.Controls.Add(hardwareScrollPanel);
            hardwarePanel.Controls.Add(hardwareToolbar);
            contentHost.Controls.Add(hardwarePanel);

            if (hardwareScrollPanel.IsHandleCreated)
                SetWindowTheme(hardwareScrollPanel.Handle, IsDarkMode ? "DarkMode_Explorer" : "Explorer", null);
            hardwareScrollPanel.HandleCreated += (s, e) => 
                SetWindowTheme(hardwareScrollPanel.Handle, IsDarkMode ? "DarkMode_Explorer" : "Explorer", null);
        }

        private void RefreshHardwareBtn_Click(object? sender, EventArgs e)
        {
            if (exportButton != null) exportButton.Enabled = false;
            cardsLayout.SuspendLayout();
            cardsLayout.Controls.Clear();
            cardsLayout.RowStyles.Clear();
            cardsLayout.RowCount = 0;
            var loading = new Label { Text = "正在读取硬件信息…", Font = new Font("微软雅黑", 10F), ForeColor = IsDarkMode ? Color.FromArgb(160, 160, 160) : Color.FromArgb(110, 110, 110), AutoSize = true, Margin = new Padding(4, 8, 4, 8) };
            cardsLayout.RowCount = 1;
            cardsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cardsLayout.Controls.Add(loading);
            cardsLayout.ResumeLayout(true);
            _ = LoadHardwareInfoAsync();
        }

        private void ExportHardwareBtn_Click(object? sender, EventArgs e)
        {
            if (currentHardwareCards == null || currentHardwareCards.Count == 0)
            {
                MessageBox.Show("硬件信息尚未加载完成，请稍候或点击刷新。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string computerName = Environment.MachineName;
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string defaultFileName = $"{computerName}_硬件信息_{timestamp}.txt";

            using var sfd = new SaveFileDialog
            {
                Title = "导出硬件信息",
                Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                FileName = defaultFileName,
                DefaultExt = "txt",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                string content = BuildHardwareTxt(computerName);
                File.WriteAllText(sfd.FileName, content, new System.Text.UTF8Encoding(true));
                MessageBox.Show("已导出到：\n" + sfd.FileName, "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildHardwareTxt(string computerName)
        {
            const int totalWidth = 64;
            string eqLine = new string('=', totalWidth);
            string dashLine = new string('-', totalWidth);
            var sb = new System.Text.StringBuilder();

            sb.AppendLine(eqLine);
            sb.AppendLine("                        硕のtoolbox 硬件信息报告");
            sb.AppendLine(eqLine);
            sb.AppendLine($"  计算机名称 : {computerName}");
            sb.AppendLine($"  导出时间   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  工具箱版本 : {AppVersion}");
            sb.AppendLine($"  操作系统   : {Environment.OSVersion}");
            sb.AppendLine(eqLine);
            sb.AppendLine();

            if (currentHardwareCards != null)
            {
                foreach (var card in currentHardwareCards)
                {
                    sb.AppendLine($"【{card.Title}】");
                    sb.AppendLine(dashLine);
                    if (card.Items == null || card.Items.Count == 0)
                    {
                        sb.AppendLine("  （未检测到数据）");
                    }
                    else
                    {
                        int maxKeyWidth = 0;
                        foreach (var item in card.Items)
                        {
                            int w = GetDisplayWidth(item.Key);
                            if (w > maxKeyWidth) maxKeyWidth = w;
                        }
                        foreach (var item in card.Items)
                        {
                            string value = string.IsNullOrWhiteSpace(item.Value) ? "（未检测到）" : item.Value;
                            int pad = maxKeyWidth - GetDisplayWidth(item.Key);
                            sb.AppendLine($"  {item.Key}{new string(' ', pad + 2)}: {value}");
                        }
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine(eqLine);
            sb.AppendLine($"  报告结束 · 由 硕のtoolbox {AppVersion} 生成");
            sb.AppendLine(eqLine);
            return sb.ToString();
        }

        private static int GetDisplayWidth(string s)
        {
            int w = 0;
            foreach (char c in s) w += (c > 0x7F) ? 2 : 1;
            return w;
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(configPath)) { MessageBox.Show("找不到 tools.json", "配置缺失", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                string json = File.ReadAllText(configPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                config = JsonSerializer.Deserialize<ToolboxConfig>(json, options) ?? new ToolboxConfig();
            }
            catch (Exception ex) { MessageBox.Show("读取 tools.json 失败：" + ex.Message, "配置错误", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            categoryList.Items.Clear();
            categoryList.Items.Add(HardwareInfoCategory);
            foreach (var category in config.Categories) categoryList.Items.Add(category.Name);
            if (categoryList.Items.Count > 0) categoryList.SelectedIndex = 0;
        }

        private void ShowHardwarePanel()
        {
            toolPanel.Visible = false; hardwarePanel.Visible = true; descLabel.Visible = false;
            if (!hardwareLoaded) { hardwareLoaded = true; _ = LoadHardwareInfoAsync(); }
        }

        private void ShowToolPanel(string categoryName)
        {
            hardwarePanel.Visible = false; toolPanel.Visible = true; descLabel.Visible = true;
            descLabel.Text = "单击工具查看说明，双击打开工具。";
            toolPanel.Controls.Clear(); selectedButton = null; lastClickedButton = null; lastClickTime = DateTime.MinValue;
            CategoryItem? category = config.Categories.Find(c => c.Name == categoryName);
            if (category == null) return;
            
            Color textColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(60, 60, 60);
            Color btnBg = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White;
            Color btnBorder = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(200, 200, 200);

            foreach (var tool in category.Tools)
            {
                Button btn = new Button { Text = tool.Name, Size = new Size(130, 120), Margin = new Padding(10), FlatStyle = FlatStyle.Flat, BackColor = btnBg, ForeColor = textColor, TextImageRelation = TextImageRelation.ImageAboveText, ImageAlign = ContentAlignment.MiddleCenter, TextAlign = ContentAlignment.BottomCenter, Font = new Font("微软雅黑", 9.5F) };
                btn.FlatAppearance.BorderColor = btnBorder;
                string fullPath = ResolvePath(tool.Path);
                try { Icon? icon = Icon.ExtractAssociatedIcon(fullPath); if (icon != null) btn.Image = new Bitmap(icon.ToBitmap(), new Size(48, 48)); } catch { }
                string description = tool.Description;
                btn.MouseDown += (s, e) =>
                {
                    if (e.Button != MouseButtons.Left) return;
                    DateTime now = DateTime.Now;
                    bool isDoubleClick = (btn == lastClickedButton) && ((now - lastClickTime).TotalMilliseconds <= SystemInformation.DoubleClickTime);
                    if (isDoubleClick) { lastClickedButton = null; lastClickTime = DateTime.MinValue; Launch(tool); }
                    else { lastClickedButton = btn; lastClickTime = now; SelectButton(btn); ShowDescription(tool.Name, description); }
                };
                toolPanel.Controls.Add(btn);
            }
        }

        private async Task LoadHardwareInfoAsync()
        {
            List<HardwareCard> cards;
            try { cards = await Task.Run(HardwareInfoProvider.BuildHardwareCards); }
            catch (Exception ex) { cards = new List<HardwareCard> { new HardwareCard("读取失败", new List<KeyValuePair<string, string>> { new("错误信息", ex.Message) }) }; }
            currentHardwareCards = cards;
            RenderHardwareCards(cards);
            if (exportButton != null) exportButton.Enabled = true;
        }

        private void RenderHardwareCards(List<HardwareCard> cards)
        {
            cardsLayout.SuspendLayout(); cardsLayout.Controls.Clear(); cardsLayout.RowStyles.Clear(); cardsLayout.RowCount = cards.Count;
            for (int i = 0; i < cards.Count; i++) { cardsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); cardsLayout.Controls.Add(BuildCardControl(cards[i]), 0, i); }
            cardsLayout.ResumeLayout(true);
        }

        private Control BuildCardControl(HardwareCard data)
        {
            Color cardBg = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White;
            Color cardBorder = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(220, 222, 226);
            Color titleColor = IsDarkMode ? Color.FromArgb(0, 153, 255) : Color.FromArgb(0, 102, 204);
            Color keyColor = IsDarkMode ? Color.FromArgb(160, 160, 160) : Color.FromArgb(120, 120, 120);
            Color valColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(30, 30, 30);

            var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 12), Padding = new Padding(16, 12, 16, 12), BackColor = cardBg, BorderColor = cardBorder, CornerRadius = 8, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var inner = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, RowCount = 2, Margin = new Padding(0), Padding = new Padding(0) };
            inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); inner.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            var title = new Label { Text = data.Title, Dock = DockStyle.Fill, Font = new Font("微软雅黑", 10.5F, FontStyle.Bold), ForeColor = titleColor, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0) };
            inner.Controls.Add(title, 0, 0);
            var detail = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, RowCount = Math.Max(1, data.Items.Count), Margin = new Padding(0), Padding = new Padding(0) };
            detail.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F)); detail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int r = 0; r < data.Items.Count; r++)
            {
                detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
                var nameLabel = new Label { Text = data.Items[r].Key, Dock = DockStyle.Fill, Font = new Font("微软雅黑", 9.5F), ForeColor = keyColor, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 6, 0) };
                var valueLabel = new Label { Text = string.IsNullOrWhiteSpace(data.Items[r].Value) ? "（未检测到）" : data.Items[r].Value, Dock = DockStyle.Fill, Font = new Font("微软雅黑", 9.5F), ForeColor = valColor, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = new Padding(0) };
                detail.Controls.Add(nameLabel, 0, r); detail.Controls.Add(valueLabel, 1, r);
            }
            inner.Controls.Add(detail, 0, 1); card.Controls.Add(inner); return card;
        }

        private void SelectButton(Button btn)
        {
            if (selectedButton != null && selectedButton != btn) 
            { 
                selectedButton.FlatAppearance.BorderColor = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(200, 200, 200); 
                selectedButton.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White; 
            }
            selectedButton = btn; 
            btn.FlatAppearance.BorderColor = Color.FromArgb(0, 120, 215); 
            btn.BackColor = IsDarkMode ? Color.FromArgb(0, 80, 150) : Color.FromArgb(240, 248, 255);
        }

        private void ShowDescription(string name, string description) { descLabel.Text = string.IsNullOrWhiteSpace(description) ? name + "：暂无说明。" : name + "：" + description; }

        private string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (Path.IsPathRooted(path)) return path;
            string toolboxPath = Path.Combine(baseDir, path);
            if (File.Exists(toolboxPath) || Directory.Exists(toolboxPath)) return toolboxPath;
            string sysPath = Path.Combine(Environment.SystemDirectory, path);
            if (File.Exists(sysPath)) return sysPath;
            return toolboxPath;
        }

        private void Launch(ToolItem tool)
        {
            string fullPath = ResolvePath(tool.Path);
            if (!File.Exists(fullPath)) { MessageBox.Show("找不到工具文件：\n" + fullPath, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            string workDir = string.IsNullOrEmpty(tool.WorkingDirectory) ? Path.GetDirectoryName(fullPath) ?? baseDir : ResolvePath(tool.WorkingDirectory);
            try { Process.Start(new ProcessStartInfo { FileName = fullPath, Arguments = tool.Arguments ?? "", WorkingDirectory = workDir, UseShellExecute = true, Verb = tool.RunAsAdmin ? "runas" : "" }); }
            catch (Exception ex) { MessageBox.Show("启动失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void ApplyTheme()
        {
            this.BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248);
            contentHost.BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248);

            descLabel.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235);
            descLabel.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(60, 60, 60);

            leftPanel.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235);
            categoryList.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235);
            categoryList.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(60, 60, 60);

            versionLabel.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(235, 235, 235);
            versionLabel.ForeColor = IsDarkMode ? Color.FromArgb(160, 160, 160) : Color.FromArgb(140, 140, 140);

            phoneModeButton!.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.FromArgb(225, 225, 225);
            phoneModeButton.ForeColor = IsDarkMode ? Color.FromArgb(0, 153, 255) : Color.FromArgb(0, 102, 204);
            phoneModeButton.FlatAppearance.BorderColor = phoneModeButton.BackColor;

            toolPanel.BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 245, 245);
            hardwarePanel.BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248);
            hardwareToolbar.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 251, 252);
            hardwareScrollPanel.BackColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248);

            if (hardwareTitleLabel != null)
                hardwareTitleLabel.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(50, 50, 50);

            if (hardwareScrollPanel.IsHandleCreated)
                SetWindowTheme(hardwareScrollPanel.Handle, IsDarkMode ? "DarkMode_Explorer" : "Explorer", null);

            if (closeButton != null) { closeButton.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 250, 250); closeButton.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(80, 80, 80); }
            if (maximizeButton != null) { maximizeButton.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 250, 250); maximizeButton.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(80, 80, 80); }
            if (minimizeButton != null) { minimizeButton.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 250, 250); minimizeButton.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(80, 80, 80); }
            if (settingsButton != null) { settingsButton.BackColor = IsDarkMode ? Color.FromArgb(37, 37, 38) : Color.FromArgb(250, 250, 250); settingsButton.ForeColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(80, 80, 80); }

            if (refreshButton != null) { refreshButton.BackColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White; refreshButton.ForeColor = IsDarkMode ? Color.White : Color.FromArgb(60, 60, 60); refreshButton.FlatAppearance.BorderColor = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(200, 200, 200); }

            ApplyThemeToGjPanels();

            this.Invalidate();
            RefreshCurrentView();
        }

        private void RefreshCurrentView()
        {
            if (isGjModeActive)
            {
                string? selected = categoryList.SelectedItem as string;
                if (selected == null) return;
                HideAllGjPanels();
                switch (selected)
                {
                    case "📊 仪表盘": gjDashboardPanel.Visible = true; break;
                    case "🧰 刷机匣": gjFlashToolPanel.Visible = true; break;
                    case "📲 投屏": gjScrcpyPanel.Visible = true; break;
                }
                return;
            }

            string? currentCat = categoryList.SelectedItem as string;
            if (currentCat == null) return;

            if (currentCat == HardwareInfoCategory) 
            {
                ShowHardwarePanel();
                if (currentHardwareCards != null) RenderHardwareCards(currentHardwareCards);
            }
            else ShowToolPanel(currentCat);
        }

        private void ApplyThemeToGjPanels()
        {
            Color bgColor = IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.FromArgb(245, 246, 248);
            Color cardBg = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White;
            Color cardBorder = IsDarkMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(220, 222, 226);
            Color titleColor = IsDarkMode ? Color.FromArgb(240, 240, 240) : Color.FromArgb(50, 50, 50);
            Color tipColor = IsDarkMode ? Color.Gray : Color.Gray;

            gjDashboardPanel.BackColor = bgColor;
            gjFlashToolPanel.BackColor = bgColor;
            gjScrcpyPanel.BackColor = bgColor;

            var panels = new[] { welcomeCard, flashCard, scrcpyCard };
            foreach (var p in panels)
            {
                if (p == null) continue;
                p.BackColor = cardBg;
                p.BorderColor = cardBorder;
                
                foreach (Control c in p.Controls)
                {
                    if (c is Label lbl)
                    {
                        lbl.ForeColor = lbl.Font.Bold ? titleColor : tipColor;
                    }
                }
            }
        }
    }
}