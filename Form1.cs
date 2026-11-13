using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace Toolbox
{
    public class ToolItem
    {
        public string Name { get; set; } = "";
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

    public partial class Form1 : Form
    {
        private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string configPath;

        private FlowLayoutPanel toolPanel;
        private ListBox categoryList;
        private ToolboxConfig config = new();

        public Form1()
        {
            InitializeComponent();

            configPath = Path.Combine(baseDir, "tools.json");

            this.Text = "我的工具箱";
            this.Size = new Size(760, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            toolPanel = new FlowLayoutPanel();
            toolPanel.Dock = DockStyle.Fill;
            toolPanel.Padding = new Padding(12);
            toolPanel.AutoScroll = true;
            toolPanel.BackColor = Color.FromArgb(245, 245, 245);
            this.Controls.Add(toolPanel);

            categoryList = new ListBox();
            categoryList.Dock = DockStyle.Left;
            categoryList.Width = 170;
            categoryList.BorderStyle = BorderStyle.None;
            categoryList.Font = new Font("微软雅黑", 10.5F);
            categoryList.BackColor = Color.FromArgb(235, 235, 235);
            categoryList.SelectedIndexChanged += CategoryList_SelectedIndexChanged;
            this.Controls.Add(categoryList);

            LoadConfig();
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
            toolPanel.Controls.Clear();

            string? categoryName = categoryList.SelectedItem as string;
            if (categoryName == null) return;

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
                catch
                {
                }

                string path = tool.Path;
                btn.Click += (s, e) => Launch(path);
                toolPanel.Controls.Add(btn);
            }
        }

        private string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path)) return path;

            string toolboxPath = Path.Combine(baseDir, path);
            if (File.Exists(toolboxPath)) return toolboxPath;

            string sysPath = Path.Combine(Environment.SystemDirectory, path);
            if (File.Exists(sysPath)) return sysPath;

            return path;
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
                MessageBox.Show("找不到工具文件：" + fullPath,
                    "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}