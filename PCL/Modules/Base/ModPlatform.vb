''' <summary>
''' 跨平台识别：Windows、Linux、macOS 与 BSD（FreeBSD / OpenBSD / NetBSD / DragonFly）。
''' 所有判断仅依赖运行时信息，在非目标平台上不会抛出异常。
''' </summary>
Public Module ModPlatform

    ''' <summary>
    ''' 当前是否为 Windows。
    ''' </summary>
    Public ReadOnly Property IsWindows As Boolean
        Get
            Return OperatingSystem.IsWindows()
        End Get
    End Property

    ''' <summary>
    ''' 当前是否为 Linux。
    ''' </summary>
    Public ReadOnly Property IsLinux As Boolean
        Get
            Return OperatingSystem.IsLinux()
        End Get
    End Property

    ''' <summary>
    ''' 当前是否为 macOS。
    ''' </summary>
    Public ReadOnly Property IsMacOS As Boolean
        Get
            Return OperatingSystem.IsMacOS()
        End Get
    End Property

    Private _IsBSD As Boolean? = Nothing

    ''' <summary>
    ''' 当前是否为 BSD 系列系统。.NET 只内置了 FreeBSD 判断，
    ''' 其余 BSD 通过运行时描述文本识别。
    ''' </summary>
    Public ReadOnly Property IsBSD As Boolean
        Get
            If _IsBSD Is Nothing Then _IsBSD = DetectBsd()
            Return _IsBSD.Value
        End Get
    End Property

    Private Function DetectBsd() As Boolean
        Try
            If OperatingSystem.IsWindows() OrElse OperatingSystem.IsLinux() OrElse OperatingSystem.IsMacOS() Then Return False
            If OperatingSystem.IsFreeBSD() Then Return True
            Dim Description As String = System.Runtime.InteropServices.RuntimeInformation.OSDescription
            If String.IsNullOrWhiteSpace(Description) Then Return False
            Return Description.IndexOf("BSD", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                   Description.IndexOf("DragonFly", StringComparison.OrdinalIgnoreCase) >= 0
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' BSD 的具体分支名称，例如 FreeBSD、OpenBSD、NetBSD；不是 BSD 时返回 Nothing。
    ''' </summary>
    Public ReadOnly Property BsdFlavor As String
        Get
            If Not IsBSD Then Return Nothing
            Try
                If OperatingSystem.IsFreeBSD() Then Return "FreeBSD"
                Dim Description As String = System.Runtime.InteropServices.RuntimeInformation.OSDescription
                For Each Name In {"FreeBSD", "OpenBSD", "NetBSD", "DragonFly"}
                    If Description.IndexOf(Name, StringComparison.OrdinalIgnoreCase) >= 0 Then Return Name
                Next
            Catch
            End Try
            Return "BSD"
        End Get
    End Property

    ''' <summary>
    ''' Windows 专属：内存优化（写入系统内存管理策略）。其他平台（含 Linux / macOS / BSD）禁用。
    ''' </summary>
    Public ReadOnly Property SupportsRamOptimize As Boolean
        Get
            Return IsWindows
        End Get
    End Property

    ''' <summary>
    ''' Windows 专属：Java Launch Wrapper。其他平台（含 Linux / macOS / BSD）禁用。
    ''' </summary>
    Public ReadOnly Property SupportsJavaLaunchWrapper As Boolean
        Get
            Return IsWindows
        End Get
    End Property

    ''' <summary>
    ''' 显卡性能首选项：Windows 调整系统首选项，Linux 通过 PRIME 环境变量实现。
    ''' macOS 与 BSD 没有等价机制，界面中禁用该设置。
    ''' </summary>
    Public ReadOnly Property SupportsGpuPreference As Boolean
        Get
            Return IsWindows OrElse IsLinux
        End Get
    End Property

    ''' <summary>
    ''' 是否允许自定义主题配色。Windows 版按《PCL 存储库合理使用指南》固定龙猫蓝，
    ''' Linux / macOS / BSD 可以自定义。
    ''' </summary>
    Public ReadOnly Property CanCustomizeTheme As Boolean
        Get
            Return Not IsWindows
        End Get
    End Property

    ''' <summary>
    ''' 是否为类 Unix 系统（除 Windows 外的所有平台）。
    ''' </summary>
    Public ReadOnly Property IsUnixLike As Boolean
        Get
            Return Not IsWindows
        End Get
    End Property

    ''' <summary>
    ''' 平台名称，用于日志与界面展示：windows / linux / macos / freebsd 等。
    ''' </summary>
    Public ReadOnly Property PlatformName As String
        Get
            If IsWindows Then Return "windows"
            If IsMacOS Then Return "macos"
            If IsLinux Then Return "linux"
            If IsBSD Then Return BsdFlavor.ToLowerInvariant()
            Return "unknown"
        End Get
    End Property

    ''' <summary>
    ''' Minecraft JSON 规则使用的操作系统名称。Minecraft 只认识 windows / osx / linux，
    ''' 因此 BSD 与 Linux 共用 linux 规则。
    ''' </summary>
    Public ReadOnly Property MinecraftOsName As String
        Get
            If IsWindows Then Return "windows"
            If IsMacOS Then Return "osx"
            Return "linux"
        End Get
    End Property

    ''' <summary>
    ''' 用户配置根目录：macOS 使用 ~/Library/Application Support，其他类 Unix 使用 XDG 配置目录。
    ''' Windows 返回 ApplicationData。
    ''' </summary>
    Public ReadOnly Property UserConfigHome As String
        Get
            If IsWindows Then
                Return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            End If
            If IsMacOS Then
                Dim Home As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                If String.IsNullOrWhiteSpace(Home) Then Return Nothing
                Return IO.Path.Combine(Home, "Library", "Application Support")
            End If
            Dim Xdg As String = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
            If String.IsNullOrWhiteSpace(Xdg) Then
                Dim Home As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                If String.IsNullOrWhiteSpace(Home) Then Return Nothing
                Xdg = IO.Path.Combine(Home, ".config")
            End If
            Return Xdg
        End Get
    End Property

End Module
