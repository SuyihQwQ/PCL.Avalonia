Imports System.Net
Imports System.Net.Http

''' <summary>
''' 网络代理设置：类型（系统/无/SOCKS5/HTTP）、地址、认证，以及为 Minecraft 注入代理。
''' 与 ModNet 配合使用，将启动器自身请求与游戏进程请求分别接上代理。
''' </summary>
Public Module ModProxy

    Public Enum NetProxyType
        System = 0
        None = 1
        Socks5 = 2
        Http = 3
    End Enum

    ''' <summary>
    ''' 一个已解析的代理端点。
    ''' </summary>
    Public Class NetProxyEndpoint
        Public Type As NetProxyType
        Public Host As String = ""
        Public Port As Integer = 0
        Public Username As String = ""
        Public Password As String = ""
        Public ReadOnly Property IsValid As Boolean
            Get
                Return Host <> "" AndAlso Port > 0 AndAlso Port <= 65535
            End Get
        End Property
        Public ReadOnly Property IsSocks As Boolean
            Get
                Return Type = NetProxyType.Socks5
            End Get
        End Property
        Public Function ToUriString() As String
            Dim Scheme As String = If(IsSocks, "socks5", "http")
            Return $"{Scheme}://{Host}:{Port}"
        End Function
        Public Function ToWebProxy() As WebProxy
            Dim Proxy As New WebProxy(ToUriString())
            If Username <> "" Then Proxy.Credentials = New NetworkCredential(Username, Password)
            Return Proxy
        End Function
        Public Overrides Function ToString() As String
            Return If(IsValid, ToUriString(), "(无效代理)")
        End Function
    End Class

    ' 系统代理解析结果缓存，避免频繁读取桌面配置
    Private _SystemCache As NetProxyEndpoint = Nothing
    Private _SystemCacheTime As Long = 0
    Private ReadOnly SystemCacheLock As New Object()
    Private Const SystemCacheMs As Long = 10000

    ''' <summary>
    ''' 当前设置的代理类型。
    ''' </summary>
    Public Function GetProxyType() As NetProxyType
        Try
            Return CType(Settings.Get(Of Integer)("NetProxyType"), NetProxyType)
        Catch ex As Exception
            Return NetProxyType.System
        End Try
    End Function

    ''' <summary>
    ''' 读取自定义代理设置（不检查类型）。
    ''' </summary>
    Public Function GetCustomEndpoint() As NetProxyEndpoint
        Dim Result As New NetProxyEndpoint With {
            .Type = If(GetProxyType() = NetProxyType.Socks5, NetProxyType.Socks5, NetProxyType.Http),
            .Host = If(Settings.Get(Of String)("NetProxyAddress"), "").Trim(),
            .Port = Settings.Get(Of Integer)("NetProxyPort"),
            .Username = If(Settings.Get(Of String)("NetProxyUsername"), ""),
            .Password = If(Settings.Get(Of String)("NetProxyPassword"), "")
        }
        Return Result
    End Function

    ''' <summary>
    ''' 获取当前生效的代理；返回 Nothing 表示直连。
    ''' </summary>
    Public Function GetActiveEndpoint(Optional ForceRefreshSystem As Boolean = False) As NetProxyEndpoint
        Select Case GetProxyType()
            Case NetProxyType.None
                Return Nothing
            Case NetProxyType.System
                Return ResolveSystemEndpoint(ForceRefreshSystem)
            Case Else
                Dim Endpoint = GetCustomEndpoint()
                Return If(Endpoint.IsValid, Endpoint, Nothing)
        End Select
    End Function

    ''' <summary>
    ''' 解析“使用系统代理”的实际代理：环境变量、KDE、GNOME、macOS、Windows 等。
    ''' </summary>
    Public Function ResolveSystemEndpoint(Optional ForceRefresh As Boolean = False) As NetProxyEndpoint
        SyncLock SystemCacheLock
            If Not ForceRefresh AndAlso _SystemCache IsNot Nothing AndAlso GetTimeMs() - _SystemCacheTime < SystemCacheMs Then
                Return _SystemCache
            End If
            Dim Result As NetProxyEndpoint = Nothing
            Try
                Select Case True
                    Case OperatingSystem.IsWindows()
                        Result = ResolveWindowsProxy()
                    Case OperatingSystem.IsMacOS()
                        Result = ResolveMacProxy()
                    Case Else
                        Result = ResolveLinuxProxy()
                End Select
            Catch ex As Exception
                Logger.Warn(ex, "读取系统代理失败")
            End Try
            _SystemCache = Result
            _SystemCacheTime = GetTimeMs()
            If Result IsNot Nothing Then Logger.Info($"已读取系统代理：{Result}")
            Return Result
        End SyncLock
    End Function

    ''' <summary>
    ''' 迁移旧版代理设置（NetProxyMode 0/1/2 + 完整 NetProxyAddress）到新设置。
    ''' </summary>
    Public Sub MigrateLegacyProxySettings()
        Try
            If Settings.Get(Of Boolean)("NetProxyMigrated") Then Return
            Dim LegacyMode As Integer = Settings.Get(Of Integer)("NetProxyMode")
            Select Case LegacyMode
                Case 0, 1
                    '默认不使用代理（旧版默认值 0 表示系统代理，这里统一迁移为不使用）
                    Settings.Set("NetProxyType", CInt(NetProxyType.None))
                Case Else
                    Dim Address As String = If(Settings.Get(Of String)("NetProxyAddress"), "").Trim()
                    Dim Endpoint = ParseProxyUri(Address)
                    If Endpoint IsNot Nothing Then
                        Settings.Set("NetProxyType", CInt(If(Endpoint.IsSocks, NetProxyType.Socks5, NetProxyType.Http)))
                        Settings.Set("NetProxyAddress", Endpoint.Host)
                        Settings.Set("NetProxyPort", Endpoint.Port)
                        Settings.Set("NetProxyUsername", Endpoint.Username)
                        Settings.Set("NetProxyPassword", Endpoint.Password)
                    Else
                        Settings.Set("NetProxyType", CInt(NetProxyType.System))
                    End If
            End Select
            Settings.Set("NetProxyMigrated", True)
            Logger.Info($"代理设置已迁移：类型 {Settings.Get(Of Integer)("NetProxyType")}，地址 {Settings.Get(Of String)("NetProxyAddress")}:{Settings.Get(Of Integer)("NetProxyPort")}")
        Catch ex As Exception
            Logger.Warn(ex, "迁移代理设置失败")
        End Try
    End Sub

