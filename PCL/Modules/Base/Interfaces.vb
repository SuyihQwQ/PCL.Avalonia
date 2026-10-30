''' <summary>
''' 指示该控件是可勾选的。
''' </summary>
Public Interface IMyRadio
    Event Check(sender As Object, e As RouteEventArgs)
    Event Changed(sender As Object, e As RouteEventArgs)
End Interface

''' <summary>
''' 指示该页面支持通过 F5 刷新。
''' </summary>
Public Interface IRefreshable
    Sub Refresh()
End Interface
