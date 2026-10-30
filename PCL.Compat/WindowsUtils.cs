using System.Security.Principal;

namespace MeloongCore;

public static class WindowsUtils {

    /// <summary>
    /// 判断当前进程是否以管理员身份运行。非 Windows 平台恒为 <c>false</c>。
    /// </summary>
    public static bool HasAdminRole() {
        if (!OperatingSystem.IsWindows()) return false;
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

}
