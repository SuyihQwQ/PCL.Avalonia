' SPDX-License-Identifier: Apache-2.0
Imports Avalonia
Imports Avalonia.Media

''' <summary>
''' 全局字体配置：内嵌 PCLEnglish、MiSans 与 Twemoji，
''' 或按设置完全使用系统字体；emoji 固定走字体回退表，避免 Skia 选中系统彩色 emoji。
''' </summary>
Public Module FontConfig

    '内嵌字体（PCL 英文 + 小米 MiSans 中文 + Twemoji 表情）
    Public Const EmbeddedFamily As String =
        "avares://PCL.Avalonia/Assets/Fonts/PCLEnglish.ttf#PCL English, " &
        "avares://PCL.Avalonia/Assets/Fonts/MiSans-Regular.ttf#MiSans, " &
        "avares://PCL.Avalonia/Assets/Fonts/Twemoji.Mozilla.ttf#Twemoji Mozilla"

    'Twemoji 的 URI 形式（FontFallbacks 用）
    Public Const EmojiFamily As String =
        "avares://PCL.Avalonia/Assets/Fonts/Twemoji.Mozilla.ttf#Twemoji Mozilla"

    '系统 emoji 字体（Twemoji 不可用时的兜底）
    Private ReadOnly EmojiSystemFamily As String =
        If(OperatingSystem.IsWindows(), "Segoe UI Emoji", "Noto Color Emoji")

    'emoji / 符号区段
    Private ReadOnly EmojiRanges As String() =
        {"U+2190-21FF", "U+2300-23FF", "U+25A0-27BF", "U+2B00-2BFF", "U+1F000-1FAFF"}

    ''' <summary>
    ''' 各平台默认 UI 字体链（“完全使用系统字体”时使用）。
    ''' </summary>
    Public Function GetSystemFamily() As String
        If OperatingSystem.IsWindows() Then Return "Microsoft YaHei UI, Segoe UI, Segoe UI Emoji"
        If OperatingSystem.IsMacOS() Then Return "PingFang SC, Helvetica Neue, Apple Color Emoji"
        Return "Noto Sans CJK SC, Noto Sans, Noto Color Emoji"
    End Function

    ''' <summary>
    ''' 当前设置的字体族字符串。
    ''' </summary>
    Public Function ActiveFamily() As String
        Try
            Return If(Settings.Get(Of Boolean)("UiSystemFont"), GetSystemFamily(), EmbeddedFamily)
        Catch ex As Exception
            Return EmbeddedFamily
        End Try
    End Function

    ''' <summary>
    ''' 启动时构建字体管理器配置。需要在 Avalonia 初始化前调用。
    ''' </summary>
    Public Function Create() As FontManagerOptions
        Dim Options As New FontManagerOptions With {.DefaultFamilyName = ActiveFamily()}
        Try
            If Not Settings.Get(Of Boolean)("UiSystemFont") Then
                Dim Fallbacks As New List(Of FontFallback)
                For Each Range In EmojiRanges
                    '优先内嵌 Twemoji，缺失时退回系统 emoji 字体
                    Fallbacks.Add(New FontFallback With {
                        .FontFamily = New FontFamily(EmojiFamily),
                        .UnicodeRange = UnicodeRange.Parse(Range)
                    })
                    Fallbacks.Add(New FontFallback With {
                        .FontFamily = New FontFamily(EmojiSystemFamily),
                        .UnicodeRange = UnicodeRange.Parse(Range)
                    })
                Next
                Options.FontFallbacks = Fallbacks
            End If
        Catch ex As Exception
            Logger.Warn(ex, "构建字体回退配置失败")
        End Try
        Return Options
    End Function

    ''' <summary>
    ''' 运行中切换字体（设置变化或启动完成后调用）。
    ''' 通过更新全局动态资源 PCLFont 生效；emoji 回退表在启动时确定，切换后建议重启。
    ''' </summary>
    Public Sub ApplyRuntime()
        Try
            Dim Family As New FontFamily(ActiveFamily())
            Dim CurrentApp = Application.Current
            If CurrentApp IsNot Nothing Then CurrentApp.Resources("PCLFont") = Family
            Dim MainWindow = ModMain.FrmMain
            If MainWindow IsNot Nothing Then MainWindow.FontFamily = Family
        Catch ex As Exception
            Logger.Warn(ex, "应用字体设置失败")
        End Try
    End Sub

End Module
