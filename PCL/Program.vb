Imports System.Text
Imports Avalonia
Imports Avalonia.Media

Public Module Program

    <STAThread>
    Public Sub Main(args As String())
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        MeloongCore.Main.Init("PCL.Avalonia")
        Dim RemainingArgs = ParseWorkingDirectory(args)
        Try
            EnsureDataPath()
        Catch ex As Exception
            ShowStartupError(GetDefaultPathData(), ex)
            Environment.Exit(4)
        End Try
        Logger.Instance = New FileLogger With {
            .logFolder = PathData,
            .MinLevel = If(ModeDebug, LogLevel.Trace, LogLevel.Info)
        }
        If Not OperatingSystem.IsWindows() Then
            Try
                DirectoryUtils.Create(PathConfigFolder)
            Catch ex As Exception
                Logger.Warn(ex, "创建用户配置文件夹失败")
            End Try
        End If
        ApplyLinuxCursorTheme()
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(RemainingArgs)
    End Sub

    ''' <summary>
    ''' 解析 --working-dir，覆盖配置文件夹与默认 .minecraft 的基础目录。
    ''' </summary>
    Private Function ParseWorkingDirectory(args As String()) As String()
        Dim Remaining As New List(Of String)
        Dim Index As Integer = 0
        While Index < args.Length
            Dim Arg As String = args(Index)
            Dim Value As String = Nothing
            If Arg = "--working-dir" Then
                If Index + 1 >= args.Length Then
                    ShowStartupMessage("启动参数 --working-dir 缺少路径。", "启动参数错误")
                    Environment.Exit(4)
                End If
                Value = args(Index + 1)
                Index += 2
            ElseIf Arg.StartsWith("--working-dir=", StringComparison.Ordinal) Then
                Value = Arg.Substring("--working-dir=".Length)
                Index += 1
            Else
                Remaining.Add(Arg)
                Index += 1
                Continue While
            End If
            If String.IsNullOrWhiteSpace(Value) Then
                ShowStartupMessage("启动参数 --working-dir 缺少路径。", "启动参数错误")
                Environment.Exit(4)
            End If
            Try
                WorkingDirectoryOverride = PathUtils.AddSlashSuffix(IO.Path.GetFullPath(Value))
            Catch ex As Exception
                ShowStartupMessage($"启动参数 --working-dir 无效（{Value}）：{ex.Message}", "启动参数错误")
                Environment.Exit(4)
            End Try
        End While
        Return Remaining.ToArray()
    End Function

    ''' <summary>
    ''' 配置文件夹不可写时显示与原版一致的错误提示，随后由调用方退出程序。
    ''' </summary>
    Private Sub ShowStartupError(Folder As String, ex As Exception)
        Dim Text As String = $"PCL 没有对当前文件夹的权限（{Folder}），请尝试：" & vbLf &
            "1. 将 PCL 移动到其他文件夹。" & vbLf &
            $"2. 删除当前目录中的 PCL.Avalonia 文件夹，然后再试。"
        If OperatingSystem.IsWindows() Then
            Text &= vbLf & "3. 右键 PCL 选择属性，打开 兼容性 中的 以管理员身份运行此程序。"
        Else
            Text &= vbLf & "3. 检查当前用户对该文件夹是否有写入权限。"
        End If
        Console.Error.WriteLine(ex)
        ShowStartupMessage(Text, "运行环境错误")
    End Sub

    ''' <summary>
    ''' 在 Avalonia 启动前显示错误提示；Windows 使用系统消息框，Linux 使用 zenity / kdialog。
    ''' </summary>
    Private Sub ShowStartupMessage(Text As String, Title As String)
        Console.Error.WriteLine(Text)
        If OperatingSystem.IsWindows() Then
            MessageBoxW(IntPtr.Zero, Text, Title, &H10UI Or &H10000UI)
        ElseIf OperatingSystem.IsLinux() Then
            ShowLinuxStartupMessage(Text, Title)
        End If
    End Sub

    ''' <summary>
    ''' Linux 没有系统级消息框，优先尝试桌面环境自带的 zenity / kdialog。
    ''' </summary>
    Private Sub ShowLinuxStartupMessage(Text As String, Title As String)
        For Each Tool As String In {"zenity", "kdialog"}
            Try
                Dim Info As New ProcessStartInfo(Tool) With {
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }
                If Tool = "zenity" Then
                    Info.ArgumentList.Add("--error")
                    Info.ArgumentList.Add("--title=" & Title)
                    Info.ArgumentList.Add("--text=" & Text)
                Else
                    Info.ArgumentList.Add("--title")
                    Info.ArgumentList.Add(Title)
                    Info.ArgumentList.Add("--error")
                    Info.ArgumentList.Add(Text)
                End If
                Using Proc = Process.Start(Info)
                    If Proc Is Nothing Then Continue For
                    Proc.WaitForExit()
                    If Proc.ExitCode = 0 Then Return
                End Using
            Catch
            End Try
        Next
    End Sub

    <Runtime.InteropServices.DllImport("user32.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Function MessageBoxW(hWnd As IntPtr, Text As String, Caption As String, Flags As UInteger) As Integer
    End Function

    Public Function BuildAvaloniaApp() As AppBuilder
        Dim Builder = AppBuilder.Configure(Of App)() _
            .UsePlatformDetect()
        'Skia 在某些 Linux 系统上会把无法渲染的位图字体（如 8514fix）当作默认字体，
        '未显式指定字体的控件一旦提前排版就会抛出 glyphTypeface 异常，因此固定使用 PCL 字体
        Builder = Builder.With(New FontManagerOptions With {
            .DefaultFamilyName = "avares://PCL.Avalonia/Assets/Fonts/PCLEnglish.ttf#PCL English, Noto Sans CJK SC, MiSans TC"
        })
        'Wayland 下禁止协商服务端标题栏，避免启动时先闪出系统标题栏再被移除
        If OperatingSystem.IsLinux() Then
            Builder = Builder _
                .With(New WaylandPlatformOptions With {.ForceDrawnDecorations = True}) _
                .With(New X11PlatformOptions With {.WmClass = "PCL"})
        End If
        Return Builder _
            .UseWaylandWithFallback() _
            .LogToTrace()
    End Function

#Region "系统光标"

    ''' <summary>
    ''' Wayland 下 Avalonia 固定以 "default" 主题加载光标，与系统设置不一致。
    ''' 启动前从 GTK、KDE、GSettings 配置中读取光标主题，生成一个继承该主题的 default 主题覆盖，
    ''' 并通过 XCURSOR_PATH 让 libwayland-cursor 优先读取它。
    ''' </summary>
    Private Sub ApplyLinuxCursorTheme()
        If Not OperatingSystem.IsLinux() Then Return
        Try
            Dim ThemeName = Environment.GetEnvironmentVariable("XCURSOR_THEME")
            Dim ThemeSize = Environment.GetEnvironmentVariable("XCURSOR_SIZE")
            If String.IsNullOrWhiteSpace(ThemeName) Then ThemeName = ReadGtkCursorSetting("gtk-cursor-theme-name")
            If String.IsNullOrWhiteSpace(ThemeSize) Then ThemeSize = ReadGtkCursorSetting("gtk-cursor-theme-size")
            If String.IsNullOrWhiteSpace(ThemeName) Then ThemeName = ReadKdeCursorSetting("cursorTheme")
            If String.IsNullOrWhiteSpace(ThemeSize) Then ThemeSize = ReadKdeCursorSetting("cursorSize")
            If String.IsNullOrWhiteSpace(ThemeName) Then ThemeName = ReadGSettings("org.gnome.desktop.interface", "cursor-theme")
            If String.IsNullOrWhiteSpace(ThemeSize) Then ThemeSize = ReadGSettings("org.gnome.desktop.interface", "cursor-size")
            If String.IsNullOrWhiteSpace(ThemeName) Then Return
            If Not String.IsNullOrWhiteSpace(ThemeSize) Then SetProcessEnvironment("XCURSOR_SIZE", ThemeSize)

            If Not ThemeName.Equals("default", StringComparison.OrdinalIgnoreCase) Then
                Dim OverrideFolder = IO.Path.Combine(PathData, "CursorTheme")
                IO.Directory.CreateDirectory(IO.Path.Combine(OverrideFolder, "default"))
                IO.File.WriteAllText(IO.Path.Combine(OverrideFolder, "default", "index.theme"),
                    $"[Icon Theme]{vbLf}Name=Default{vbLf}Inherits={ThemeName}{vbLf}")
                SetProcessEnvironment("XCURSOR_PATH", OverrideFolder & ":" & CursorSearchPaths)
            End If
            Logger.Info($"已应用系统光标设置：{ThemeName}，{If(ThemeSize, "默认")}px")
        Catch ex As Exception
            Logger.Warn(ex, "读取系统光标设置失败")
        End Try
    End Sub

    ''' <summary>
    ''' libwayland-cursor 1.26 在未设置 XCURSOR_PATH 时使用的默认搜索路径。
    ''' </summary>
    Private ReadOnly Property CursorSearchPaths As String
        Get
            Dim Existing = Environment.GetEnvironmentVariable("XCURSOR_PATH")
            If Not String.IsNullOrWhiteSpace(Existing) Then Return Existing
            Dim DataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
            If String.IsNullOrWhiteSpace(DataHome) Then DataHome = IO.Path.Combine(UserProfile, ".local", "share")
            Return String.Join(":",
                IO.Path.Combine(DataHome, "icons"),
                IO.Path.Combine(UserProfile, ".icons"),
                "/usr/share/icons",
                "/usr/share/pixmaps",
                IO.Path.Combine(UserProfile, ".cursors"),
                "/usr/share/cursors/xorg-x11")
        End Get
    End Property

    ''' <summary>
    ''' 同时更新托管环境变量与原生 environ。
    ''' .NET 在 Unix 上仅维护托管副本，原生库通过 getenv 读不到。
    ''' </summary>
    Private Sub SetProcessEnvironment(Name As String, Value As String)
        Environment.SetEnvironmentVariable(Name, Value)
        If OperatingSystem.IsLinux() OrElse OperatingSystem.IsMacOS() Then
            NativeSetenv(Name, Value, 1)
        End If
    End Sub

    <Runtime.InteropServices.DllImport("libc", EntryPoint:="setenv", CharSet:=Runtime.InteropServices.CharSet.Ansi, SetLastError:=True)>
    Private Function NativeSetenv(Name As String, Value As String, Overwrite As Integer) As Integer
    End Function

    Private Function ReadGtkCursorSetting(Key As String) As String
        For Each FileName In {"gtk-4.0/settings.ini", "gtk-3.0/settings.ini"}
            Dim Value = ReadIniValue(Path.Combine(ConfigHome, FileName), "Settings", Key)
            If Not String.IsNullOrWhiteSpace(Value) Then Return Value.Trim(""""c)
        Next
        Return Nothing
    End Function

    Private Function ReadKdeCursorSetting(Key As String) As String
        Return ReadIniValue(Path.Combine(ConfigHome, "kcminputrc"), "Mouse", Key)
    End Function

    Private ReadOnly Property UserProfile As String
        Get
            Return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        End Get
    End Property

    Private ReadOnly Property ConfigHome As String
        Get
            Dim Path = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
            If String.IsNullOrWhiteSpace(Path) Then Path = IO.Path.Combine(UserProfile, ".config")
            Return Path
        End Get
    End Property

    Private Function ReadIniValue(FilePath As String, Section As String, Key As String) As String
        If Not IO.File.Exists(FilePath) Then Return Nothing
        Dim InSection As Boolean = False
        For Each Line In IO.File.ReadLines(FilePath)
            Dim Trimmed = Line.Trim()
            If Trimmed.StartsWith("[") AndAlso Trimmed.EndsWith("]") Then
                InSection = Trimmed.Substring(1, Trimmed.Length - 2).Trim().Equals(Section, StringComparison.OrdinalIgnoreCase)
                Continue For
            End If
            If Not InSection OrElse Trimmed.StartsWith(";") OrElse Trimmed.StartsWith("#") Then Continue For
            Dim Index = Trimmed.IndexOf("="c)
            If Index <= 0 Then Continue For
            If Trimmed.Substring(0, Index).Trim().Equals(Key, StringComparison.OrdinalIgnoreCase) Then
                Return Trimmed.Substring(Index + 1).Trim()
            End If
        Next
        Return Nothing
    End Function

    Private Function ReadGSettings(Schema As String, Key As String) As String
        Try
            Using Proc = Process.Start(New ProcessStartInfo("gsettings") With {
                .Arguments = $"get {Schema} {Key}",
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True})
                If Proc Is Nothing Then Return Nothing
                If Not Proc.WaitForExit(1500) Then
                    Try
                        Proc.Kill()
                    Catch
                    End Try
                    Return Nothing
                End If
                Dim Output = Proc.StandardOutput.ReadToEnd().Trim().Trim("'"c)
                If Proc.ExitCode = 0 AndAlso Output <> "" Then Return Output
            End Using
        Catch
        End Try
        Return Nothing
    End Function

#End Region

#Region "桌面集成"

    ''' <summary>
    ''' Wayland 下窗口不携带图标，合成器按 AppId 匹配已安装的 .desktop 文件来显示任务栏图标。
    ''' 启动时在用户数据目录写入一份桌面项与图标，已有系统安装的桌面项时不覆盖。
    ''' </summary>
    Public Sub EnsureLinuxDesktopEntry()
        If Not OperatingSystem.IsLinux() Then Return
        Try
            Dim DesktopFile = IO.Path.Combine(DataHome, "applications", "PCL.Avalonia.desktop")
            Dim Existing = FindDesktopEntry("PCL.Avalonia.desktop")
            If Existing IsNot Nothing AndAlso Not IO.File.ReadAllText(Existing).Contains("X-PCL-Generated=true") Then Return
            '清理旧版本生成的 PCL.desktop，避免开始菜单出现两个图标
            Try
                Dim LegacyEntry = FindDesktopEntry("PCL.desktop")
                If LegacyEntry IsNot Nothing AndAlso IO.File.ReadAllText(LegacyEntry).Contains("X-PCL-Generated=true") Then IO.File.Delete(LegacyEntry)
            Catch ex As Exception
                Logger.Warn(ex, "清理旧桌面图标项失败")
            End Try
            'AppImage 运行时 ProcessPath 指向临时挂载目录，桌面项应指向 AppImage 文件本身
            Dim ExePath = Environment.GetEnvironmentVariable("APPIMAGE")
            If String.IsNullOrWhiteSpace(ExePath) Then ExePath = Environment.ProcessPath
            If String.IsNullOrWhiteSpace(ExePath) Then Return
            '图标放入 hicolor 主题
            Dim IconFile = IO.Path.Combine(DataHome, "icons", "hicolor", "256x256", "apps", "PCL.Avalonia.png")
            Using Stream = Platform.AssetLoader.Open(New Uri("avares://PCL.Avalonia/Assets/Images/icon.png"))
                Using Memory As New IO.MemoryStream()
                    Stream.CopyTo(Memory)
                    Dim Bytes = Memory.ToArray()
                    If Not IO.File.Exists(IconFile) OrElse Not IO.File.ReadAllBytes(IconFile).SequenceEqual(Bytes) Then
                        IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(IconFile))
                        IO.File.WriteAllBytes(IconFile, Bytes)
                    End If
                End Using
            End Using
            Dim ExecPath = """" & ExePath & """"
            If IO.Path.GetFileNameWithoutExtension(ExePath).Equals("dotnet", StringComparison.OrdinalIgnoreCase) Then
                Dim EntryAssembly = Reflection.Assembly.GetEntryAssembly()?.Location
                If Not String.IsNullOrWhiteSpace(EntryAssembly) Then ExecPath &= " """ & EntryAssembly & """"
            End If
            '任务管理器会先用 QIcon::hasThemeIcon 查询图标名，而 plasmashell 的图标主题缓存可能早于图标写入；
            '使用绝对路径可让 Plasma 直接加载文件，不受主题缓存影响
            Dim Content = String.Join(vbLf, {
                "[Desktop Entry]",
                "Type=Application",
                "Name=PCL.Avalonia",
                "Comment=Plain Craft Launcher",
                "Exec=" & ExecPath,
                "Icon=" & IconFile,
                "Terminal=false",
                "Categories=Game;",
                "StartupWMClass=PCL.Avalonia",
                "X-PCL-Generated=true",
                ""
            })
            If Not IO.File.Exists(DesktopFile) OrElse IO.File.ReadAllText(DesktopFile) <> Content Then
                IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(DesktopFile))
                IO.File.WriteAllText(DesktopFile, Content)
                Logger.Info($"已写入桌面图标项：{DesktopFile}")
                RefreshKdeServiceCache()
            End If
        Catch ex As Exception
            Logger.Warn(ex, "写入桌面图标项失败")
        End Try
    End Sub

    ''' <summary>
    ''' KDE 的服务数据库不会立即感知新写入的桌面项，后台重建一次缓存，让任务栏马上使用新图标。
    ''' </summary>
    Private Sub RefreshKdeServiceCache()
        If TypeOf Application.Current?.ApplicationLifetime IsNot Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime Then Return
        If Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP")?.Contains("KDE", StringComparison.OrdinalIgnoreCase) <> True Then Return
        Try
            Dim FileName = If(IO.File.Exists("/usr/bin/kbuildsycoca6"), "/usr/bin/kbuildsycoca6",
                If(IO.File.Exists("/usr/bin/kbuildsycoca5"), "/usr/bin/kbuildsycoca5", Nothing))
            If FileName Is Nothing Then Return
            Process.Start(New ProcessStartInfo(FileName) With {
                .Arguments = "--noincremental",
                .UseShellExecute = False,
                .CreateNoWindow = True
            })
        Catch ex As Exception
            Logger.Warn(ex, "刷新 KDE 桌面项缓存失败")
        End Try
    End Sub

    Private Function FindDesktopEntry(FileName As String) As String
        Dim Folders As New List(Of String) From {DataHome}
        Dim DataDirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS")
        If String.IsNullOrWhiteSpace(DataDirs) Then DataDirs = "/usr/local/share:/usr/share"
        Folders.AddRange(DataDirs.Split(":"c, StringSplitOptions.RemoveEmptyEntries))
        For Each Folder In Folders
            Dim Candidate = IO.Path.Combine(Folder, "applications", FileName)
            If IO.File.Exists(Candidate) Then Return Candidate
        Next
        Return Nothing
    End Function

    Private ReadOnly Property DataHome As String
        Get
            Dim Folder = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
            If String.IsNullOrWhiteSpace(Folder) Then Folder = IO.Path.Combine(UserProfile, ".local", "share")
            Return Folder
        End Get
    End Property

#End Region

End Module
