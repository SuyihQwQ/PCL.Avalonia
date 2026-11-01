Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Avalonia
Imports Avalonia.Platform
Imports Newtonsoft.Json

Public Module Localization

    Private ReadOnly HanPattern As New Regex("[\u4e00-\u9fff]", RegexOptions.Compiled)
    Private ReadOnly AmbiguousValues As New HashSet(Of String)(StringComparer.Ordinal)
    Private ReadOnly ChineseValues As New Dictionary(Of String, String)(StringComparer.Ordinal)
    Private ReadOnly EnglishValues As New Dictionary(Of String, String)(StringComparer.Ordinal)
    Private ReadOnly CustomTranslations As New Dictionary(Of String, String)(StringComparer.Ordinal)
    Private ReadOnly CustomLanguageKeys As New HashSet(Of String)(StringComparer.Ordinal)
    Private ReadOnly ReverseTranslations As New Dictionary(Of String, String)(StringComparer.Ordinal)
    Private _CurrentLanguage As String = "zh-CN"
    Private _IsInitialized As Boolean

    Public ReadOnly Property CurrentLanguage As String
        Get
            Return _CurrentLanguage
        End Get
    End Property

    Public Sub Initialize(Language As String)
        If Not _IsInitialized Then
            ChineseValues.Clear()
            EnglishValues.Clear()
            LoadLanguage("zh-CN", ChineseValues)
            LoadLanguage("en-US", EnglishValues)
            LoadCustomTranslations()
            Dim Index As Integer = 0
            For Each Entry In CustomTranslations.OrderBy(Function(Item) Item.Key, StringComparer.Ordinal)
                Dim Key As String = $"LangAvalonia{Index:D4}"
                ChineseValues.Add(Key, Entry.Key)
                EnglishValues.Add(Key, Entry.Value)
                CustomLanguageKeys.Add(Key)
                Index += 1
            Next
            BuildReverseTranslations()
            _IsInitialized = True
        End If
        SetLanguage(Language)
    End Sub

    Public Sub SetLanguage(Language As String)
        If Not _IsInitialized Then
            Initialize(Language)
            Return
        End If

        Dim NormalizedLanguage As String
        If String.Equals(Language, "en-US", StringComparison.OrdinalIgnoreCase) Then
            NormalizedLanguage = "en-US"
        Else
            If Not String.IsNullOrWhiteSpace(Language) AndAlso
               Not String.Equals(Language, "zh-CN", StringComparison.OrdinalIgnoreCase) Then
                Logger.Warn($"不支持界面语言 {Language}，将使用简体中文")
            End If
            NormalizedLanguage = "zh-CN"
        End If

        _CurrentLanguage = NormalizedLanguage
        Dim Values = If(NormalizedLanguage = "en-US", EnglishValues, ChineseValues)
        For Each Entry In ChineseValues
            Dim Value As String = Nothing
            If Values.TryGetValue(Entry.Key, Value) AndAlso
               (NormalizedLanguage <> "en-US" OrElse CustomLanguageKeys.Contains(Entry.Key) OrElse Not HanPattern.IsMatch(Value)) Then
                Application.Current.Resources(Entry.Key) = Value
            Else
                Application.Current.Resources(Entry.Key) = Entry.Value
            End If
        Next
        PageInstanceExport.RefreshLocalization()
    End Sub

    Public Function GetText(Key As String, ParamArray Arguments As Object()) As String
        Dim Value As String = Nothing
        If _CurrentLanguage = "en-US" Then
            EnglishValues.TryGetValue(Key, Value)
            If String.IsNullOrEmpty(Value) OrElse (Not CustomLanguageKeys.Contains(Key) AndAlso HanPattern.IsMatch(Value)) Then Value = Nothing
        End If
        If Value Is Nothing Then ChineseValues.TryGetValue(Key, Value)
        If Value Is Nothing Then
            Logger.Warn($"缺少界面语言资源：{Key}")
            Return Key
        End If
        If Arguments Is Nothing OrElse Arguments.Length = 0 Then Return Value
        Return String.Format(CultureInfo.CurrentCulture, Value, Arguments)
    End Function

    Public Function TranslateText(Text As String) As String
        If Text Is Nothing OrElse _CurrentLanguage <> "en-US" Then Return Text
        Dim Translation As String = Nothing
        If ReverseTranslations.TryGetValue(Text, Translation) Then Return Translation
        Return Text
    End Function

    Private Sub LoadLanguage(Language As String, Values As Dictionary(Of String, String))
        Dim ResourceUri As New Uri($"avares://PCL/Assets/Language/{Language}.json")
        Using Reader As New IO.StreamReader(AssetLoader.Open(ResourceUri))
            Dim Loaded = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.ReadToEnd())
            If Loaded Is Nothing Then Throw New InvalidDataException($"语言资源 {Language} 为空")
            For Each Entry In Loaded
                Values.Add(Entry.Key, Entry.Value)
            Next
        End Using
    End Sub

    Private Sub BuildReverseTranslations()
        Dim Candidates As New Dictionary(Of String, String)(StringComparer.Ordinal)
        For Each Entry In ChineseValues
            Dim Translation As String = Nothing
            If String.IsNullOrWhiteSpace(Entry.Value) OrElse
               Not EnglishValues.TryGetValue(Entry.Key, Translation) OrElse
               String.IsNullOrWhiteSpace(Translation) OrElse
               HanPattern.IsMatch(Translation) Then Continue For

            Dim Existing As String = Nothing
            If Candidates.TryGetValue(Entry.Value, Existing) Then
                If Not String.Equals(Existing, Translation, StringComparison.Ordinal) Then AmbiguousValues.Add(Entry.Value)
            Else
                Candidates.Add(Entry.Value, Translation)
            End If
        Next

        For Each Candidate In Candidates
            If Not AmbiguousValues.Contains(Candidate.Key) Then ReverseTranslations.Add(Candidate.Key, Candidate.Value)
        Next
        For Each Entry In CustomTranslations
            ReverseTranslations(Entry.Key) = Entry.Value
        Next
    End Sub

    Private Sub LoadCustomTranslations()
        Dim ResourceUri As New Uri("avares://PCL/Assets/Language/overrides.json")
        Using Reader As New IO.StreamReader(AssetLoader.Open(ResourceUri))
            Dim Loaded = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(Reader.ReadToEnd())
            If Loaded Is Nothing Then Throw New InvalidDataException("自定义英文词条为空")
            For Each Entry In Loaded
                CustomTranslations.Add(Entry.Key.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf),
                                       Entry.Value.Replace(vbCrLf, vbLf).Replace(vbCr, vbLf))
            Next
        End Using
    End Sub

End Module
