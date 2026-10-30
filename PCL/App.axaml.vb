Imports Avalonia
Imports Avalonia.Controls.ApplicationLifetimes
Imports Avalonia.Markup.Xaml

Public Class App
    Inherits Application

    Public Overrides Sub Initialize()
        AvaloniaXamlLoader.Load(Me)
    End Sub

    Public Overrides Sub OnFrameworkInitializationCompleted()
        ThemeRefresh()
        If TypeOf ApplicationLifetime Is IClassicDesktopStyleApplicationLifetime Then
            DirectCast(ApplicationLifetime, IClassicDesktopStyleApplicationLifetime).MainWindow = New FormMain()
        End If
        MyBase.OnFrameworkInitializationCompleted()
    End Sub

End Class