#Region "各平台系统代理读取"

    Private Function ResolveLinuxProxy() As NetProxyEndpoint
        '1. 环境变量（KDE/GNOME 的“使用环境变量”模式、终端启动时最常见）
        Dim FromEnv = ResolveProxyEnvironment()
        If FromEnv IsNot Nothing Then Return FromEnv
        '2. KDE KIO 设置（kioslaverc）
        Dim FromKde = ResolveKdeProxy()
        If FromKde IsNot Nothing Then Return FromKde
        '3. GNOME/GTK（gsettings / dconf）
        Dim FromGnome = ResolveGnomeProxy()
        If FromGnome IsNot Nothing Then Return FromGnome
        Return Nothing
    End Function

    Private Function ResolveProxyEnvironment() As NetProxyEndpoint
        For Each Key In {"https_proxy", "HTTPS_PROXY", "http_proxy", "HTTP_PROXY", "all_proxy", "ALL_PROXY"}
            Dim Value As String = Environment.GetEnvironmentVariable(Key)
            If String.IsNullOrWhiteSpace(Value) Then Continue For
            Dim Endpoint = ParseProxyUri(Value)
            If Endpoint IsNot Nothing Then Return Endpoint
        Next
        Return Nothing
    End Function

    ''' <summary>
    ''' 解析 KDE 的 kioslaverc（KIO 代理设置）。ProxyType：0 无代理，1 手动，4 使用环境变量。
    ''' </summary>
    Private Function ResolveKdeProxy() As NetProxyEndpoint
        Try
            Dim ConfigPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "kioslaverc")
            If Not FileUtils.Exists(ConfigPath) Then Return Nothing
            Dim Values = ReadIniSection(ConfigPath, "Proxy Settings")
            If Values Is Nothing Then Return Nothing
            Dim ProxyType As String = Nothing
            If Not Values.TryGetValue("ProxyType", ProxyType) Then Return Nothing
            Select Case ProxyType.Trim()
                Case "0"
                    Return Nothing
                Case "4"
                    Return ResolveProxyEnvironment()
            End Select
            '手动模式：优先 httpsProxy，其次 httpProxy、socksProxy
            For Each Key In {"httpsProxy", "httpProxy", "socksProxy"}
                Dim Raw As String = Nothing
                If Not Values.TryGetValue(Key, Raw) Then Continue For
                Dim Endpoint = ParseKdeProxyValue(Raw, Key.StartsWith("socks", StringComparison.OrdinalIgnoreCase))
                If Endpoint IsNot Nothing Then Return Endpoint
            Next
            Return Nothing
        Catch ex As Exception
            Logger.Warn(ex, "读取 KDE 代理设置失败")
            Return Nothing
        End Try
    End Function

    ' KDE 的值格式通常为 "http://127.0.0.1 8080" 或 "socks://127.0.0.1 1080"，也可能带用户名密码
    Private Function ParseKdeProxyValue(Raw As String, IsSocks As Boolean) As NetProxyEndpoint
        If String.IsNullOrWhiteSpace(Raw) Then Return Nothing
        Dim Text = Raw.Trim()
        Dim Scheme As String = If(IsSocks, "socks5", "http")
        Text = Text.Replace(" ", ":") ' "http://host 8080" → "http://host:8080"
        If Not Text.Contains("://") Then Text = Scheme & "://" & Text
        If Text.StartsWith("socks://", StringComparison.OrdinalIgnoreCase) Then Text = "socks5://" & Text.Substring(8)
        Return ParseProxyUri(Text)
    End Function

    ''' <summary>
    ''' 读取 GNOME/GTK 的 gsettings 代理设置（需要系统存在 gsettings 命令）。
    ''' </summary>
    Private Function ResolveGnomeProxy() As NetProxyEndpoint
        Try
            Dim Mode = RunCommand("gsettings", "get org.gnome.system.proxy mode", 2000)
            If Mode Is Nothing Then Return Nothing
            Mode = Mode.Trim().Trim("'"c)
            If Mode <> "manual" Then Return Nothing
            '优先 https，其次 http，再 socks
            For Each Entry In {("https", "http"), ("http", "http"), ("socks", "socks")}
                Dim Prefix As String = "org.gnome.system.proxy." & Entry.Item1
                Dim Host = RunCommand("gsettings", $"get {Prefix} host", 2000)
                Dim Port = RunCommand("gsettings", $"get {Prefix} port", 2000)
                If String.IsNullOrWhiteSpace(Host) Then Continue For
                Host = Host.Trim().Trim("'"c)
                Dim PortValue As Integer = 0
                Integer.TryParse(If(Port, "").Trim(), PortValue)
                If PortValue <= 0 Then Continue For
                Return New NetProxyEndpoint With {.Type = If(Entry.Item2 = "socks", NetProxyType.Socks5, NetProxyType.Http),
                    .Host = Host, .Port = PortValue}
            Next
            Return Nothing
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Function ResolveWindowsProxy() As NetProxyEndpoint
        Try
            Dim Proxy As IWebProxy = WebRequest.GetSystemWebProxy()
            If Proxy IsNot Nothing Then
                Dim Uri = Proxy.GetProxy(New Uri("https://example.com/"))
                If Uri IsNot Nothing AndAlso Uri.Host <> "example.com" AndAlso Uri.Port > 0 Then
                    Dim IsSocks = Uri.Scheme.StartsWith("socks", StringComparison.OrdinalIgnoreCase)
                    Return New NetProxyEndpoint With {.Type = If(IsSocks, NetProxyType.Socks5, NetProxyType.Http),
                        .Host = Uri.Host, .Port = Uri.Port}
                End If
            End If
        Catch ex As Exception
            Logger.Warn(ex, "读取 Windows 系统代理失败")
        End Try
        Return Nothing
    End Function

    Private Function ResolveMacProxy() As NetProxyEndpoint
        Try
            Dim Output = RunCommand("/usr/sbin/scutil", "--proxy", 3000)
            If String.IsNullOrWhiteSpace(Output) Then Return Nothing
            Dim Map As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each Line In Output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                Dim Text = Line.Trim()
                Dim Parts = Text.Split({":"c}, 2)
                If Parts.Length = 2 Then Map(Parts(0).Trim()) = Parts(1).Trim()
            Next
            For Each Entry In {("HTTPS", "HTTP"), ("HTTP", "HTTP"), ("SOCKS", "SOCKS")}
                Dim Enable As String = Nothing
                If Not Map.TryGetValue($"{(Entry.Item1)}Enable", Enable) OrElse Enable <> "1" Then Continue For
                Dim Host As String = Nothing, Port As String = Nothing
                If Not Map.TryGetValue($"{Entry.Item1}Proxy", Host) Then Continue For
                If Not Map.TryGetValue($"{Entry.Item1}Port", Port) Then Continue For
                Dim PortValue As Integer = 0
                Integer.TryParse(Port, PortValue)
                If Host <> "" AndAlso PortValue > 0 Then
                    Return New NetProxyEndpoint With {.Type = If(Entry.Item2 = "SOCKS", NetProxyType.Socks5, NetProxyType.Http),
                        .Host = Host, .Port = PortValue}
                End If
            Next
            Return Nothing
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

