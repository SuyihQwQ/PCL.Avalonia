// SPDX-License-Identifier: Apache-2.0
using System.Runtime.InteropServices;

namespace MeloongCore;

/// <summary>
/// 跨平台的物理内存信息。
/// </summary>
public static class SystemInfo {

    /// <summary>
    /// 物理内存总量（字节）。
    /// </summary>
    public static long TotalPhysicalMemory => GetTotalPhysicalMemory();

    /// <summary>
    /// 当前可用的物理内存（字节）。
    /// </summary>
    public static long AvailablePhysicalMemory => GetAvailablePhysicalMemory();

    private static long? _TotalPhysicalMemory;

    private static long GetTotalPhysicalMemory() {
        _TotalPhysicalMemory ??= QueryMemory().Total;
        return _TotalPhysicalMemory.Value;
    }

    private static long GetAvailablePhysicalMemory() => QueryMemory().Available;

    private static (long Total, long Available) QueryMemory() {
        try {
            if (OperatingSystem.IsWindows()) {
                return QueryWindowsMemory();
            }
            if (OperatingSystem.IsLinux()) {
                return QueryLinuxMemory();
            }
            if (OperatingSystem.IsMacOS()) {
                return QueryMacMemory();
            }
        } catch {
            // 忽略平台查询失败，退回 GC 估算
        }
        var info = GC.GetGCMemoryInfo();
        long total = info.TotalAvailableMemoryBytes;
        long used = info.MemoryLoadBytes;
        return (total, Math.Max(total - used, 0));
    }

    #region Windows

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    private static (long Total, long Available) QueryWindowsMemory() {
        var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref status)) {
            throw new InvalidOperationException("GlobalMemoryStatusEx 调用失败");
        }
        return ((long)status.ullTotalPhys, (long)status.ullAvailPhys);
    }

    #endregion

    #region Linux

    private static (long Total, long Available) QueryLinuxMemory() {
        long total = 0, available = 0, free = 0, buffers = 0, cached = 0;
        foreach (var line in File.ReadLines("/proc/meminfo")) {
            var parts = line.Split(':', 2);
            if (parts.Length != 2) continue;
            var valueText = parts[1].Trim().Split(' ')[0];
            if (!long.TryParse(valueText, out var value)) continue;
            value *= 1024;
            switch (parts[0]) {
                case "MemTotal": total = value; break;
                case "MemAvailable": available = value; break;
                case "MemFree": free = value; break;
                case "Buffers": buffers = value; break;
                case "Cached": cached = value; break;
            }
        }
        if (total <= 0) throw new InvalidOperationException("无法读取 /proc/meminfo");
        if (available <= 0) available = free + buffers + cached;
        return (total, Math.Clamp(available, 0, total));
    }

    #endregion

    #region macOS

    [DllImport("libSystem.dylib")]
    private static extern int sysctlbyname(string name, out long oldp, ref nuint oldlenp, IntPtr newp, nuint newlen);

    private static (long Total, long Available) QueryMacMemory() {
        nuint size = sizeof(long);
        if (sysctlbyname("hw.memsize", out var total, ref size, IntPtr.Zero, 0) != 0 || total <= 0) {
            throw new InvalidOperationException("无法读取 hw.memsize");
        }
        var info = GC.GetGCMemoryInfo();
        long available = info.TotalAvailableMemoryBytes > 0
            ? Math.Max(info.TotalAvailableMemoryBytes - info.MemoryLoadBytes, 0)
            : total;
        return (total, Math.Min(available, total));
    }

    #endregion

}
