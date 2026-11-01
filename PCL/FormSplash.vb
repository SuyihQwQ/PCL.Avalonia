Imports Avalonia.Controls
Imports Avalonia.Media
Imports Avalonia.Media.Imaging
Imports Avalonia.Platform

''' <summary>
''' 启动时居中显示的 PCL 图标，对应原版的 SplashScreen("Images\icon.ico")。
''' </summary>
Public Class FormSplash
    Inherits Window

    Public Sub New()
        WindowDecorations = WindowDecorations.None
        TransparencyLevelHint = {WindowTransparencyLevel.Transparent}
        TransparencyBackgroundFallback = Brushes.Transparent
        Background = Brushes.Transparent
        ShowInTaskbar = False
        ShowActivated = False
        CanResize = False
        CanMinimize = False
        CanMaximize = False
        WindowStartupLocation = WindowStartupLocation.CenterScreen
        SizeToContent = SizeToContent.WidthAndHeight
        Using Stream = AssetLoader.Open(New Uri("avares://PCL/Assets/Images/icon.png"))
            Dim IconBitmap As New Bitmap(Stream)
            Icon = New WindowIcon(IconBitmap)
            Content = New Image With {
                .Source = IconBitmap,
                .Width = 256,
                .Height = 256,
                .Stretch = Stretch.Uniform
            }
        End Using
    End Sub

End Class