#End Region

#Region "工具函数"

    ''' <summary>
    ''' 解析代理 URI，支持 http://host:port、socks5://user:pass@host:port 以及 host:port。
    ''' </summary>
    Public Function ParseProxyUri(Raw As String) As NetProxyEndpoint
        Try
            If String.IsNullOrWhiteSpace(Raw) Then Return Nothing
            Dim Text = Raw.Trim()
            If Not Text.Contains("://") Then Text = "http://" & Text
            Dim Uri As New Uri(Text)
            If String.IsNullOrEmpty(Uri.Host) OrElse Uri.Port <= 0 Then Return Nothing
            Dim IsSocks = Uri.Scheme.StartsWith("socks", StringComparison.OrdinalIgnoreCase)
            Dim User = "", Pass = ""
            If Not String.IsNullOrEmpty(Uri.UserInfo) Then
                Dim Parts = Uri.UserInfo.Split(":"c, 2)
                User = Uri.UnescapeDataString(Parts(0))
                Pass = If(Parts.Length > 1, Uri.UnescapeDataString(Parts(1)), "")
            End If
            Return New NetProxyEndpoint With {
                .Type = If(IsSocks, NetProxyType.Socks5, NetProxyType.Http),
                .Host = Uri.Host, .Port = Uri.Port, .Username = User, .Password = Pass}
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' 读取 INI 文件某个节的所有键值（用于 kioslaverc）。
    ''' </summary>
    Private Function ReadIniSection(FilePath As String, Section As String) As Dictionary(Of String, String)
        Try
            If Not FileUtils.Exists(FilePath) Then Return Nothing
            Dim Lines = FileUtils.ReadAsString(FilePath).Split({vbCrLf, vbLf}, StringSplitOptions.None)
            Dim Result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim InSection As Boolean = False, Found As Boolean = False
            For Each Line In Lines
                Dim Text = Line.Trim()
                If Text = "" OrElse Text.StartsWith("#", StringComparison.Ordinal) OrElse Text.StartsWith(";", StringComparison.Ordinal) Then Continue For
                If Text.StartsWith("[", StringComparison.Ordinal) Then
                    InSection = String.Equals(Text.Trim("[", "]"), Section, StringComparison.OrdinalIgnoreCase)
                    If InSection Then Found = True
                    Continue For
                End If
                If Not InSection Then Continue For
                Dim Split = Text.Split("="c, 2)
                If Split.Length = 2 Then Result(Split(0).Trim()) = Split(1).Trim()
            Next
            Return If(Found, Result, Nothing)
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' 运行一个带超时的命令并返回标准输出；失败返回 Nothing。
    ''' </summary>
    Private Function RunCommand(Command As String, Arguments As String, TimeoutMs As Integer) As String
        Try
            Dim Info As New ProcessStartInfo(Command) With {
                .Arguments = Arguments, .RedirectStandardOutput = True, .RedirectStandardError = True,
                .UseShellExecute = False, .CreateNoWindow = True}
            Using Proc = Process.Start(Info)
                If Proc Is Nothing Then Return Nothing
                Dim Task = Proc.StandardOutput.ReadToEndAsync()
                If Not Proc.WaitForExit(TimeoutMs) Then
                    Try : Proc.Kill() : Catch : End Try
                    Return Nothing
                End If
                If Task.Wait(500) Then Return Task.Result
                Return Nothing
            End Using
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

#End Region

End Module
