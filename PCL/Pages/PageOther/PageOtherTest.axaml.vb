''' <summary>
''' “更多”页面的百宝箱子页面。
''' </summary>
Public Class PageOtherTest
    Inherits MyPageRight

    Public Sub New()
        InitControls()
    End Sub

    Public Shared Sub StartCustomDownload(Url As String, FileName As String, Optional Folder As String = Nothing)
    End Sub
    Public Shared Sub Jrrp()
    End Sub
    Public Shared Sub RubbishClear()
    End Sub
    Public Shared Sub MemoryOptimize(ShowHint As Boolean)
    End Sub
    Public Shared Sub MemoryOptimizeInternal(ShowHint As Boolean)
    End Sub
    Public Shared Function GetRandomCave() As String
        Return ""
    End Function
    Public Shared Function GetRandomHint() As String
        Return RandomOne(New List(Of String) From {
            "你可以在 设置 → 启动 中调整游戏内存分配。",
            "在 设置 → 个性化 中可以更换主题颜色和背景图片。",
            "把背景图片放进 PCL 数据文件夹的 Pictures 文件夹，PCL 会在每次启动时随机加载一张。",
            "PCL.Avalonia 是原版 PCL 的跨平台移植版本。",
            "遇到问题时，可以在 更多 → 帮助 中查找常见问题的解决办法。"
        })
    End Function
    Public Shared Function GetRandomPresetHint() As String
        Return ""
    End Function

End Class
