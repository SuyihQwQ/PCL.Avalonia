Imports Avalonia.LogicalTree

Public Class SettingService

#Region "属性"

    ''' <summary>
    ''' 指定该控件所对应的设置项。
    ''' </summary>
    Public Shared Function GetKey(obj As Control) As String
        If obj Is Nothing Then Return Nothing
        Return obj.GetValue(KeyProperty)
    End Function
    Public Shared Sub SetKey(obj As Control, value As String)
        obj.SetValue(KeyProperty, value)
    End Sub
    Public Shared ReadOnly KeyProperty As AttachedProperty(Of String) =
        AvaloniaProperty.RegisterAttached(Of SettingService, Control, String)("Key", Nothing)

    ''' <summary>
    ''' 指定该控件所对应的设置值。
    ''' </summary>
    Public Shared Function GetValue(obj As Control) As String
        If obj Is Nothing Then Return Nothing
        Return obj.GetValue(ValueProperty)
    End Function
    Public Shared Sub SetValue(obj As Control, value As String)
        obj.SetValue(ValueProperty, value)
    End Sub
    Public Shared ReadOnly ValueProperty As AttachedProperty(Of String) =
        AvaloniaProperty.RegisterAttached(Of SettingService, Control, String)("Value", Nothing)

#End Region

    ''' <summary>
    ''' 重置该控件以及它所有的子控件的设置值。
    ''' </summary>
    Public Shared Sub ResetSettings(Target As Control)
        If Target Is Nothing Then Return
        For Each Child In Target.GetLogicalChildren()
            If TypeOf Child Is Control Then ResetSettings(Child)
        Next
        Dim Key = GetKey(Target)
        If Key Is Nothing Then Return
        Settings.Reset(Key, PageInstanceLeft.Instance?.PathVersion)
    End Sub

    ''' <summary>
    ''' 更新该控件以及它所有的子控件当前显示的设置值。
    ''' </summary>
    Public Shared Sub RefreshSettings(Target As Control)
        If Target Is Nothing Then Return
        For Each Child In Target.GetLogicalChildren()
            If TypeOf Child Is Control Then RefreshSettings(Child)
        Next
        Dim Key = GetKey(Target)
        If Key Is Nothing Then Return
        Dim NewValue = Settings.Get(Key, PageInstanceLeft.Instance?.PathVersion)
        If TypeOf Target Is ISettingControl Then DirectCast(Target, ISettingControl).RefreshSetting(CStr(NewValue))
    End Sub

    ''' <summary>
    ''' 当控件对应的设置改变时，将内容保存到设置中。
    ''' </summary>
    Public Shared Sub SaveSetting(sender As Object)
        If AniControlEnabled <> 0 Then Return
        If TypeOf sender IsNot ISettingControl Then Return
        Dim Control = TryCast(sender, Control)
        If Control Is Nothing Then Return
        '控件加载前，XAML 中的默认值不应覆盖已保存的设置
        If Not Control.IsLoaded Then Return
        Dim Key = GetKey(Control)
        If Key Is Nothing Then Return
        Dim NewValue As String = DirectCast(sender, ISettingControl).GetCurrentSetting()
        If NewValue Is Nothing Then Return
        Settings.Set(Key, NewValue, PageInstanceLeft.Instance?.PathVersion)
    End Sub

End Class

Public Interface ISettingControl

    ''' <summary>
    ''' 更新该控件当前所显示的设置值。
    ''' </summary>
    Sub RefreshSetting(NewValue As String)
    ''' <summary>
    ''' 获取该控件当前设定的设置值。
    ''' 若不应当关联该设置值，则返回 Nothing。
    ''' </summary>
    Function GetCurrentSetting() As String

End Interface
