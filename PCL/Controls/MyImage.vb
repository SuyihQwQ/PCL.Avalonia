Imports System.IO
Imports System.Net
Imports System.Net.Http

Public Class MyImage
    Inherits Image

#Region "公开属性"

    ''' <summary>
    ''' 网络图片的缓存有效期。
    ''' 在这个时间后，才会重新尝试下载图片。
    ''' </summary>
    Public FileCacheExpiredTime As New TimeSpan(7, 0, 0, 0) '7 天

    ''' <summary>
    ''' 是否允许将网络图片存储到本地用作缓存。
    ''' </summary>
    Public Property EnableCache As Boolean = True

    ''' <summary>
    ''' 与 Image 的 Source 类似。
    ''' 若输入以 http 开头的字符串，则会尝试下载图片然后显示；若 EnableCache 设为 True，图片还会保存为本地缓存。
    ''' 支持 WebP 格式的图片。
    ''' </summary>
    Public Shadows Property Source As String
        Get
            Return _Source
        End Get
        Set(value As String)
            If value = "" Then value = Nothing
            If value IsNot Nothing AndAlso value.StartsWith("//", StringComparison.Ordinal) Then value = "https:" & value
            If _Source = value Then Return
            _Source = value
            If Not IsInitialized Then Return '属性读取顺序修正：在完成 XAML 属性读取后再触发图片加载
            Load()
        End Set
    End Property
    Private _Source As String = ""

    ''' <summary>
    ''' 若 Source 是一个网络图片，该地址将作为第二图片源。
    ''' </summary>
    Public Property FallbackSource As String

    ''' <summary>
    ''' 正在下载网络图片时显示的本地图片。
    ''' </summary>
    Public Property LoadingSource As String = "avares://PCL/Assets/Images/Icons/NoIcon.png"

