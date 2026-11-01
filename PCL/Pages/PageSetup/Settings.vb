Imports System.Security.Cryptography

Public Class Settings

    ''' <summary>
    ''' 设置项的储存位置。
    ''' </summary>
    Public Enum Sources
        Normal
        Registry
        Instance
    End Enum

    Public Shared ReadOnly Entries As Dictionary(Of String, Setting) = (New List(Of Setting) From {
        New Setting("Identify", "", Source:=Sources.Registry),
        New Setting("WindowHeight", 550),
        New Setting("WindowWidth", 900),
        New Setting("AprilYear", 0, Source:=Sources.Registry, Encrypted:=True),
        New Setting("Potatoes", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("HintDownloadThread", False, Source:=Sources.Registry),
        New Setting("HintNotice", 0, Source:=Sources.Registry),
        New Setting("HintDownload", 0, Source:=Sources.Registry),
        New Setting("HintHide", False, Source:=Sources.Registry),
        New Setting("HintHandInstall", False, Source:=Sources.Registry),
        New Setting("HintBuy", False, Source:=Sources.Registry),
        New Setting("HintClearRubbish", 0, Source:=Sources.Registry),
        New Setting("HintUpdateMod", False, Source:=Sources.Registry),
        New Setting("HintCustomCommand", False, Source:=Sources.Registry),
        New Setting("HintCustomWarn", False, Source:=Sources.Registry),
        New Setting("HintMoreAdvancedSetup", False, Source:=Sources.Registry),
        New Setting("HintIndieSetup", False, Source:=Sources.Registry),
        New Setting("HintExportConfig", False, Source:=Sources.Registry),
        New Setting("HintSnapshot", False, Source:=Sources.Registry),
        New Setting("SystemEulaVersion", 0, Source:=Sources.Registry),
        New Setting("SystemCount", 0, Source:=Sources.Registry, Encrypted:=True),
        New Setting("SystemLaunchCount", 0, Source:=Sources.Registry, Encrypted:=True),
        New Setting("SystemLastVersionReg", 0, Source:=Sources.Registry, Encrypted:=True),
        New Setting("SystemHighestSavedBetaVersionReg", 0, Source:=Sources.Registry, Encrypted:=True),
        New Setting("SystemHighestBetaVersionReg", 0, Source:=Sources.Registry, Encrypted:=True),
        New Setting("SystemHighestAlphaVersionReg", 0, Source:=Sources.Registry, Encrypted:=True),
        New Setting("SystemHelpVersion", 0, Source:=Sources.Registry),
        New Setting("SystemDebugMode", False, Source:=Sources.Registry, OnChanged:=Sub() Logger.Instance.MinLevel = If(ModeDebug, LogLevel.Trace, LogLevel.Info)),
        New Setting("SystemDebugAnim", 9, Source:=Sources.Registry),
        New Setting("SystemDebugDelay", False, Source:=Sources.Registry),
        New Setting("SystemDebugSkipCopy", False, Source:=Sources.Registry),
        New Setting("SystemSystemCache", "", Source:=Sources.Registry),
        New Setting("SystemSystemUpdate", 0),
        New Setting("SystemSystemActivity", 0),
        New Setting("SystemSystemTelemetry", True, Source:=Sources.Registry),
        New Setting("CacheDrops", "", Source:=Sources.Registry),
        New Setting("CacheConfig", 0, Source:=Sources.Registry),
        New Setting("CacheExportConfig", "", Source:=Sources.Registry),
        New Setting("CacheSavedPageUrl", "", Source:=Sources.Registry),
        New Setting("CacheSavedPageVersion", "", Source:=Sources.Registry),
        New Setting("CacheMsOAuthRefresh", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsAccess", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsProfileJson", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsUuid", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsName", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsV2Migrated", False, Source:=Sources.Registry),
        New Setting("CacheMsV2OAuthRefresh", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsV2Access", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsV2ProfileJson", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsV2Uuid", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsV2Name", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheMsV2Expires", 0L, Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheNideAccess", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheNideClient", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheNideUuid", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheNideName", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheNideUsername", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheNidePass", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheNideServer", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthAccess", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthClient", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthUuid", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthName", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthUsername", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthPass", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthServerServer", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthServerName", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheAuthServerRegister", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("CacheDownloadFolder", "", Source:=Sources.Registry),
        New Setting("CacheJavaListVersion", 0, Source:=Sources.Registry),
        New Setting("LoginRemember", True, Source:=Sources.Registry, Encrypted:=True),
        New Setting("LoginLegacyName", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("LoginMsJson", "{}", Source:=Sources.Registry, Encrypted:=True),
        New Setting("LoginNideEmail", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("LoginNidePass", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("LoginAuthEmail", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("LoginAuthPass", "", Source:=Sources.Registry, Encrypted:=True),
        New Setting("LoginType", McLoginType.Legacy, Source:=Sources.Registry),
        New Setting("LoginPageType", 0),
        New Setting("LaunchSkinID", "", Source:=Sources.Registry),
        New Setting("LaunchSkinType", 0, Source:=Sources.Registry, OnChanged:=Sub() PageSetupLaunch.UpdateSkinType()),
        New Setting("LaunchSkinSlim", False, Source:=Sources.Registry),
        New Setting("LaunchFolderSelect", ""),
        New Setting("LaunchFolders", "", Source:=Sources.Registry),
        New Setting("LaunchArgumentTitle", ""),
        New Setting("LaunchArgumentInfo", "PCL"),
        New Setting("LaunchArgumentJavaSelect", "", Source:=Sources.Registry),
        New Setting("LaunchArgumentJavaAll", "[]", Source:=Sources.Registry),
        New Setting("LaunchArgumentIndie", 0),
        New Setting("LaunchArgumentIndieV2", 4),
        New Setting("LaunchArgumentVisible", 5, Source:=Sources.Registry),
        New Setting("LaunchArgumentPriority", 1, Source:=Sources.Registry),
        New Setting("LaunchArgumentWindowWidth", 854),
        New Setting("LaunchArgumentWindowHeight", 480),
        New Setting("LaunchArgumentWindowType", 1),
        New Setting("LaunchArgumentRam", False, Source:=Sources.Registry),
        New Setting("LaunchAdvanceJvm", "-XX:-OmitStackTraceInFastThrow -Djdk.lang.Process.allowAmbiguousCommands=True -Dfml.ignoreInvalidMinecraftCertificates=True -Dfml.ignorePatchDiscrepancies=True"),
        New Setting("LaunchAdvanceGame", ""),
        New Setting("LaunchAdvanceRun", ""),
        New Setting("LaunchAdvanceRunWait", True),
        New Setting("LaunchAdvanceDisableJLW", False),
        New Setting("LaunchAdvanceDisableLUA", False),
        New Setting("LaunchAdvanceGraphicCard", True, Source:=Sources.Registry),
        New Setting("LaunchAdvanceGC", 4),
        New Setting("LaunchRamType", 0),
        New Setting("LaunchRamCustom", 15),
        New Setting("LinkLastAutoJoinInviteCode", "", Source:=Sources.Registry),
        New Setting("LinkLatencyMode", 0, Source:=Sources.Registry),
        New Setting("LinkCustomPeer", ""),
        New Setting("LinkEasyTierVersion", -1, Source:=Sources.Registry),
        New Setting("ToolHelpChinese", True, Source:=Sources.Registry),
        New Setting("ToolDownloadThread", 63, Source:=Sources.Registry),
        New Setting("ToolDownloadSpeed", 42, Source:=Sources.Registry),
        New Setting("ToolDownloadSource", 1, Source:=Sources.Registry),
        New Setting("ToolDownloadVersion", 1, Source:=Sources.Registry),
        New Setting("ToolDownloadTranslate", 0, Source:=Sources.Registry),
        New Setting("ToolDownloadTranslateV2", 1, Source:=Sources.Registry),
        New Setting("ToolDownloadCert", False, Source:=Sources.Registry),
        New Setting("ToolDownloadMod", 2, Source:=Sources.Registry),
        New Setting("ToolModLocalNameStyle", 0, Source:=Sources.Registry),
        New Setting("ToolUpdateRelease", False, Source:=Sources.Registry),
        New Setting("ToolUpdateSnapshot", False, Source:=Sources.Registry),
        New Setting("ToolUpdateReleaseLast", "", Source:=Sources.Registry),
        New Setting("ToolUpdateSnapshotLast", "", Source:=Sources.Registry),
        New Setting("UiLauncherTransparent", 600, OnChanged:=Sub(Value) If FrmMain IsNot Nothing Then FrmMain.Opacity = Val(Value) / 1000 + 0.4),
        New Setting("UiLauncherHue", 180),
        New Setting("UiLauncherSat", 80),
        New Setting("UiLauncherDelta", 90),
        New Setting("UiLauncherLight", 20),
        New Setting("UiLauncherTheme", 0, OnChanged:=Sub() ThemeRefresh()),
        New Setting("UiLauncherThemeMode", 0, OnChanged:=Sub() ThemeRefresh()), '0 跟随系统，1 浅色，2 深色
        New Setting("UiLauncherThemeHide", "0|1|2|3|4", Source:=Sources.Registry, Encrypted:=True),
        New Setting("UiLauncherThemeHide2", "0|1|2|3|4", Source:=Sources.Registry, Encrypted:=True),
        New Setting("UiLauncherLogo", True),
        New Setting("UiLauncherEmail", False),
        New Setting("UiBackgroundColorful", True, OnChanged:=Sub() ThemeRefresh()),
        New Setting("UiBackgroundOpacity", 1000, OnChanged:=Sub(Value) FormMain.UpdateBackgroundAndTitleBar(Value)),
        New Setting("UiBackgroundBlur", 0, OnChanged:=Sub(Value) FormMain.UpdateBackgroundAndTitleBar(Value)),
        New Setting("UiBackgroundSuit", 0, OnChanged:=Sub(Value) FormMain.UpdateBackgroundAndTitleBar(Value)),
        New Setting("UiCustomType", 0, OnChanged:=Sub(Value) PageSetupUI.OnMainPageTypeChanged()),
        New Setting("UiCustomPreset", 0, OnChanged:=Sub(Value) PageLaunchRight.OnHomepagePresetChanged()),
        New Setting("UiCustomNet", ""),
        New Setting("UiLogoType", 1, OnChanged:=Sub(Value) FormMain.UpdateBackgroundAndTitleBar(Value)),
        New Setting("UiLogoText", "", OnChanged:=Sub(Value) FormMain.UpdateBackgroundAndTitleBar(Value)),
        New Setting("UiLogoLeft", False, OnChanged:=Sub(Value) FormMain.UpdateBackgroundAndTitleBar(Value)),
        New Setting("UiMusicVolume", 500),
        New Setting("UiMusicStop", False),
        New Setting("UiMusicStart", False),
        New Setting("UiMusicRandom", True),
        New Setting("UiMusicAuto", True),
        New Setting("UiHiddenPageDownload", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenPageLink", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenPageSetup", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenPageOther", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenFunctionSelect", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenFunctionModUpdate", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenFunctionInstanceModify", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenFunctionHidden", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenSetupLaunch", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenSetupUi", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenSetupLink", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenSetupSystem", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenOtherHelp", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenOtherFeedback", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenOtherVote", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenOtherAbout", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("UiHiddenOtherTest", False, OnChanged:=Sub(Value) PageSetupUI.HiddenRefresh()),
        New Setting("VersionAdvanceJvm", "", Source:=Sources.Instance),
        New Setting("VersionAdvanceGame", "", Source:=Sources.Instance),
        New Setting("VersionAdvanceAssets", 0, Source:=Sources.Instance),
        New Setting("VersionAdvanceAssetsV2", False, Source:=Sources.Instance),
        New Setting("VersionAdvanceRun", "", Source:=Sources.Instance),
        New Setting("VersionAdvanceRunWait", True, Source:=Sources.Instance),
        New Setting("VersionAdvanceDisableJLW", False, Source:=Sources.Instance),
        New Setting("VersionAdvanceDisableLUA", False, Source:=Sources.Instance),
        New Setting("VersionAdvanceDisableModUpdate", False, Source:=Sources.Instance),
        New Setting("VersionAdvanceGC", 0, Source:=Sources.Instance),
        New Setting("VersionRamType", 2, Source:=Sources.Instance),
        New Setting("VersionRamCustom", 15, Source:=Sources.Instance),
        New Setting("VersionRamOptimize", 0, Source:=Sources.Instance),
        New Setting("VersionArgumentTitle", "", Source:=Sources.Instance),
        New Setting("VersionArgumentInfo", "", Source:=Sources.Instance),
        New Setting("VersionArgumentIndie", -1, Source:=Sources.Instance),
        New Setting("VersionArgumentIndieV2", False, Source:=Sources.Instance),
        New Setting("VersionArgumentJavaSelect", "使用全局设置", Source:=Sources.Instance),
        New Setting("VersionArgumentJavaV2", 0, Source:=Sources.Instance),
        New Setting("VersionArgumentJavaRange", "", Source:=Sources.Instance),
        New Setting("VersionServerEnter", "", Source:=Sources.Instance),
        New Setting("VersionServerLogin", 0, Source:=Sources.Instance),
        New Setting("VersionServerNide", "", Source:=Sources.Instance),
        New Setting("VersionServerAuthRegister", "", Source:=Sources.Instance),
        New Setting("VersionServerAuthName", "", Source:=Sources.Instance),
        New Setting("VersionServerAuthServer", "", Source:=Sources.Instance)
    }).ToDictionary(Function(e) e.Key)

    Public Class Setting
        Public Key As String
        Public Encrypted As Boolean
        Public DefaultValue As Object
        Public Source As Sources
        ''' <summary>
        ''' 当设置项的值实际被改变时触发，参数为新值。
        ''' </summary>
        Public OnChanged As Action(Of Object)
        ''' <summary>
        ''' 该设置的值的实际类型。
        ''' </summary>
        Public Type As Type

        Public Sub New(Key As String, Value As Object, Optional Source As Sources = Sources.Normal, Optional Encrypted As Boolean = False, Optional OnChanged As Action(Of Object) = Nothing)
            Try
                Me.Key = Key
                Me.DefaultValue = Value
                Me.Encrypted = Encrypted
                Me.Source = Source
                Me.Type = If(Value, "").GetType
                Me.OnChanged = OnChanged
            Catch ex As Exception
                Logger.Error(ex, "初始化设置项失败")
            End Try
        End Sub

        ''' <summary>
        ''' 读取过的设置的缓存，若从未读取过则为 Nothing。
        ''' 若为版本独立设置，键为版本路径；否则，键为空字符串。
        ''' </summary>
        Public ValueCache As New ConcurrentDictionary(Of String, Object)

        Public Function GetCache(InstancePath As String) As Object
            Dim CacheKey As String = If(Source = Sources.Instance, InstancePath, "")
            Dim Result = Nothing
            Return If(ValueCache.TryGetValue(CacheKey, Result), Result, Nothing)
        End Function

        Public Sub [Set](Value As Object, InstancePath As String)
            Dim CacheKey As String = If(Source = Sources.Instance, InstancePath, "")
            ValueCache(CacheKey) = Value
        End Sub

        ''' <summary>
        ''' 立即将当前的 Value 写入对应的注册表或文件。
        ''' </summary>
        Public Sub Save(Optional InstancePath As String = Nothing)
            Dim Value As String = GetCache(InstancePath)
            Logger.Trace($"保存设置：{Key} {If(Encrypted, "", "= " & Value)}{If(InstancePath Is Nothing, "", $"（实例：{InstancePath}）")}")
            If Encrypted Then
                Try
                    If Value Is Nothing Then Value = ""
                    Value = DesEncrypt(Value, "PCL" & Identify)
                Catch ex As Exception
                    Logger.Warn(ex, $"加密设置失败：{Key}")
                End Try
            End If
            Select Case Source
                Case Sources.Normal
                    WriteIni("Setup", Key, Value)
                Case Sources.Registry
                    Try
                        RegistryWrite(Key, Value)
                    Catch ex As Exception
                        Logger.Warn(ex, $"写入注册表失败（{Key} → {Value}）", LogBehavior.Toast)
                    End Try
                Case Sources.Instance
                    If InstancePath Is Nothing Then Throw New Exception($"保存版本独立设置 {Key} 时未提供目标版本")
                    WriteIni(Path.Combine(InstancePath, "PCL", "Setup.ini"), Key, Value)
            End Select
        End Sub

    End Class

    ''' <summary>
    ''' 改变某个设置项的值。
    ''' </summary>
    Public Shared Sub [Set](Key As String, Value As Object, Optional InstancePath As String = Nothing)
        Dim Entry As Setting = Nothing
        If Not Entries.TryGetValue(Key, Entry) Then Throw New KeyNotFoundException("未找到设置项：" & Key)
        Try
            Value = CTypeDynamic(Value, Entry.Type)
            If Entry.GetCache(InstancePath) Is Nothing Then [Get](Key, InstancePath)
            If Entry.GetCache(InstancePath) = Value Then Return
            Entry.Set(Value, InstancePath)
            Entry.Save(InstancePath)
            If Entry.OnChanged IsNot Nothing Then Entry.OnChanged.Invoke(Value)
        Catch ex As Exception
            Logger.Error(ex, $"设置设置项时出错（{Key}, {Value}）")
        End Try
    End Sub

    Public Shared Sub [Set](Key As String, Value As Object, Instance As McInstance)
        [Set](Key, Value, Instance?.PathVersion)
    End Sub

    ''' <summary>
    ''' 写入某个未经加密的设置项。
    ''' 若该设置项经过了加密，则会抛出异常。
    ''' </summary>
    Public Shared Sub SetSafe(Key As String, Value As Object, Optional InstancePath As String = Nothing)
        Dim Entry As Setting = Nothing
        If Not Entries.TryGetValue(Key, Entry) Then Throw New KeyNotFoundException("未找到设置项：" & Key)
        If Entry.Encrypted Then Throw New InvalidOperationException("禁止写入加密设置项：" & Key)
        [Set](Key, Value, InstancePath)
    End Sub

    Public Shared Sub SetSafe(Key As String, Value As Object, Instance As McInstance)
        SetSafe(Key, Value, Instance?.PathVersion)
    End Sub

    ''' <summary>
    ''' 获取某个设置项的值。
    ''' </summary>
    Public Shared Function [Get](Key As String, Optional InstancePath As String = Nothing)
        Dim Entry As Setting = Nothing
        If Not Entries.TryGetValue(Key, Entry) Then Throw New KeyNotFoundException("未找到设置项：" & Key)
        '对部分设置强制赋值
        Dim ForcedSettings As New Dictionary(Of String, Object) From {
            {"UiHiddenPageLink", True},
            {"UiHiddenSetupLink", True}
        }
        Dim ForcedSetting As Object = Nothing
        If ForcedSettings.TryGetValue(Key, ForcedSetting) Then
            Entry.Set(ForcedSetting, InstancePath)
            Return ForcedSetting
        End If
        Dim Value = Entry.GetCache(InstancePath)
        If Value IsNot Nothing Then Return Value
        '正常读取
        Try
            Dim GotValue As String = Nothing
            Dim DefaultValue As String = If(Entry.Encrypted, DesEncrypt(Entry.DefaultValue, "PCL" & Identify), Entry.DefaultValue)
            Select Case Entry.Source
                Case Sources.Normal
                    GotValue = ReadIni("Setup", Key, DefaultValue)
                Case Sources.Registry
                    GotValue = RegistryRead(Key, DefaultValue)
                Case Sources.Instance
                    If InstancePath Is Nothing Then
                        Throw New Exception($"读取版本设置 {Key} 时未提供目标版本")
                    Else
                        GotValue = ReadIni(Path.Combine(InstancePath, "PCL", "Setup.ini"), Key, DefaultValue)
                    End If
            End Select
            If Entry.Encrypted Then
                If GotValue = DefaultValue Then
                    GotValue = Entry.DefaultValue
                Else
                    Try
                        GotValue = DesDecrypt(GotValue, "PCL" & Identify)
                    Catch ex As Exception
                        Logger.Warn(ex, $"解密设置失败：{Key}")
                        GotValue = Entry.DefaultValue
                        Entry.Set(Entry.DefaultValue, InstancePath)
                        Entry.Save(InstancePath)
                    End Try
                End If
            End If
            Entry.Set(CTypeDynamic(GotValue, Entry.Type), InstancePath)
        Catch ex As Exception
            Logger.Error(ex, $"读取设置失败：{Key}", LogBehavior.Toast)
            Entry.Set(CTypeDynamic(Entry.DefaultValue, Entry.Type), InstancePath)
        End Try
        Return Entry.GetCache(InstancePath)
    End Function

    Public Shared Function [Get](Key As String, Instance As McInstance)
        Return [Get](Key, Instance?.PathVersion)
    End Function

    Public Shared Function [Get](Of T)(Key As String, Optional InstancePath As String = Nothing) As T
        Return [Get](Key, InstancePath)
    End Function

    Public Shared Function [Get](Of T)(Key As String, Instance As McInstance) As T
        Return [Get](Of T)(Key, Instance?.PathVersion)
    End Function

    ''' <summary>
    ''' 获取某个未经加密的设置项的值。
    ''' 若该设置项经过了加密，则会抛出异常。
    ''' </summary>
    Public Shared Function GetSafe(Key As String, Optional InstancePath As String = Nothing)
        Dim Entry As Setting = Nothing
        If Not Entries.TryGetValue(Key, Entry) Then Throw New KeyNotFoundException("未找到设置项：" & Key)
        If Entry.Encrypted Then Throw New InvalidOperationException("禁止读取加密设置项：" & Key)
        Return [Get](Key, InstancePath)
    End Function

    Public Shared Function GetSafe(Key As String, Instance As McInstance)
        Return GetSafe(Key, Instance?.PathVersion)
    End Function

    ''' <summary>
    ''' 初始化某个设置项的值。
    ''' </summary>
    Public Shared Sub Reset(Key As String, Optional InstancePath As String = Nothing)
        Dim Entry As Setting = Nothing
        If Not Entries.TryGetValue(Key, Entry) Then Throw New KeyNotFoundException("未找到设置项：" & Key)
        Try
            If Entry.GetCache(InstancePath) Is Nothing Then [Get](Key, InstancePath)
            If Entry.GetCache(InstancePath) = Entry.DefaultValue Then Return
            Entry.Set(Entry.DefaultValue, InstancePath)
            Select Case Entry.Source
                Case Sources.Normal
                    DeleteIniKey("Setup", Key)
                Case Sources.Registry
                    Try
                        RegistryDelete(Key)
                    Catch ex As Exception
                        Logger.Warn(ex, $"删除注册表失败（{Key}）", LogBehavior.Toast)
                    End Try
                Case Sources.Instance
                    If InstancePath Is Nothing Then Throw New Exception($"重置版本设置 {Key} 时未提供目标版本")
                    DeleteIniKey(Path.Combine(InstancePath, "PCL", "Setup.ini"), Key)
            End Select
            If Entry.OnChanged IsNot Nothing Then Entry.OnChanged.Invoke(Entry.DefaultValue)
        Catch ex As Exception
            Logger.Error(ex, $"重置设置项时出错（{Key}）")
        End Try
    End Sub

    Public Shared Sub Reset(Key As String, Instance As McInstance)
        Reset(Key, Instance?.PathVersion)
    End Sub

    ''' <summary>
    ''' 获取某个设置项的默认值。
    ''' </summary>
    Public Shared Function GetDefault(Key As String) As String
        Return Entries(Key).DefaultValue
    End Function

    ''' <summary>
    ''' 对应的注册表或文件是否已经储存了某个设置项。
    ''' </summary>
    Public Shared Function HasSaved(Key As String, Optional InstancePath As String = Nothing) As Boolean
        Select Case Entries(Key).Source
            Case Sources.Normal
                Return HasIniKey("Setup", Key)
            Case Sources.Registry
                Try
                    Return RegistryHas(Key)
                Catch ex As Exception
                    Logger.Warn(ex, $"判断注册表 {Key} 是否存在时出错")
                    Return False
                End Try
            Case Else
                If InstancePath Is Nothing Then Throw New Exception($"判断版本设置 {Key} 是否存在时未提供目标版本")
                Return HasIniKey(Path.Combine(InstancePath, "PCL", "Setup.ini"), Key)
        End Select
    End Function

    Public Shared Function HasSaved(Key As String, Instance As McInstance) As Boolean
        Return HasSaved(Key, Instance?.PathVersion)
    End Function

#Region "注册表兼容"

    Private Shared ReadOnly RegPath As String = $"HKEY_CURRENT_USER\Software\{RegFolder}"

    '非 Windows 平台没有注册表，使用数据文件夹下的 Registry.ini 代替
    Private Shared Function RegistryRead(Key As String, DefaultValue As String) As String
        If OperatingSystem.IsWindows() Then Return RegistryUtils.TryRead(RegPath, Key, DefaultValue)
        Return ReadIni("Registry", Key, DefaultValue)
    End Function
    Private Shared Sub RegistryWrite(Key As String, Value As String)
        If OperatingSystem.IsWindows() Then
            RegistryUtils.Write(RegPath, Key, Value)
        Else
            WriteIni("Registry", Key, Value)
        End If
    End Sub
    Private Shared Sub RegistryDelete(Key As String)
        If OperatingSystem.IsWindows() Then
            RegistryUtils.Delete(RegPath, Key)
        Else
            DeleteIniKey("Registry", Key)
        End If
    End Sub
    Private Shared Function RegistryHas(Key As String) As Boolean
        If OperatingSystem.IsWindows() Then Return RegistryUtils.Has(RegPath, Key)
        Return HasIniKey("Registry", Key)
    End Function

#End Region

#Region "DES 加密"

    Private Shared ReadOnly desInitialVector As Byte() = Encoding.UTF8.GetBytes("95168702")

    ''' <summary>
    ''' 使用 DES 对称加密算法加密字符串。
    ''' </summary>
    Private Shared Function DesEncrypt(sourceString As String, Optional key As String = Nothing) As String
        key = If(key Is Nothing, "@;$ Abv2", key.GetStableHashCode().ToString().EnsureLength("X"c, 8).Substring(0, 8))
        Dim btKey As Byte() = Encoding.UTF8.GetBytes(key)
        Using des As DES = DES.Create()
            des.Key = btKey
            des.IV = desInitialVector
            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write)
                    Dim inData As Byte() = Encoding.UTF8.GetBytes(sourceString)
                    cs.Write(inData, 0, inData.Length)
                    cs.FlushFinalBlock()
                    Return Convert.ToBase64String(ms.ToArray())
                End Using
            End Using
        End Using
    End Function

    ''' <summary>
    ''' 使用 DES 对称加密算法解密字符串。
    ''' </summary>
    Private Shared Function DesDecrypt(encryptedString As String, Optional key As String = Nothing) As String
        key = If(key Is Nothing, "@;$ Abv2", key.GetStableHashCode().ToString().EnsureLength("X"c, 8).Substring(0, 8))
        Dim btKey As Byte() = Encoding.UTF8.GetBytes(key)
        Using des As DES = DES.Create()
            des.Key = btKey
            des.IV = desInitialVector
            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write)
                    Dim inData As Byte() = Convert.FromBase64String(encryptedString)
                    cs.Write(inData, 0, inData.Length)
                    cs.FlushFinalBlock()
                    Return Encoding.UTF8.GetString(ms.ToArray())
                End Using
            End Using
        End Using
    End Function

#End Region

End Class
