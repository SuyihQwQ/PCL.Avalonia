Public Class PageSpeedRight
    Inherits MyPageRight

    Public Sub New()
        InitControls()
    End Sub

    Private Sub Init() Handles Me.Loaded
        PanBack.ScrollToHome()
    End Sub

End Class
