Imports System.Text
Imports Avalonia

Public Module Program

    <STAThread>
    Public Sub Main(args As String())
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        MeloongCore.Main.Init("PCL")
        Logger.Instance = New FileLogger With {
            .logFolder = PathData,
            .MinLevel = If(ModeDebug, LogLevel.Trace, LogLevel.Info)
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args)
    End Sub

    Public Function BuildAvaloniaApp() As AppBuilder
        Return AppBuilder.Configure(Of App)() _
            .UsePlatformDetect() _
            .LogToTrace()
    End Function

End Module
