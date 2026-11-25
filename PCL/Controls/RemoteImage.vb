' SPDX-License-Identifier: Apache-2.0
Imports System.Collections.Concurrent
Imports System.Net.Http
Imports Avalonia.Markup.Xaml
Imports Avalonia.Media
Imports Avalonia.Media.Imaging
Imports Avalonia.Platform

''' <summary>
''' 兼容旧版主页里的网络图片画刷：Source="{local:RemoteImage Url='https://...'}"。
''' Avalonia 的 ImageBrush 无法直接解析网络地址，这里在后台下载完成后回填 Source。
''' </summary>
Public Class RemoteImage
    Inherits MarkupExtension

    ''' <summary>
    ''' 图片地址，支持 http(s) 与本地文件路径。
    ''' </summary>
    Public Property Url As String

    '主页图片同样跟随网络代理设置
    Private Shared ReadOnly Property Client As HttpClient
        Get
            Return ModNet.GetSharedHttpClient("image-brush", ModNet.NetRequestCategory.Other, 20)
        End Get
    End Property
    Private Shared ReadOnly Cache As New ConcurrentDictionary(Of String, IImageBrushSource)()

    Public Overrides Function ProvideValue(ServiceProvider As IServiceProvider) As Object
        Dim Target As ImageBrush = Nothing
        If ServiceProvider IsNot Nothing Then
            Dim Provider = TryCast(ServiceProvider.GetService(GetType(IProvideValueTarget)), IProvideValueTarget)
            If Provider IsNot Nothing Then Target = TryCast(Provider.TargetObject, ImageBrush)
        End If
        If Target IsNot Nothing Then LoadImage(Url, Target)
        Return Nothing
    End Function

    Private Shared Sub LoadImage(Address As String, Target As ImageBrush)
        If String.IsNullOrWhiteSpace(Address) Then Return
        Dim Cached As IImageBrushSource = Nothing
        If Cache.TryGetValue(Address, Cached) Then
            Target.Source = Cached
            Return
        End If
        RunInNewThread(
            Sub()
                Try
                    'ImageBrush.Source 需要 IImageBrushSource，Bitmap 同时实现了 IImage 与 IImageBrushSource
                    Dim Pic As IImageBrushSource = Nothing
                    If Address.StartsWithF("http") Then
                        Dim Bytes As Byte() = Client.GetByteArrayAsync(Address).GetAwaiter().GetResult()
                        Pic = New Bitmap(New IO.MemoryStream(Bytes))
                    ElseIf Address.StartsWithF("avares://") Then
                        Using Stream As IO.Stream = AssetLoader.Open(New Uri(Address))
                            Pic = New Bitmap(Stream)
                        End Using
                    Else
                        Pic = New Bitmap(Address)
                    End If
                    Cache(Address) = Pic
                    RunInUi(Sub() Target.Source = Pic)
                Catch ex As Exception
                    Logger.Warn(ex, $"加载主页图片失败：{Address}")
                End Try
            End Sub, "RemoteImage Loader", ThreadPriority.BelowNormal)
    End Sub
End Class
