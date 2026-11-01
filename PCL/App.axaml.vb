Imports Avalonia
Imports Avalonia.Controls.ApplicationLifetimes
Imports Avalonia.Markup.Xaml
Imports Avalonia.Threading

Public Class App
    Inherits Application

    Public Overrides Sub Initialize()
        AvaloniaXamlLoader.Load(Me)
    End Sub

    Public Overrides Sub OnFrameworkInitializationCompleted()
        FixDispatcherClock()
        ThemeRefresh()
        AddHandler ActualThemeVariantChanged, Sub() ThemeVariantChanged()
        If TypeOf ApplicationLifetime Is IClassicDesktopStyleApplicationLifetime Then
            DirectCast(ApplicationLifetime, IClassicDesktopStyleApplicationLifetime).MainWindow = New FormMain()
        End If
        MyBase.OnFrameworkInitializationCompleted()
    End Sub

    ''' <summary>
    ''' Avalonia 12 在 Wayland 平台初始化前就会创建 UI Dispatcher，其 Now 使用 Dispatcher 自带的 Stopwatch；
    ''' 之后接入的 ManagedDispatcherImpl 用另一个时钟等待定时器，两者固定偏差（平台初始化耗时）会让所有
    ''' DispatcherTimer 推迟同样时长，表现为动画只有几帧。这里把计时源改为与等待时钟一致的平台时钟。
    ''' </summary>
    Private Sub FixDispatcherClock()
        Try
            Dim DispatcherType = GetType(Dispatcher)
            Dim ImplField = DispatcherType.GetField("_impl", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
            Dim TimeProviderField = DispatcherType.GetField("_timeProvider", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
            If ImplField Is Nothing OrElse TimeProviderField Is Nothing Then Return
            Dim Impl = ImplField.GetValue(Dispatcher.UIThread)
            If Impl Is Nothing Then Return
            Dim NowProperty = Impl.GetType().GetProperty("Now", Reflection.BindingFlags.Public Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
            If NowProperty Is Nothing Then Return
            Dim ImplNow = CLng(NowProperty.GetValue(Impl))
            Dim OldProvider = TryCast(TimeProviderField.GetValue(Dispatcher.UIThread), Func(Of Long))
            Dim Skew = If(OldProvider Is Nothing, 0L, OldProvider() - ImplNow)
            If Math.Abs(Skew) <= 2 Then Return
            Dim PlatformNow As Func(Of Long) = Function() CLng(NowProperty.GetValue(Impl))
            TimeProviderField.SetValue(Dispatcher.UIThread, PlatformNow)
            Logger.Info($"已校正 Dispatcher 计时时钟，偏差 {Skew} ms")
        Catch ex As Exception
            Logger.Warn(ex, "校正 Dispatcher 计时时钟失败")
        End Try
    End Sub

End Class
