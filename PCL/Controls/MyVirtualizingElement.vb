Public Class MyVirtualizingElement(Of T As Control)
    Inherits Control

    Private Initializer As Func(Of T)
    Public Sub New(Initializer As Func(Of T))
        Me.Initializer = Initializer
        LazyLoadBehavior.OnFirstEnterScrollViewerViewport(Me, AddressOf Init)
    End Sub

    ''' <summary>
    ''' 实例化此控件。
    ''' </summary>
    Public Function Init() As T
        Dim Element As T = Initializer()
        If Parent IsNot Nothing Then
            If TypeOf Parent IsNot Panel Then Throw New Exception("MyVirtualizingElement 的父级必须是一个 Panel")
            Dim ParentPanel As Panel = Parent
            Dim CurrentIndex As Integer = ParentPanel.Children.IndexOf(Me)
            ParentPanel.Children.RemoveAt(CurrentIndex)
            ParentPanel.Children.Insert(CurrentIndex, Element)
        End If
        Return Element
    End Function
    Public Shared Widening Operator CType(Virtualized As MyVirtualizingElement(Of T)) As T
        Return Virtualized.Init()
    End Operator

End Class

'非泛型形式
Public Class MyVirtualizingElement
    Inherits Control

    Private Initializer As Func(Of Control)
    Public Sub New(Initializer As Func(Of Control))
        Me.Initializer = Initializer
        LazyLoadBehavior.OnFirstEnterScrollViewerViewport(Me, AddressOf Init)
    End Sub

    ''' <summary>
    ''' 实例化此控件。
    ''' </summary>
    Public Function Init() As Control
        Dim Element As Control = Initializer()
        If Parent IsNot Nothing Then
            If TypeOf Parent IsNot Panel Then Throw New Exception("MyVirtualizingElement 的父级必须是一个 Panel")
            Dim ParentPanel As Panel = Parent
            Dim CurrentIndex As Integer = ParentPanel.Children.IndexOf(Me)
            ParentPanel.Children.RemoveAt(CurrentIndex)
            ParentPanel.Children.Insert(CurrentIndex, Element)
        End If
        Return Element
    End Function

    ''' <summary>
    ''' 获取实例化后的控件。
    ''' 如果该控件没有实例化，则会立即实例化。
    ''' 如果类型错误，则返回原值。
    ''' </summary>
    Public Shared Function TryInit(Element As Control) As Control
        If GeneralUtils.IsGenericInstanceOf(Element, GetType(MyVirtualizingElement(Of ))) Then
            Return CType(Element, Object).Init()
        ElseIf TypeOf Element Is MyVirtualizingElement Then
            Return CType(Element, MyVirtualizingElement).Init()
        Else
            Return Element
        End If
    End Function

End Class
