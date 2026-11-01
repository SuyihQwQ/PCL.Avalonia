Imports System.Runtime.InteropServices

''' <summary>
''' 通过 X11（含 XWayland）枚举窗口。Linux 下无法使用 user32，而 LWJGL2 等旧版本只会创建 X11 窗口。
''' 所有方法都应在同一个线程内调用同一个 Display 连接。
''' </summary>
Friend NotInheritable Class X11WindowHelper

    Private Const LibX11 As String = "libX11.so.6"
    Private Const XA_CARDINAL As ULong = 6

    <DllImport(LibX11, EntryPoint:="XOpenDisplay", CharSet:=CharSet.Ansi)>
    Public Shared Function XOpenDisplay(displayName As String) As IntPtr
    End Function

    <DllImport(LibX11)>
    Public Shared Function XCloseDisplay(display As IntPtr) As Integer
    End Function

    <DllImport(LibX11)>
    Private Shared Function XDefaultRootWindow(display As IntPtr) As ULong
    End Function

    <DllImport(LibX11, CharSet:=CharSet.Ansi)>
    Private Shared Function XInternAtom(display As IntPtr, atomName As String, onlyIfExists As Boolean) As ULong
    End Function

    <DllImport(LibX11)>
    Private Shared Function XGetWindowProperty(display As IntPtr, window As ULong, propertyAtom As ULong,
            longOffset As Long, longLength As Long, deleteProperty As Boolean, requiredType As ULong,
            ByRef actualType As ULong, ByRef actualFormat As Integer, ByRef itemCount As ULong,
            ByRef bytesAfter As ULong, ByRef data As IntPtr) As Integer
    End Function

    <DllImport(LibX11)>
    Private Shared Function XGetGeometry(display As IntPtr, drawable As ULong, ByRef root As ULong,
            ByRef x As Integer, ByRef y As Integer, ByRef width As Integer, ByRef height As Integer,
            ByRef borderWidth As Integer, ByRef depth As Integer) As Integer
    End Function

    <DllImport(LibX11)>
    Private Shared Function XFetchName(display As IntPtr, window As ULong, ByRef windowName As IntPtr) As Integer
    End Function

    <DllImport(LibX11)>
    Private Shared Function XFree(data As IntPtr) As Integer
    End Function

    ''' <summary>
    ''' 获取 _NET_CLIENT_LIST 中的全部窗口 ID。属性不存在（非 EWMH 窗口管理器）时返回空集合。
    ''' </summary>
    Public Shared Function GetClientWindows(display As IntPtr) As HashSet(Of Long)
        Dim Result As New HashSet(Of Long)
        Dim Atom = XInternAtom(display, "_NET_CLIENT_LIST", True)
        If Atom = 0 Then Return Result
        Dim ActualType As ULong = 0, ActualFormat As Integer = 0, ItemCount As ULong = 0
        Dim BytesAfter As ULong = 0, Data As IntPtr = IntPtr.Zero
        Dim Status = XGetWindowProperty(display, XDefaultRootWindow(display), Atom, 0, 65536, False, 0,
            ActualType, ActualFormat, ItemCount, BytesAfter, Data)
        If Status <> 0 OrElse Data = IntPtr.Zero Then Return Result
        Try
            If ActualFormat = 32 Then
                For i As Integer = 0 To CInt(ItemCount) - 1
                    Result.Add(Marshal.ReadIntPtr(Data, i * IntPtr.Size).ToInt64())
                Next
            End If
        Finally
            XFree(Data)
        End Try
        Return Result
    End Function

    ''' <summary>
    ''' 判断窗口是否可能是刚启动的游戏窗口：尺寸足够大、标题非空，且 _NET_WM_PID 与游戏进程一致（若窗口提供）。
    ''' </summary>
    Public Shared Function IsGameWindow(display As IntPtr, windowId As Long, gamePid As Integer) As Boolean
        Dim Root As ULong = 0, X As Integer = 0, Y As Integer = 0, Width As Integer = 0
        Dim Height As Integer = 0, BorderWidth As Integer = 0, Depth As Integer = 0
        If XGetGeometry(display, CULng(windowId), Root, X, Y, Width, Height, BorderWidth, Depth) = 0 Then Return False
        If Width < 200 OrElse Height < 150 Then Return False
        Dim WindowPid = GetWindowPid(display, CULng(windowId))
        If WindowPid <> 0 AndAlso WindowPid <> gamePid Then Return False
        Return Not String.IsNullOrWhiteSpace(GetWindowTitle(display, CULng(windowId)))
    End Function

    Private Shared Function GetWindowPid(display As IntPtr, windowId As ULong) As Integer
        Dim Atom = XInternAtom(display, "_NET_WM_PID", True)
        If Atom = 0 Then Return 0
        Dim ActualType As ULong = 0, ActualFormat As Integer = 0, ItemCount As ULong = 0
        Dim BytesAfter As ULong = 0, Data As IntPtr = IntPtr.Zero
        Dim Status = XGetWindowProperty(display, windowId, Atom, 0, 1, False, XA_CARDINAL,
            ActualType, ActualFormat, ItemCount, BytesAfter, Data)
        If Status <> 0 OrElse Data = IntPtr.Zero Then Return 0
        Try
            If ItemCount = 0 Then Return 0
            Return Marshal.ReadInt32(Data)
        Finally
            XFree(Data)
        End Try
    End Function

    Private Shared Function GetWindowTitle(display As IntPtr, windowId As ULong) As String
        '优先使用 UTF-8 的 _NET_WM_NAME，回退到 WM_NAME
        Dim Atom = XInternAtom(display, "_NET_WM_NAME", True)
        If Atom <> 0 Then
            Dim ActualType As ULong = 0, ActualFormat As Integer = 0, ItemCount As ULong = 0
            Dim BytesAfter As ULong = 0, Data As IntPtr = IntPtr.Zero
            If XGetWindowProperty(display, windowId, Atom, 0, 1024, False, 0,
                    ActualType, ActualFormat, ItemCount, BytesAfter, Data) = 0 AndAlso Data <> IntPtr.Zero Then
                Try
                    If ItemCount > 0 Then Return Marshal.PtrToStringUTF8(Data)
                Finally
                    XFree(Data)
                End Try
            End If
        End If
        Dim Name As IntPtr = IntPtr.Zero
        If XFetchName(display, windowId, Name) = 0 OrElse Name = IntPtr.Zero Then Return ""
        Try
            Return Marshal.PtrToStringAnsi(Name)
        Finally
            XFree(Name)
        End Try
    End Function

End Class
