'动画引擎模块
'使用 Ani 作为方法或属性的开头，使用 Aa 作为单个动画对象的开头（便于自动补全）

Imports Avalonia.Markup.Xaml.MarkupExtensions
Imports Avalonia.Threading

Public Module ModAnimation

#Region "声明"

    ''' <summary>
    ''' 动画速度。设置系统上线后再接入设置项。
    ''' </summary>
    Public ReadOnly Property AniSpeed As Double
        Get
            Return 1.0
        End Get
    End Property
    ''' <summary>
    ''' 动画组列表。
    ''' </summary>
    Public AniGroups As New Dictionary(Of String, AniGroupEntry)
    Public Class AniGroupEntry
        Public Data As List(Of AniData)
        Public StartTick As Long
        Public Uuid As Integer = GetUuid()
    End Class
    ''' <summary>
    ''' 上一次记刻的时间。
    ''' </summary>
    Private AniLastTick As Long
    ''' <summary>
    ''' 动画模块是否正在运行。
    ''' </summary>
    Public AniRunning As Boolean = False
    Private _AniControlEnabled As Integer = 0
    Private ReadOnly AniControlEnabledLock As New Object
    ''' <summary>
    ''' 控件动画执行是否开启。先 +1，再 -1。
    ''' </summary>
    Public Property AniControlEnabled() As Integer
        Get
            Return _AniControlEnabled
        End Get
        Set(value As Integer)
            SyncLock AniControlEnabledLock
                _AniControlEnabled = value
            End SyncLock
        End Set
    End Property

    Private AniTimerInstance As DispatcherTimer

#End Region

#Region "类与枚举"

    ''' <summary>
    ''' 单个动画对象。
    ''' </summary>
    Public Structure AniData

        Public TypeMain As AniType
        Public TypeSub As AniTypeSub

        ''' <summary>
        ''' 动画总长度。
        ''' </summary>
        Public TimeTotal As Integer
        ''' <summary>
        ''' 已经执行的动画长度。如果为负数则为延迟。
        ''' </summary>
        Public TimeFinished As Double
        ''' <summary>
        ''' 已经完成的百分比。
        ''' </summary>
        Public TimePercent As Double

        ''' <summary>
        ''' 是否为“以后”。
        ''' </summary>
        Public IsAfter As Boolean

        ''' <summary>
        ''' 插值器类型。
        ''' </summary>
        Public Ease As AniEase
        ''' <summary>
        ''' 动画对象。
        ''' </summary>
        Public Obj As Object
        ''' <summary>
        ''' 动画值。
        ''' </summary>
        Public Value As Object
        ''' <summary>
        ''' 上次执行时的动画值。
        ''' </summary>
        Public ValueLast As Object

        Public Overrides Function ToString() As String
            Return TypeMain.ToString() & " | " & TimeFinished & "/" & TimeTotal & "(" & Math.Round(TimePercent * 100) & "%)" & If(Obj Is Nothing, "", " | " & Obj.ToString & "(" & Obj.GetType.Name & ")")
        End Function

    End Structure

    ''' <summary>
    ''' 动画基础种类。
    ''' </summary>
    Public Enum AniType
        ''' <summary>
        ''' 单个 Double 的动画，包括位置、长宽、透明度等。这需要附属类型。
        ''' </summary>
        Number
        ''' <summary>
        ''' 颜色属性的动画。这需要附属类型。
        ''' </summary>
        Color
        ''' <summary>
        ''' 缩放控件大小。比起 4 个 DoubleAnimation 来说效率更高。
        ''' </summary>
        Scale
        ''' <summary>
        ''' 文字一个个出现。
        ''' </summary>
        TextAppear
        ''' <summary>
        ''' 执行代码。
        ''' </summary>
        Code
        ''' <summary>
        ''' 以变换方式缩放控件。
        ''' </summary>
        ScaleTransform
        ''' <summary>
        ''' 以变换方式旋转控件。
        ''' </summary>
        RotateTransform
    End Enum
    ''' <summary>
    ''' 动画扩展种类。
    ''' </summary>
    Public Enum AniTypeSub
        X
        Y
        Width
        Height
        Opacity
        Value
        Radius
        BorderThickness
        StrokeThickness
        TranslateX
        TranslateY
        [Double]
        DoubleParam
        GridLengthWidth
    End Enum

#End Region

#Region "种类"

    'DoubleAnimation

    Public Function AaX(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.X,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaY(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Y,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaWidth(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Width,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaHeight(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Height,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaOpacity(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Opacity,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaValue(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Value,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaRadius(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Radius,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaBorderThickness(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.BorderThickness,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaStrokeThickness(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.StrokeThickness,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    Public Function AaGridLengthWidth(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.GridLengthWidth,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    'DoubleAnimation（Obj, Prop, [Res]）

    ''' <summary>
    ''' 改变数字属性的动画。
    ''' </summary>
    Public Function AaDouble(Obj As Object, Prop As AvaloniaProperty, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.Double, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = {Obj, Prop, ""}, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    ''' <summary>
    ''' 获取数字动画值。
    ''' </summary>
    Public Function AaDouble(Lambda As ParameterizedThreadStart, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.DoubleParam, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Lambda, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    'ColorAnimation（Obj, Prop, [Res]）

    ''' <summary>
    ''' 改变颜色属性的动画。
    ''' </summary>
    Public Function AaColor(Obj As Control, Prop As AvaloniaProperty, Value As MyColor, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Color, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = {Obj, Prop, ""}, .Value = Value, .IsAfter = After, .TimeFinished = -Delay, .ValueLast = New MyColor(0, 0, 0, 0)}
    End Function
    ''' <summary>
    ''' 改变颜色属性为一个资源的动画。
    ''' </summary>
    Public Function AaColor(Obj As Control, Prop As AvaloniaProperty, Res As String, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Color, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = {Obj, Prop, Res}, .Value = New MyColor(Application.Current.FindResource(Res)) - New MyColor(Obj.GetValue(Prop)), .IsAfter = After, .TimeFinished = -Delay, .ValueLast = New MyColor(0, 0, 0, 0)}
    End Function

    'Scale

    ''' <summary>
    ''' 缩放控件的动画。
    ''' </summary>
    Public Function AaScale(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False, Optional Absolute As Boolean = False) As AniData
        Dim ChangeRect As MyRect
        Dim Target As Control = Obj
        If Absolute Then
            ChangeRect = New MyRect(-0.5 * Value, -0.5 * Value, Value, Value)
        Else
            ChangeRect = New MyRect(-0.5 * Target.Bounds.Width * Value, -0.5 * Target.Bounds.Height * Value, Target.Bounds.Width * Value, Target.Bounds.Height * Value)
        End If
        Return New AniData With {.TypeMain = AniType.Scale, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = ChangeRect, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    'TextAppear

    ''' <summary>
    ''' 让一段文字一个个字出现或消失的动画。
    ''' </summary>
    Public Function AaTextAppear(Obj As Object, Optional Hide As Boolean = False, Optional TimePerText As Boolean = True, Optional Time As Integer = 70, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.TextAppear, .Ease = If(Ease, New AniEaseLinear), .TimeTotal = If(TimePerText, Time * If(TypeOf Obj Is TextBlock, Obj.Text, Obj.Context.ToString).ToString.Length, Time), .Obj = Obj, .Value = {If(TypeOf Obj Is TextBlock, Obj.Text, Obj.Context.ToString), Hide}, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    'Code

    ''' <summary>
    ''' 执行代码。这将会在执行时在主线程调用。
    ''' </summary>
    Public Function AaCode(Code As ThreadStart, Optional Delay As Integer = 0, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Code,
                                   .TimeTotal = 1, .Value = Code, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    'ScaleTransform

    ''' <summary>
    ''' 以变换方式缩放控件的动画。
    ''' </summary>
    Public Function AaScaleTransform(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.ScaleTransform, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    'RotateTransform

    ''' <summary>
    ''' 以变换方式旋转控件的动画。
    ''' </summary>
    Public Function AaRotateTransform(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.RotateTransform, .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    'TranslateTransform

    ''' <summary>
    ''' 利用 TranslateTransform 移动 X 轴的动画，这不会造成布局更新。
    ''' </summary>
    Public Function AaTranslateX(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.TranslateX,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function
    ''' <summary>
    ''' 利用 TranslateTransform 移动 Y 轴的动画，这不会造成布局更新。
    ''' </summary>
    Public Function AaTranslateY(Obj As Object, Value As Double, Optional Time As Integer = 400, Optional Delay As Integer = 0, Optional Ease As AniEase = Nothing, Optional After As Boolean = False) As AniData
        Return New AniData With {.TypeMain = AniType.Number, .TypeSub = AniTypeSub.TranslateY,
                                   .TimeTotal = Time, .Ease = If(Ease, New AniEaseLinear), .Obj = Obj, .Value = Value, .IsAfter = After, .TimeFinished = -Delay}
    End Function

    '特殊

    ''' <summary>
    ''' 将一个 StackPanel 中的各个项目依次显示。
    ''' </summary>
    Public Function AaStack(Stack As StackPanel, Optional Time As Integer = 100, Optional Delay As Integer = 25) As List(Of AniData)
        Dim Result = New List(Of AniData)
        Dim AniDelay As Integer = 0
        For Each Item In Stack.Children
            Item.Opacity = 0
            Result.Add(AaOpacity(Item, 1, Time, AniDelay))
            AniDelay += Delay
        Next
        Return Result
    End Function

#End Region

#Region "缓动函数"

    Public Enum AniEasePower As Integer
        Weak = 2
        Middle = 3
        Strong = 4
        ExtraStrong = 5
    End Enum
    ''' <summary>
    ''' 缓动函数基类。
    ''' </summary>
    Public MustInherit Class AniEase
        Public MustOverride Function GetValue(t As Double) As Double
        Public Overridable Function GetDelta(t1 As Double, t0 As Double) As Double
            Return GetValue(t1) - GetValue(t0)
        End Function
    End Class
    ''' <summary>
    ''' 渐入渐出组合。
    ''' </summary>
    Public Class AniEaseInout
        Inherits AniEase
        Private ReadOnly EaseIn As AniEase, EaseOut As AniEase, EaseInPercent As Double
        Public Sub New(EaseIn As AniEase, EaseOut As AniEase, Optional EaseInPercent As Double = 0.5)
            Me.EaseIn = EaseIn : Me.EaseOut = EaseOut : Me.EaseInPercent = EaseInPercent
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            If t < EaseInPercent Then
                Return EaseInPercent * EaseIn.GetValue(t / EaseInPercent)
            Else
                Return (1 - EaseInPercent) * EaseOut.GetValue((t - EaseInPercent) / (1 - EaseInPercent)) + EaseInPercent
            End If
        End Function
    End Class

    'Linear / 线性
    Public Class AniEaseLinear
        Inherits AniEase
        Public Overrides Function GetValue(t As Double) As Double
            Return t.Clamp(0, 1)
        End Function
        Public Overrides Function GetDelta(t1 As Double, t0 As Double) As Double
            Return t1.Clamp(0, 1) - t0.Clamp(0, 1)
        End Function
    End Class

    'Fluent / 平滑
    Public Class AniEaseInFluent
        Inherits AniEase
        Private ReadOnly p As AniEasePower
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return t.Clamp(0, 1) ^ p
        End Function
    End Class
    Public Class AniEaseOutFluent
        Inherits AniEase
        Private ReadOnly p As AniEasePower
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return 1 - (1 - t).Clamp(0, 1) ^ p
        End Function
    End Class
    Public Class AniEaseInoutFluent
        Inherits AniEase
        Private Ease As AniEaseInout
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle, Optional Middle As Double = 0.5)
            Ease = New AniEaseInout(New AniEaseInFluent(Power), New AniEaseOutFluent(Power), Middle)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Ease.GetValue(t)
        End Function
    End Class
    ''' <summary>
    ''' 以特定速度开始的平滑结束。
    ''' </summary>
    Public Class AniEaseOutFluentWithInitial
        Inherits AniEase
        Private ReadOnly alpha As Double '(初速度 / 平均速度) – 1
        Public Sub New(InitialPixelPerSecond As Double, TotalSecond As Double, TotalDistance As Double)
            Dim v0_norm As Double = InitialPixelPerSecond * TotalSecond / TotalDistance
            alpha = v0_norm - 1.0
            If alpha < 0 Then alpha = 0
        End Sub
        Public Overrides Function GetValue(percent As Double) As Double
            Dim p As Double = percent.Clamp(0, 1)
            If alpha = 0 Then Return p
            Return (alpha + 1) * p / (1 + alpha * p)
        End Function
    End Class

    'Back / 回弹
    Public Class AniEaseInBack
        Inherits AniEase
        Private ReadOnly p As Double
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = 3 - Power * 0.5
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = t.Clamp(0, 1)
            Return t ^ p * Math.Cos(1.5 * Math.PI * (1 - t))
        End Function
    End Class
    Public Class AniEaseOutBack
        Inherits AniEase
        Private ReadOnly p As Double
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = 3 - Power * 0.5
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = t.Clamp(0, 1)
            Return 1 - (1 - t) ^ p * Math.Cos(1.5 * Math.PI * t)
        End Function
    End Class

    'Car / 平滑-回弹
    Public Class AniEaseInCar
        Inherits AniEase
        Private Ease As AniEaseInout
        Public Sub New(Optional Middle As Double = 0.7, Optional Power As AniEasePower = AniEasePower.Middle)
            Ease = New AniEaseInout(New AniEaseInBack(Power), New AniEaseOutFluent(Power), Middle)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Ease.GetValue(t)
        End Function
    End Class
    Public Class AniEaseOutCar
        Inherits AniEase
        Private Ease As AniEaseInout
        Public Sub New(Optional Middle As Double = 0.3, Optional Power As AniEasePower = AniEasePower.Middle)
            Ease = New AniEaseInout(New AniEaseInFluent(Power), New AniEaseOutBack(Power), Middle)
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            Return Ease.GetValue(t)
        End Function
    End Class

    'Elastic / 弹簧
    Public Class AniEaseInElastic
        Inherits AniEase
        Private ReadOnly p As Integer '6~9
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power + 4
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = t.Clamp(0, 1)
            Return t ^ ((p - 1) * 0.25) * Math.Cos((p - 3.5) * Math.PI * (1 - t) ^ 1.5)
        End Function
    End Class
    Public Class AniEaseOutElastic
        Inherits AniEase
        Private ReadOnly p As Integer
        Public Sub New(Optional Power As AniEasePower = AniEasePower.Middle)
            p = Power + 4
        End Sub
        Public Overrides Function GetValue(t As Double) As Double
            t = 1 - t.Clamp(0, 1)
            Return 1 - t ^ ((p - 1) * 0.25) * Math.Cos((p - 3.5) * Math.PI * (1 - t) ^ 1.5)
        End Function
    End Class

#End Region

#Region "接口（开始、中断、检测）"

    ''' <summary>
    ''' 开始一个动画组。
    ''' </summary>
    ''' <param name="AniGroup">由 Aa 开头的函数初始化的 AniData 对象集合。</param>
    ''' <param name="Name">动画组的名称。如果重复会直接停止同名动画组。</param>
    Public Sub AniStart(AniGroup As IList, Optional Name As String = "", Optional RefreshTime As Boolean = False)
        If RefreshTime Then AniLastTick = GetTimeMs()
        Dim AllAnis = New List(Of AniData)
        For Each Element In AniGroup
            If TypeOf Element Is ICollection Then
                AllAnis.AddRange(Element)
            Else
                AllAnis.Add(Element)
            End If
        Next
        Dim NewEntry As New AniGroupEntry With {.Data = AllAnis, .StartTick = GetTimeMs()}
        If Name = "" Then
            Name = NewEntry.Uuid
        Else
            AniStop(Name)
        End If
        AniGroups.Add(Name, NewEntry)
    End Sub
    ''' <summary>
    ''' 开始一个动画组。
    ''' </summary>
    Public Sub AniStart(AniGroup As AniData, Optional Name As String = "", Optional RefreshTime As Boolean = False)
        AniStart(New List(Of AniData) From {AniGroup}, Name, RefreshTime)
    End Sub
    ''' <summary>
    ''' 直接停止一个动画组。
    ''' </summary>
    ''' <param name="Name">需要停止的动画组的名称。</param>
    Public Sub AniStop(Name As String)
        AniGroups.Remove(Name)
    End Sub
    ''' <summary>
    ''' 获取动画是否正在进行中。
    ''' </summary>
    Public Function AniIsRun(Name As String) As Boolean
        Return AniGroups.ContainsKey(Name)
    End Function

#End Region

    Private AniCount As Integer = 0
    Private AniFPSCounter As Integer = 0
    Private AniFPSTimer As Long = 0
    ''' <summary>
    ''' 当前的动画 FPS。
    ''' </summary>
    Public AniFPS As Integer = 0

    ''' <summary>
    ''' 重置动画计时起点，避免长时间布局后第一帧把动画直接跳到结束。
    ''' </summary>
    Public Sub AniResetClock()
        AniLastTick = GetTimeMs()
    End Sub

    ''' <summary>
    ''' 开始动画执行（UI 线程上的 DispatcherTimer 驱动）。
    ''' </summary>
    Public Sub AniStart()
        If Not RunInUi() Then
            RunInUi(AddressOf AniStart)
            Return
        End If
        If AniRunning Then Return
        AniLastTick = GetTimeMs()
        AniFPSTimer = AniLastTick
        AniRunning = True
        AniTimerInstance = New DispatcherTimer(DispatcherPriority.Render) With {.Interval = TimeSpan.FromMilliseconds(16)}
        AddHandler AniTimerInstance.Tick, AddressOf AniEngineTick
        AniTimerInstance.Start()
        Logger.Info("动画计时器开始")
    End Sub

    Private Sub AniEngineTick(sender As Object, e As EventArgs)
        Try
            Dim DeltaTime As Long = (GetTimeMs() - AniLastTick).Clamp(0, 100000)
            If DeltaTime < 3 Then Return
            If ModeDebug Then
                If (GetTimeMs() - AniFPSTimer).Clamp(0, 100000) >= 500 Then
                    AniFPS = AniFPSCounter
                    AniFPSCounter = 0
                    AniFPSTimer = GetTimeMs()
                End If
                AniFPSCounter += 2
            End If
            AniCount = 0
            AniTimer(DeltaTime * AniSpeed)
        Catch ex As Exception
            Logger.Error(ex, "动画帧执行失败", LogBehavior.AlertThenCrash)
        End Try
    End Sub

    ''' <summary>
    ''' 动画定时器事件。
    ''' </summary>
    Public Sub AniTimer(DeltaTick As Double)
        Try
            AniLastTick = GetTimeMs()
            If DeltaTick / AniSpeed > 100 Then Logger.Info($"两个动画帧间隔 {DeltaTick} ms")
            Dim i As Integer = -1
            '循环每个动画组
            Do While i + 1 < AniGroups.Count
                i += 1
                '初始化
                Dim Entry As AniGroupEntry = AniGroups.Values(i)
                If Entry.StartTick > AniLastTick Then Continue Do
                Dim CanRemoveAfter = True
                Dim ii = 0

                '循环每个动画
                Do While ii < Entry.Data.Count
                    Dim Anim As AniData = Entry.Data(ii)
                    '执行种类
                    If Anim.IsAfter = False Then '之前
                        CanRemoveAfter = False
                        Anim.TimeFinished += DeltaTick
                        If Anim.TimeFinished > 0 Then
                            Anim = AniRun(Anim)
                            AniCount += 1
                        End If
                        '如果当前动画已执行完毕
                        If Anim.TimeFinished >= Anim.TimeTotal Then
                            '如果是去向颜色资源的动画，恢复资源引用
                            If Anim.TypeMain = AniType.Color AndAlso CStr(Anim.Obj(2)) <> "" Then AniRestoreResource(Anim)
                            Entry.Data.RemoveAt(ii)
                            GoTo NextAni
                        End If
                        Entry.Data(ii) = Anim
                    Else '之后
                        If CanRemoveAfter Then
                            CanRemoveAfter = False
                            Anim.IsAfter = False
                            Entry.Data(ii) = Anim
                            GoTo NextAni
                        Else
                            Exit Do
                        End If
                    End If
                    ii += 1
NextAni:
                Loop

                '如果当前动画组都执行完毕则删除
                If Not Entry.Data.Any() Then
                    For Current = 0 To AniGroups.Count - 1
                        If AniGroups.ElementAt(Current).Value.Uuid = Entry.Uuid Then
                            AniGroups.Remove(AniGroups.ElementAt(Current).Key)
                            Exit For
                        End If
                    Next
                    i -= 1
                End If
            Loop

        Catch ex As Exception
            Logger.Error(ex, "动画刻执行失败", LogBehavior.Toast)
        End Try
    End Sub

    ''' <summary>
    ''' 执行一个动画。
    ''' </summary>
    ''' <param name="Ani">执行的动画对象。</param>
    Private Function AniRun(Ani As AniData) As AniData
        Try
            Select Case Ani.TypeMain

                Case AniType.Number
                    Dim Delta As Double = MathUtils.Lerp(0, Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent))
                    If Delta <> 0 Then
                        Select Case Ani.TypeSub
                            Case AniTypeSub.X
                                If TypeOf Ani.Obj Is Window Then
                                    Dim W As Window = Ani.Obj
                                    W.Position = New PixelPoint(W.Position.X + CInt(Math.Round(Delta * W.RenderScaling)), W.Position.Y)
                                Else
                                    Dim Obj As Control = Ani.Obj
                                    Select Case Obj.HorizontalAlignment
                                        Case HorizontalAlignment.Left, HorizontalAlignment.Stretch
                                            Obj.Margin = New Thickness(Obj.Margin.Left + Delta, Obj.Margin.Top, Obj.Margin.Right, Obj.Margin.Bottom)
                                        Case HorizontalAlignment.Right
                                            Obj.Margin = New Thickness(Obj.Margin.Left, Obj.Margin.Top, Obj.Margin.Right - Delta, Obj.Margin.Bottom)
                                    End Select
                                End If
                            Case AniTypeSub.Y
                                If TypeOf Ani.Obj Is Window Then
                                    Dim W As Window = Ani.Obj
                                    W.Position = New PixelPoint(W.Position.X, W.Position.Y + CInt(Math.Round(Delta * W.RenderScaling)))
                                Else
                                    Dim Obj As Control = Ani.Obj
                                    Select Case Obj.VerticalAlignment
                                        Case VerticalAlignment.Top
                                            Obj.Margin = New Thickness(Obj.Margin.Left, Obj.Margin.Top + Delta, Obj.Margin.Right, Obj.Margin.Bottom)
                                        Case VerticalAlignment.Bottom
                                            Obj.Margin = New Thickness(Obj.Margin.Left, Obj.Margin.Top, Obj.Margin.Right, Obj.Margin.Bottom - Delta)
                                    End Select
                                End If
                            Case AniTypeSub.Opacity
                                If TypeOf Ani.Obj Is Control Then
                                    Dim Obj As Control = Ani.Obj
                                    Obj.Opacity = (Obj.Opacity + Delta).Clamp(0, 1)
                                End If
                            Case AniTypeSub.Width
                                Dim Obj As Control = Ani.Obj
                                Obj.Width = Math.Max(If(Double.IsNaN(Obj.Width), Obj.Bounds.Width, Obj.Width) + Delta, 0)
                            Case AniTypeSub.Height
                                Dim Obj As Control = Ani.Obj
                                Obj.Height = Math.Max(If(Double.IsNaN(Obj.Height), Obj.Bounds.Height, Obj.Height) + Delta, 0)
                            Case AniTypeSub.Value
                                Ani.Obj.Value += Delta
                            Case AniTypeSub.Radius
                                Ani.Obj.Radius += Delta
                            Case AniTypeSub.StrokeThickness
                                Ani.Obj.StrokeThickness = Math.Max(Ani.Obj.StrokeThickness + Delta, 0)
                            Case AniTypeSub.BorderThickness
                                Ani.Obj.BorderThickness = New Thickness(CType(Ani.Obj.BorderThickness, Thickness).Bottom + Delta)
                            Case AniTypeSub.TranslateX
                                EnsureTranslateTransform(Ani.Obj).X += Delta
                            Case AniTypeSub.TranslateY
                                EnsureTranslateTransform(Ani.Obj).Y += Delta
                            Case AniTypeSub.Double
                                Dim Target As AvaloniaObject = Ani.Obj(0)
                                Dim Prop As AvaloniaProperty = Ani.Obj(1)
                                Target.SetValue(Prop, CDbl(Target.GetValue(Prop)) + Delta)
                            Case AniTypeSub.DoubleParam
                                CType(Ani.Obj, ParameterizedThreadStart)(Delta)
                            Case AniTypeSub.GridLengthWidth
                                Ani.Obj.Width = New GridLength(Math.Max(Ani.Obj.Width.Value + Delta, 0), GridUnitType.Star)
                        End Select
                    End If

                Case AniType.Color
                    '利用 ValueLast 记录了余下的小数值
                    Dim Delta As MyColor = MyColor.Lerp(New MyColor(0, 0, 0, 0), Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent)) + CType(Ani.ValueLast, MyColor)
                    Dim Obj As Control = Ani.Obj(0)
                    Dim Prop As AvaloniaProperty = Ani.Obj(1)
                    Dim NewColor As MyColor = New MyColor(Obj.GetValue(Prop)) + Delta
                    If Prop.PropertyType.Name = "Color" Then
                        Obj.SetValue(Prop, CType(NewColor, Color))
                    Else
                        Obj.SetValue(Prop, CType(NewColor, SolidColorBrush))
                    End If
                    Ani.ValueLast = NewColor - New MyColor(Obj.GetValue(Prop))

                Case AniType.Scale
                    Dim Obj As Control = Ani.Obj
                    Dim Delta As Double = Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent)
                    Obj.Margin = New Thickness(Obj.Margin.Left + MathUtils.Lerp(0, Ani.Value.Left, Delta), Obj.Margin.Top + MathUtils.Lerp(0, Ani.Value.Top, Delta), Obj.Margin.Right + MathUtils.Lerp(0, Ani.Value.Left, Delta), Obj.Margin.Bottom + MathUtils.Lerp(0, Ani.Value.Top, Delta))
                    Obj.Width = Math.Max(Obj.Width + MathUtils.Lerp(0, Ani.Value.Width, Delta), 0)
                    Obj.Height = Math.Max(Obj.Height + MathUtils.Lerp(0, Ani.Value.Height, Delta), 0)

                Case AniType.TextAppear
                    Dim Text As String = Ani.Value(0)
                    Dim TextCount As Integer = If(Ani.Value(1), Text.Length, 0) +
                                               Math.Round(Text.Length * If(Ani.Value(1), -1, 1) * Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, 0))
                    Dim NewText As String = Text.Substring(0, TextCount)
                    '添加乱码
                    If TextCount < Text.Length Then
                        Dim NextText As String = Text.Substring(TextCount, 1)
                        If Convert.ToInt32(Convert.ToChar(NextText)) >= Convert.ToInt32(Convert.ToChar(128)) Then
                            NewText &= Encoding.GetEncoding("GB18030").GetString(New Byte() {RandomInteger(16 + 160, 87 + 160), RandomInteger(1 + 160, 89 + 160)})
                        Else
                            NewText &= RandomInteger(0, 85).ConvertRadix(86)
                        End If
                    End If
                    If TypeOf Ani.Obj Is TextBlock Then
                        Ani.Obj.Text = NewText
                    Else
                        Ani.Obj.Context = NewText
                    End If

                Case AniType.Code
                    Ani.TimeFinished = Ani.TimeTotal
                    CType(Ani.Value, ThreadStart)()

                Case AniType.ScaleTransform
                    Dim Scale As ScaleTransform = EnsureScaleTransform(Ani.Obj)
                    Dim Delta As Double = MathUtils.Lerp(0, Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent))
                    Scale.ScaleX = Math.Max(Scale.ScaleX + Delta, 0)
                    Scale.ScaleY = Math.Max(Scale.ScaleY + Delta, 0)

                Case AniType.RotateTransform
                    Dim Rotate As RotateTransform = EnsureRotateTransform(Ani.Obj)
                    Dim Delta As Double = MathUtils.Lerp(0, Ani.Value, Ani.Ease.GetDelta(Ani.TimeFinished / Ani.TimeTotal, Ani.TimePercent))
                    Rotate.Angle += Delta

            End Select
            Ani.TimePercent = Ani.TimeFinished / Ani.TimeTotal
        Catch ex As Exception
            Logger.Error(ex, $"执行动画失败：{Ani}", LogBehavior.Toast)
        End Try
        Return Ani
    End Function

    ''' <summary>
    ''' 获取控件当前缩放比例，供增量缩放动画使用。
    ''' </summary>
    Public Function AniScaleX(Obj As Control) As Double
        If Obj.RenderTransform Is Nothing Then Return 1
        If TypeOf Obj.RenderTransform Is ScaleTransform Then Return DirectCast(Obj.RenderTransform, ScaleTransform).ScaleX
        Dim Group = TryCast(Obj.RenderTransform, TransformGroup)
        If Group Is Nothing Then Return 1
        Dim Result = Group.Children.OfType(Of ScaleTransform)().FirstOrDefault()
        Return If(Result Is Nothing, 1, Result.ScaleX)
    End Function

    ''' <summary>
    ''' 直接设置控件的缩放比例，兼容缩放、旋转、位移共用的 TransformGroup。
    ''' </summary>
    Public Sub AniSetScale(Obj As Control, Value As Double)
        Dim Scale = EnsureScaleTransform(Obj)
        Scale.ScaleX = Value
        Scale.ScaleY = Value
    End Sub

    ''' <summary>
    ''' 获取控件当前旋转角度，供增量旋转动画使用。
    ''' </summary>
    Public Function AniAngle(Obj As Control) As Double
        If Obj.RenderTransform Is Nothing Then Return 0
        If TypeOf Obj.RenderTransform Is RotateTransform Then Return DirectCast(Obj.RenderTransform, RotateTransform).Angle
        Dim Group = TryCast(Obj.RenderTransform, TransformGroup)
        If Group Is Nothing Then Return 0
        Dim Result = Group.Children.OfType(Of RotateTransform)().FirstOrDefault()
        Return If(Result Is Nothing, 0, Result.Angle)
    End Function

    ''' <summary>
    ''' Avalonia 的 RenderTransform 只有一个槽位，缩放、旋转、位移需要共享同一个 TransformGroup。
    ''' </summary>
    Private Function EnsureTransformGroup(Obj As Control) As TransformGroup
        Dim Group = TryCast(Obj.RenderTransform, TransformGroup)
        If Group Is Nothing Then
            Group = New TransformGroup
            If Obj.RenderTransform IsNot Nothing Then Group.Children.Add(Obj.RenderTransform)
            Obj.RenderTransform = Group
        End If
        Return Group
    End Function

    Private Function EnsureScaleTransform(Obj As Control) As ScaleTransform
        Dim Group = EnsureTransformGroup(Obj)
        Dim Result = Group.Children.OfType(Of ScaleTransform)().FirstOrDefault()
        If Result Is Nothing Then
            Result = New ScaleTransform(1, 1)
            Group.Children.Add(Result)
        End If
        Return Result
    End Function

    Private Function EnsureRotateTransform(Obj As Control) As RotateTransform
        Dim Group = EnsureTransformGroup(Obj)
        Dim Result = Group.Children.OfType(Of RotateTransform)().FirstOrDefault()
        If Result Is Nothing Then
            Result = New RotateTransform(0)
            Group.Children.Add(Result)
        End If
        Return Result
    End Function

    Private Function EnsureTranslateTransform(Obj As Control) As TranslateTransform
        Dim Group = EnsureTransformGroup(Obj)
        Dim Result = Group.Children.OfType(Of TranslateTransform)().FirstOrDefault()
        If Result Is Nothing Then
            Result = New TranslateTransform(0, 0)
            Group.Children.Add(Result)
        End If
        Return Result
    End Function

    ''' <summary>
    ''' 动画结束后恢复属性上的动态资源绑定，避免主题色切换失效。
    ''' </summary>
    Private Sub AniRestoreResource(Ani As AniData)
        Try
            Dim Obj As Control = Ani.Obj(0)
            Dim Prop As AvaloniaProperty = Ani.Obj(1)
            Dim Res As String = CStr(Ani.Obj(2))
            Obj.Bind(Prop, New DynamicResourceExtension(Res))
        Catch ex As Exception
            Logger.Warn(ex, $"恢复颜色资源 {Ani.Obj(2)} 失败")
        End Try
    End Sub

End Module
