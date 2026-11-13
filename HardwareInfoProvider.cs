using System;
using System.Collections.Generic;
using System.Management;

namespace Toolbox
{
    public static class HardwareInfoProvider
    {
        public static List<HardwareCard> BuildHardwareCards()
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

        private static List<KeyValuePair<string, string>> BuildMemoryItems()
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

        private static List<KeyValuePair<string, string>> BuildVideoItems()
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

        private static List<KeyValuePair<string, string>> BuildMonitorItems()
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

        private static List<KeyValuePair<string, string>> BuildDiskItems()
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

        private static List<KeyValuePair<string, string>> BuildSoundItems()
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

        private static List<KeyValuePair<string, string>> BuildNetworkItems()
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

        private static string QuerySingle(string wmiClass, string property)
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

        private static string GetUptime()
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
    }
}