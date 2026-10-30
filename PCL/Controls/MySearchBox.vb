Imports Avalonia.Controls.Shapes

''' <summary>
''' 带搜索图标与清除按钮的搜索框。
''' </summary>
Public Class MySearchBox
    Inherits MyCard

    Public Event TextChanged(sender As Object, e As EventArgs)

    Public ReadOnly Property TextBox As MyTextBox
    Public ReadOnly Property BtnClear As MyIconButton

    Public Sub New()
        Height = 40
        TextBox = New MyTextBox With {
            .HasBackground = False,
            .BorderThickness = New Thickness(0),
            .Padding = New Thickness(32, 0, 40, 0),
            .MaxLength = 50,
            .FontSize = 13.5,
            .VerticalContentAlignment = VerticalAlignment.Center
        }
        Dim ShapeSearch As New Path With {
            .Width = 14,
            .Height = 14,
            .Margin = New Thickness(14, 0, 0, 0),
            .IsHitTestVisible = False,
            .Stretch = Stretch.Uniform,
            .HorizontalAlignment = HorizontalAlignment.Left,
            .Data = Geometry.Parse("M675.9168 242.816C556.3008 123.2 362.4448 123.2 242.8288 242.816 123.2128 362.432 123.2128 556.288 242.8288 675.904 362.4448 795.52 556.3008 795.52 675.9168 675.904 795.5328 556.288 795.5328 362.432 675.9168 242.816L675.9168 242.816ZM1000.7168 892.48C1030.6048 922.368 1030.6048 970.816 1000.7168 1000.704 970.8288 1030.656 922.3808 1030.656 892.4288 1000.704L730.0608 838.336C728.1408 836.416 727.4368 833.92 725.7728 831.872 546.3168 960.448 295.7568 945.408 134.5408 784.192-44.8512 604.8-44.8512 313.92 134.5408 134.528 313.9328-44.864 604.8128-44.864 784.2048 134.528 945.4208 295.744 960.4608 546.304 831.8848 725.76 833.9328 727.424 836.4288 728.128 838.3488 730.048L1000.7168 892.48Z")
        }
        ShapeSearch.SetResourceReference(Path.FillProperty, "ColorBrush1")
        BtnClear = New MyIconButton With {
            .IsHitTestVisible = False,
            .Opacity = 0,
            .Width = 24,
            .Height = 24,
            .Margin = New Thickness(0, 0, 10, 0),
            .LogoScale = 0.75,
            .Theme = MyIconButton.Themes.Black,
            .HorizontalAlignment = HorizontalAlignment.Right,
            .Logo = "F1 M2,0 L0,2 8,10 0,18 2,20 10,12 18,20 20,18 12,10 20,2 18,0 10,8 2,0Z"
        }
        Dim Panel As New Grid
        Panel.Children.Add(TextBox)
        Panel.Children.Add(ShapeSearch)
        Panel.Children.Add(BtnClear)
        Children.Add(Panel)
        AddHandler TextBox.TextChanged, AddressOf Text_TextChanged
        AddHandler BtnClear.Click, AddressOf BtnClear_Click
        AddHandler KeyUp, AddressOf MySearchBox_KeyUp
        AddHandler Loaded, AddressOf MySearchBox_Loaded
    End Sub

    Private Sub MySearchBox_Loaded(sender As Object, e As RoutedEventArgs)
        TextBox.Focus()
    End Sub

    '属性
    Public Property HintText As String
        Get
            Return TextBox.HintText
        End Get
        Set(value As String)
            TextBox.HintText = value
        End Set
    End Property
    Public Property Text As String
        Get
            Return TextBox.Text
        End Get
        Set(value As String)
            TextBox.Text = value
        End Set
    End Property

    Private Sub Text_TextChanged(sender As Object, e As TextChangedEventArgs)
        If String.IsNullOrEmpty(TextBox.Text) Then
            AniStart(AaOpacity(BtnClear, -BtnClear.Opacity, 90), "MySearchBox ClearBtn " & Uuid)
            BtnClear.IsHitTestVisible = False
        Else
            AniStart(AaOpacity(BtnClear, 1 - BtnClear.Opacity, 90), "MySearchBox ClearBtn " & Uuid)
            BtnClear.IsHitTestVisible = True
        End If
        RaiseEvent TextChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub BtnClear_Click(sender As Object, e As EventArgs)
        TextBox.Text = ""
        TextBox.Focus()
    End Sub

    Private Sub MySearchBox_KeyUp(sender As Object, e As KeyEventArgs)
        If e.Key = Key.Enter Then RaiseCustomEvent()
    End Sub

End Class