#End Region

    ''' <summary>
    ''' 实际被呈现的图片地址。
    ''' </summary>
    Public Property ActualSource As String
        Get
            Return _ActualSource
        End Get
        Set(value As String)
            If value = "" Then value = Nothing
            If _ActualSource = value Then Return
            _ActualSource = value
            Try
                Dim Pic As IImage = If(value Is Nothing, Nothing, New MyBitmap(value)) '在这里先触发可能的文件读取，尽量避免在 UI 线程中读取文件
                RunInUi(Sub() SetImageSource(Pic))
            Catch ex As Exception
                Logger.Warn(ex, $"加载图片失败（{value}）")
                Try
                    If Not String.IsNullOrEmpty(value) AndAlso value.StartsWithF(PathTemp, True) AndAlso File.Exists(value) Then File.Delete(value)
                Catch
                End Try
            End Try
        End Set
    End Property
    Private _ActualSource As String = Nothing

    Private Sub SetImageSource(Pic As IImage)
        MyBase.Source = Pic
    End Sub

    Private _LoadTriggered As Boolean

    Private Sub MyImage_LoadTrigger(sender As Object, e As EventArgs) Handles Me.Initialized, Me.Loaded
        '程序化创建时可能不会触发 Initialized，用 Loaded 兜底
        If _LoadTriggered Then Return
        _LoadTriggered = True
        Load()
    End Sub

    Private Sub Load()
        '空
        If Source Is Nothing Then
            ActualSource = Nothing
            Return
        End If
        '本地图片
        If Not Source.StartsWithF("http") Then
            ActualSource = Source
            Return
        End If
        '从缓存加载网络图片
        Dim CacheFolder As String = Path.Combine(PathTemp, "MyImage")
        Dim CacheName As String = Source.GetStableHashCode().ToString()
        If Not EnableCache Then CacheName &= GetUuid() '不启用缓存时在末尾加上随机字符串，避免冲突
        Dim TempPath As String = Path.Combine(CacheFolder, CacheName & ".png")
        Dim TempFile As New FileInfo(TempPath)
        If EnableCache AndAlso TempFile.Exists Then
            ActualSource = TempPath '显示缓存的图片
            If (Date.Now - TempFile.LastWriteTime) < FileCacheExpiredTime Then Return '无需刷新缓存
        Else
            ActualSource = LoadingSource '显示加载中图片
        End If
        RunInNewThread(
        Sub()
            Dim IsLocalFallback As Boolean = Not String.IsNullOrEmpty(FallbackSource) AndAlso Not FallbackSource.StartsWithF("http")
            Try
                Download(Source, TempPath, If(FallbackSource IsNot Nothing AndAlso FallbackSource.StartsWithF("http") AndAlso Source <> FallbackSource, FallbackSource, Nothing))
                ActualSource = TempPath
                If Not EnableCache Then File.Delete(TempPath)
            Catch ex As Exception
                Try
                    If File.Exists(TempPath) Then File.Delete(TempPath)
                Catch
                End Try
                '加载本地备用图片
                If IsLocalFallback Then
                    Try
                        ActualSource = FallbackSource
                        Logger.Warn(ex, $"下载图片失败，使用本地备用图片（{Source}）")
                        Return
                    Catch exx As Exception
                        Logger.Error(ex, $"下载图片失败（{Source}）", LogBehavior.Toast)
                        Logger.Error(exx, $"加载备用图片失败（{FallbackSource}）", LogBehavior.Toast)
                    End Try
                Else
                    Logger.Error(ex, $"下载图片失败（{Source}，备用：{FallbackSource}）", LogBehavior.Toast)
                    ActualSource = LoadingSource
                End If
            End Try
        End Sub, "MyImage PicLoader " & GetUuid() & "#", ThreadPriority.BelowNormal)
    End Sub

    Private Shared ReadOnly HttpClient As HttpClient = CreateHttpClient()

    Private Shared Function CreateHttpClient() As HttpClient
        Dim Handler As New HttpClientHandler With {.AutomaticDecompression = DecompressionMethods.All}
        Dim Client As New HttpClient(Handler) With {.Timeout = TimeSpan.FromSeconds(20)}
        Client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36")
        Client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8")
        Return Client
    End Function

    ''' <summary>
    ''' 下载图片到指定文件。可选地在主地址失败后使用备用地址。
    ''' </summary>
    Private Shared Sub Download(Url As String, FilePath As String, Optional FallbackUrl As String = Nothing)
        Dim Folder = Path.GetDirectoryName(FilePath)
        If Not String.IsNullOrEmpty(Folder) Then Directory.CreateDirectory(Folder)
        Dim Bytes As Byte()
        Try
            Bytes = GetImageBytes(Url)
        Catch ex As Exception
            If String.IsNullOrEmpty(FallbackUrl) OrElse FallbackUrl = Url Then Throw
            Logger.Warn(ex, $"图片下载失败，尝试备用地址（{Url}）")
            Bytes = GetImageBytes(FallbackUrl)
        End Try
        File.WriteAllBytes(FilePath, Bytes)
    End Sub

    Private Shared Function GetImageBytes(Url As String) As Byte()
        Dim Request As New HttpRequestMessage(HttpMethod.Get, Url)
        Dim UriResult As Uri = Nothing
        If Uri.TryCreate(Url, UriKind.Absolute, UriResult) Then
            Dim Referer As String = UriResult.GetLeftPart(UriPartial.Authority) & "/"
            If UriResult.Host.EndsWith("hdslb.com", StringComparison.OrdinalIgnoreCase) Then Referer = "https://www.bilibili.com/"
            Request.Headers.Referrer = New Uri(Referer)
        End If
        Using Response = HttpClient.SendAsync(Request).GetAwaiter().GetResult()
            Response.EnsureSuccessStatusCode()
            Dim MediaType = Response.Content.Headers.ContentType?.MediaType
            If MediaType IsNot Nothing AndAlso MediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) Then
                Throw New Exception($"服务器返回了非图片内容（{MediaType}）")
            End If
            Return Response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        End Using
    End Function

End Class
