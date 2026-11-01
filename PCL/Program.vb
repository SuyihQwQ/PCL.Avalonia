Imports System.Text
Imports Avalonia
Imports Avalonia.Media

Public Module Program

    <STAThread>
    Public Sub Main(args As String())
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        MeloongCore.Main.Init("PCL")
        Logger.Instance = New FileLogger With {
            .logFolder = PathData,
            .MinLevel = If(ModeDebug, LogLevel.Trace, LogLevel.Info)
        }
        ApplyLinuxCursorTheme()
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)
    End Sub

    Public Function BuildAvaloniaApp() As AppBuilder
        Dim Builder = AppBuilder.Configure(Of App)() _
            .UsePlatformDetect()
        'Skia 在某些 Linux 系统上会把无法渲染的位图字体（如 8514fix）当作默认字体，
        '未显式指定字体的控件一旦提前排版就会抛出 glyphTypeface 异常，因此固定使用 PCL 字体
        Builder = Builder.With(New FontManagerOptions With {
            .DefaultFamilyName = "avares://PCL/Assets/Fonts/PCLEnglish.ttf#PCL English, Noto Sans CJK SC, MiSans TC"
        })
        'Wayland 下禁止协商服务端标题栏，避免启动时先闪出系统标题栏再被移除
        If OperatingSystem.IsLinux() Then
            Builder = Builder.With(New WaylandPlatformOptions With {.ForceDrawnDecorations = True})
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

End Module
