Imports System.IO
Imports Avalonia.Media.Imaging
Imports Avalonia.Platform
Imports SkiaSharp

''' <summary>
''' 旋转与翻转方式，取值与 System.Drawing.RotateFlipType 一致。
''' </summary>
Public Enum RotateFlipType
    RotateNoneFlipNone = 0
    Rotate90FlipNone = 1
    Rotate180FlipNone = 2
    Rotate270FlipNone = 3
    RotateNoneFlipX = 4
    Rotate90FlipX = 5
    Rotate180FlipX = 6
    Rotate270FlipX = 7
    Rotate180FlipXY = 0
    Rotate270FlipXY = 1
    RotateNoneFlipXY = 2
    Rotate90FlipXY = 3
    RotateNoneFlipY = 6
    Rotate90FlipY = 7
    Rotate180FlipY = 4
    Rotate270FlipY = 5
End Enum

''' <summary>
''' 一个万能的自动图片类型转换工具类。
''' </summary>
Public Class MyBitmap
    Implements IImage

    ''' <summary>
    ''' 存储的图片。
    ''' </summary>
    Public Property Pic As SKBitmap

    Private Shared ReadOnly ResourceCache As New ConcurrentDictionary(Of String, SKBitmap)

#Region "转换"

    Public Shared Widening Operator CType(Image As MyBitmap) As Bitmap
        If Image Is Nothing Then Return Nothing
        Return Image.ToBitmap()
    End Operator

    Public Shared Widening Operator CType(Image As MyBitmap) As ImageBrush
        If Image Is Nothing Then Return Nothing
        Return New ImageBrush(Image.ToBitmap())
    End Operator

    Public Shared Widening Operator CType(Image As ImageBrush) As MyBitmap
        If Image Is Nothing Then Return Nothing
        If TypeOf Image.Source Is Bitmap Then Return New MyBitmap(CType(Image.Source, Bitmap))
        Return Nothing
    End Operator

    Private _CachedBitmap As Bitmap

    ''' <summary>
    ''' 转换为 Avalonia 的图片对象。结果会被缓存。
    ''' </summary>
    Public Function ToBitmap() As Bitmap
        If Pic Is Nothing Then Return Nothing
        If _CachedBitmap IsNot Nothing Then Return _CachedBitmap
        Using MS As New MemoryStream()
            Pic.Encode(MS, SKEncodedImageFormat.Png, 100)
            MS.Position = 0
            _CachedBitmap = New Bitmap(MS)
        End Using
        Return _CachedBitmap
    End Function

    Public ReadOnly Property Size As Avalonia.Size Implements IImage.Size
        Get
            Dim Source = ToBitmap()
            Return If(Source Is Nothing, New Avalonia.Size(0, 0), Source.Size)
        End Get
    End Property

    Public Sub Draw(Context As DrawingContext, DestRect As Avalonia.Rect, SourceRect As Avalonia.Rect) Implements IImage.Draw
        Dim Source As IImage = ToBitmap()
        If Source IsNot Nothing Then Source.Draw(Context, DestRect, SourceRect)
    End Sub

#End Region

