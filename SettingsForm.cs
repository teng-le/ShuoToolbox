using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Toolbox
{
    public class SettingsForm : Form
    {
        private const int TitleBarHeight = 36;
        private const int WindowCornerRadius = 6;
        private AppSettings settings;

        private Panel contentHost = null!;
        private Panel generalPanel = null!;
        private Panel updatePanel = null!;
        
        private Button btnNavGeneral = null!;
        private Button btnNavUpdate = null!;

        private ComboBox themeCombo = null!;
        private CheckBox autoStartCheck = null!;
        private CheckBox startMinCheck = null!;
        
        private Button saveBtn = null!;
        private Button cancelBtn = null!;

        public SettingsForm()
        {
            settings = AppSettings.Load();
            InitializeUI();
            LoadSettingsToUI();
        }

        private void InitializeUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(600, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 250, 250);
            this.DoubleBuffered = true;

            // ==========================================
            // 核心修复：严格按照从底层到顶层的顺序添加控件
            // ==========================================

            // 1. 先加填充区（Fill）
            contentHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(250, 250, 250) };
            this.Controls.Add(contentHost);

            // 2. 加左侧导航栏（Left）
            var leftNav = new Panel { Dock = DockStyle.Left, Width = 140, BackColor = Color.FromArgb(245, 245, 245) };
            var leftLine = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.FromArgb(230, 230, 230) };
            leftNav.Controls.Add(leftLine);

            btnNavGeneral = CreateNavButton("常规设置", 10);
            btnNavUpdate = CreateNavButton("检查更新", 55);
            btnNavGeneral.Click += (s, e) => SwitchPage("General");
            btnNavUpdate.Click += (s, e) => SwitchPage("Update");
            leftNav.Controls.Add(btnNavGeneral);
            leftNav.Controls.Add(btnNavUpdate);
            this.Controls.Add(leftNav);

            // 3. 加底部按钮栏（Bottom）
            var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(250, 250, 250) };
            var bottomLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 220, 220) };
            bottomBar.Controls.Add(bottomLine);

            cancelBtn = new Button 
            { 
                Text = "取消", 
                Size = new Size(80, 32), 
                Location = new Point(bottomBar.Width - 200, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(240, 240, 240), 
                ForeColor = Color.FromArgb(60, 60, 60), 
                Font = new Font("微软雅黑", 9.5F),
                DialogResult = DialogResult.Cancel // 保证底层关闭
            };
            cancelBtn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            cancelBtn.FlatAppearance.BorderSize = 1;
            
            saveBtn = new Button 
            { 
                Text = "保存", 
                Size = new Size(80, 32), 
                Location = new Point(bottomBar.Width - 110, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(0, 120, 215), 
                ForeColor = Color.White, 
                Font = new Font("微软雅黑", 9.5F) 
            };
            saveBtn.FlatAppearance.BorderSize = 0;
            saveBtn.Click += SaveBtn_Click;

            bottomBar.Controls.Add(cancelBtn);
            bottomBar.Controls.Add(saveBtn);
            this.Controls.Add(bottomBar);

            // 4. 加标题栏（Top）
            var titlePanel = new Panel { Dock = DockStyle.Top, Height = TitleBarHeight, BackColor = Color.FromArgb(240, 240, 240) };
            var titleLabel = new Label { Text = "设置", Font = new Font("微软雅黑", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), AutoSize = true, Location = new Point(16, 9), BackColor = Color.Transparent };
            titlePanel.Controls.Add(titleLabel);

            var closeBtn = new Button { Text = "✕", FlatStyle = FlatStyle.Flat, Size = new Size(40, TitleBarHeight), Location = new Point(titlePanel.Width - 40, 0), Anchor = AnchorStyles.Top | AnchorStyles.Right, ForeColor = Color.FromArgb(80, 80, 80), BackColor = Color.FromArgb(240, 240, 240), TabStop = false, DialogResult = DialogResult.Cancel };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.MouseEnter += (s, e) => { closeBtn.BackColor = Color.FromArgb(232, 17, 35); closeBtn.ForeColor = Color.White; };
            closeBtn.MouseLeave += (s, e) => { closeBtn.BackColor = Color.FromArgb(240, 240, 240); closeBtn.ForeColor = Color.FromArgb(80, 80, 80); };
            titlePanel.Controls.Add(closeBtn);

            var titleLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220, 220, 220) };
            titlePanel.Controls.Add(titleLine);

            titlePanel.MouseDown += TitleBar_MouseDown;
            titleLabel.MouseDown += TitleBar_MouseDown;
            titleLine.MouseDown += TitleBar_MouseDown;
            this.Controls.Add(titlePanel);

            // 构建具体页面
            BuildGeneralPanel();
            BuildUpdatePanel();

            this.Load += (s, e) => 
            {
                ApplyWindowRegion();
                SwitchPage("General"); // 默认显示常规设置
            };
            this.Resize += (s, e) => ApplyWindowRegion();
        }

        private void BuildGeneralPanel()
        {
            generalPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), Visible = false };
            
            var lblTheme = new Label { Text = "界面主题:", Location = new Point(20, 30), AutoSize = true, Font = new Font("微软雅黑", 9.5F), ForeColor = Color.FromArgb(140, 140, 140) };
            themeCombo = new ComboBox { Location = new Point(120, 27), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("微软雅黑", 9.5F), Enabled = false };
            themeCombo.Items.AddRange(new object[] { "亮色 (开发中)" });
            
            autoStartCheck = new CheckBox { Text = "开机自动启动", Location = new Point(20, 80), AutoSize = true, Font = new Font("微软雅黑", 9.5F) };
            
            startMinCheck = new CheckBox { Text = "启用系统托盘 (关闭时最小化到托盘)", Location = new Point(20, 120), AutoSize = true, Font = new Font("微软雅黑", 9.5F) };
            var lblTrayTip = new Label { Text = "注：开启/关闭托盘需重启软件生效", Location = new Point(40, 145), AutoSize = true, Font = new Font("微软雅黑", 8.5F), ForeColor = Color.Gray };

            generalPanel.Controls.AddRange(new Control[] { lblTheme, themeCombo, autoStartCheck, startMinCheck, lblTrayTip });
        }

        private void BuildUpdatePanel()
        {
            updatePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), Visible = false };
            
            var titleUpdate = new Label { Text = "检查更新", Font = new Font("微软雅黑", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(50, 50, 50), AutoSize = true, Location = new Point(20, 30) };
            var curVersion = new Label { Text = "当前版本：v0.4.0", Font = new Font("微软雅黑", 10F), ForeColor = Color.FromArgb(100, 100, 100), AutoSize = true, Location = new Point(20, 80) };
            
            var btnCheck = new Button
            {
                Text = "立即检查更新",
                Size = new Size(120, 36),
                Location = new Point(20, 120),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 10F)
            };
            btnCheck.FlatAppearance.BorderSize = 0;
            btnCheck.Click += (s, e) => MessageBox.Show("本次检查更新只是一个概念，目前正在搭建中，敬请期待！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            updatePanel.Controls.AddRange(new Control[] { titleUpdate, curVersion, btnCheck });
        }

        private Button CreateNavButton(string text, int top)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(140, 45),
                Location = new Point(0, top),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 245, 245),
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("微软雅黑", 10F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SwitchPage(string pageName)
        {
            // 重置导航按钮状态
            btnNavGeneral.BackColor = Color.FromArgb(245, 245, 245);
            btnNavGeneral.ForeColor = Color.FromArgb(60, 60, 60);
            btnNavUpdate.BackColor = Color.FromArgb(245, 245, 245);
            btnNavUpdate.ForeColor = Color.FromArgb(60, 60, 60);

            // 清空内容区
            contentHost.Controls.Clear();

            if (pageName == "General")
            {
                btnNavGeneral.BackColor = Color.FromArgb(250, 250, 250);
                btnNavGeneral.ForeColor = Color.FromArgb(0, 120, 215);
                
                generalPanel.Visible = true;
                contentHost.Controls.Add(generalPanel);
            }
            else if (pageName == "Update")
            {
                btnNavUpdate.BackColor = Color.FromArgb(250, 250, 250);
                btnNavUpdate.ForeColor = Color.FromArgb(0, 120, 215);
                
                updatePanel.Visible = true;
                contentHost.Controls.Add(updatePanel);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 如果是保存点出去的，直接放行
            if (this.DialogResult == DialogResult.OK)
            {
                base.OnFormClosing(e);
                return;
            }

            // 只有当用户在【常规设置】页，且修改了东西，才拦截
            if (contentHost.Controls.Contains(generalPanel) && HasUnsavedChanges())
            {
                var result = MessageBox.Show("您有未保存的修改，确定要放弃吗？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    e.Cancel = true; // 拦截，留在页面
                    return;
                }
            }
            
            // 确认放弃，赋予 Cancel 状态放行
            this.DialogResult = DialogResult.Cancel;
            base.OnFormClosing(e);
        }

        private bool HasUnsavedChanges()
        {
            return autoStartCheck.Checked != settings.AutoStart ||
                   startMinCheck.Checked != settings.StartMinimized;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
        }

        private void ApplyWindowRegion()
        {
            if (!IsHandleCreated) return;
            var rgn = NativeMethods.CreateRoundRectRgn(0, 0, Width + 1, Height + 1, WindowCornerRadius * 2, WindowCornerRadius * 2);
            if (rgn != IntPtr.Zero) NativeMethods.SetWindowRgn(Handle, rgn, true);
        }

        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        private void LoadSettingsToUI()
        {
            themeCombo.SelectedIndex = 0;
            autoStartCheck.Checked = settings.AutoStart;
            startMinCheck.Checked = settings.StartMinimized;
        }

        private void SaveBtn_Click(object? sender, EventArgs e)
        {
            settings.AutoStart = autoStartCheck.Checked;
            settings.StartMinimized = startMinCheck.Checked;
            
            settings.Save();

            try
            {
                string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
                using var key = Registry.CurrentUser.OpenSubKey(runKey, true);
                if (key != null)
                {
                    if (settings.AutoStart)
                        key.SetValue("ShuoToolbox", Application.ExecutablePath);
                    else
                        key.DeleteValue("ShuoToolbox", false);
                }
            }
            catch { }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}