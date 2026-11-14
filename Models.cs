using System.Collections.Generic;

namespace Toolbox
{
    public class ToolItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Path { get; set; } = "";
        public string Arguments { get; set; } = "";
        public string WorkingDirectory { get; set; } = "";
        public bool RunAsAdmin { get; set; } = false;
    }

    public class CategoryItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
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
}