#Region "构造"

    Public Sub New()
    End Sub

    Public Sub New(FilePathOrResourceName As String)
        If String.IsNullOrEmpty(FilePathOrResourceName) Then Throw New ArgumentException("图片路径为空", NameOf(FilePathOrResourceName))
        Try
            FilePathOrResourceName = FilePathOrResourceName.Replace("pack://application:,,,/images/", PathImage)
            If FilePathOrResourceName.StartsWithF(PathImage) Then
                ' 使用缓存
                Pic = ResourceCache.GetOrAdd(FilePathOrResourceName,
                    Function(Key As String)
                        Using Stream As Stream = AssetLoader.Open(New Uri(Key))
                            Return SKBitmap.Decode(Stream)
                        End Using
                    End Function)
            Else
                ' 一次性读入内存，避免长期占用文件
                Pic = SKBitmap.Decode(File.ReadAllBytes(FilePathOrResourceName))
            End If
            If Pic Is Nothing Then Throw New ArgumentException("图片格式不支持，或图片文件损坏")
        Catch ex As Exception
            Pic = Nothing
            If TypeOf ex Is ArgumentException Then
                Throw New Exception($"图片格式不支持，或图片文件损坏（{FilePathOrResourceName}）", ex)
            Else
                Throw New Exception($"加载 MyBitmap 意外失败（{FilePathOrResourceName}）", ex)
            End If
        End Try
    End Sub

    Public Sub New(Image As SKBitmap)
        Pic = Image
    End Sub

    Public Sub New(Image As Bitmap)
        Pic = DecodeAvaloniaBitmap(Image)
    End Sub

    Public Sub New(Image As IImage)
        If Image Is Nothing Then Throw New ArgumentNullException(NameOf(Image))
        If TypeOf Image Is Bitmap Then
            Pic = DecodeAvaloniaBitmap(Image)
        Else
            Throw New NotSupportedException($"不支持将 {Image.GetType().Name} 转换为 MyBitmap")
        End If
    End Sub

    Public Sub New(Image As ImageBrush)
        If Image Is Nothing Then Return
        Dim Source As IImage = TryCast(Image.Source, IImage)
        If Source Is Nothing Then Throw New ArgumentException("图片画刷没有有效的图片源", NameOf(Image))
        Pic = New MyBitmap(Source).Pic
    End Sub

    Private Shared Function DecodeAvaloniaBitmap(Image As Bitmap) As SKBitmap
        If Image Is Nothing Then Return Nothing
        Using MS As New MemoryStream()
            Image.Save(MS)
            MS.Position = 0
            Return SKBitmap.Decode(MS)
        End Using
    End Function

#End Region

#Region "处理"

    ''' <summary>
    ''' 获取裁切的图片，这个方法不会导致原对象改变且会返回一个新的对象。
    ''' </summary>
    Public Function Clip(X As Integer, Y As Integer, Width As Integer, Height As Integer) As MyBitmap
        Width = Math.Max(Width, 1)
        Height = Math.Max(Height, 1)
        X = Math.Clamp(X, 0, Math.Max(Pic.Width - 1, 0))
        Y = Math.Clamp(Y, 0, Math.Max(Pic.Height - 1, 0))
        Width = Math.Min(Width, Pic.Width - X)
        Height = Math.Min(Height, Pic.Height - Y)
        Dim Result As New SKBitmap(New SKImageInfo(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul))
        If Not Pic.ExtractSubset(Result, New SKRectI(X, Y, X + Width, Y + Height)) Then
            Using Canvas As New SKCanvas(Result)
                Canvas.Clear(SKColors.Transparent)
                Canvas.DrawBitmap(Pic, New SKRect(X, Y, X + Width, Y + Height), New SKRect(0, 0, Width, Height), Nothing)
            End Using
        End If
        Return New MyBitmap(Result)
    End Function

    ''' <summary>
    ''' 获取旋转或翻转后的图片，这个方法不会导致原对象改变且会返回一个新的对象。
    ''' </summary>
    Public Function RotateFlip(Type As RotateFlipType) As MyBitmap
        Dim Value = CInt(Type)
        Dim Angle = (Value And 3) * 90
        Dim FlipX = (Value And 4) <> 0
        Dim SourceWidth = Pic.Width
        Dim SourceHeight = Pic.Height
        Dim OutWidth = If(Angle = 90 OrElse Angle = 270, SourceHeight, SourceWidth)
        Dim OutHeight = If(Angle = 90 OrElse Angle = 270, SourceWidth, SourceHeight)
        Dim Result As New SKBitmap(New SKImageInfo(OutWidth, OutHeight, SKColorType.Bgra8888, SKAlphaType.Premul))
        Using Canvas As New SKCanvas(Result)
            Canvas.Clear(SKColors.Transparent)
            Canvas.Translate(OutWidth / 2.0F, OutHeight / 2.0F)
            Canvas.RotateDegrees(Angle)
            If FlipX Then Canvas.Scale(-1, 1)
            Canvas.Translate(-SourceWidth / 2.0F, -SourceHeight / 2.0F)
            Canvas.DrawBitmap(Pic, 0, 0, Nothing)
        End Using
        Return New MyBitmap(Result)
    End Function

    ''' <summary>
    ''' 将图像保存到文件。
    ''' </summary>
    Public Sub Save(FilePath As String)
        Dim Directory = Path.GetDirectoryName(FilePath)
        If Not String.IsNullOrEmpty(Directory) Then IO.Directory.CreateDirectory(Directory)
        Using Stream = File.Create(FilePath)
            Pic.Encode(Stream, SKEncodedImageFormat.Png, 100)
        End Using
    End Sub

#End Region

End Class
