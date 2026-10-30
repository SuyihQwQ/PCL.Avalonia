Public Class MyDropShadow
    Inherits Border

    ''' <summary>
    ''' 阴影颜色。
    ''' </summary>
    Public Property Color As Color
        Get
            Return GetValue(ColorProperty)
        End Get
        Set(value As Color)
            SetValue(ColorProperty, value)
        End Set
    End Property
    Public Shared ReadOnly ColorProperty As StyledProperty(Of Color) =
        AvaloniaProperty.Register(Of MyDropShadow, Color)("Color", Color.FromArgb(&H71, 0, 0, 0))

    ''' <summary>
    ''' 阴影模糊半径。
    ''' </summary>
    Public Property ShadowRadius As Double
        Get
            Return GetValue(ShadowRadiusProperty)
        End Get
        Set(value As Double)
            SetValue(ShadowRadiusProperty, value)
        End Set
    End Property
    Public Shared ReadOnly ShadowRadiusProperty As StyledProperty(Of Double) =
        AvaloniaProperty.Register(Of MyDropShadow, Double)("ShadowRadius", 5.0)

    Public Sub New()
        IsHitTestVisible = False
        RefreshShadow()
    End Sub

    Protected Overrides Sub OnPropertyChanged(change As AvaloniaPropertyChangedEventArgs)
        MyBase.OnPropertyChanged(change)
        If change.Property Is ColorProperty OrElse change.Property Is ShadowRadiusProperty OrElse
           change.Property Is CornerRadiusProperty Then
            RefreshShadow()
        End If
    End Sub

    Private Sub RefreshShadow()
        BoxShadow = New BoxShadows(New BoxShadow With {
            .Color = Color,
            .Blur = ShadowRadius,
            .OffsetY = 1,
            .Spread = 0
        })
    End Sub

End Class
