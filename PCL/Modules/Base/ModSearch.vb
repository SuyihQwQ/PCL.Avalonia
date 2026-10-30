''' <summary>
''' 多段文本加权搜索。
''' </summary>
Public Module ModSearch

    ''' <summary>
    ''' 获取搜索文本的相似度。
    ''' </summary>
    ''' <param name="Source">被搜索的长内容。</param>
    ''' <param name="Query">用户输入的搜索文本。</param>
    Private Function SearchSimilarity(Source As String, Query As String) As Double
        If String.IsNullOrEmpty(Source) OrElse String.IsNullOrEmpty(Query) Then Return 0
        Dim qp As Integer = 0, lenSum As Double = 0
        Dim Str As New StringBuilder(Source.Length)
        Str.Append(Source.ToLowerInvariant().Replace(" ", ""))
        Query = Query.ToLowerInvariant().Replace(" ", "")
        Dim SourceLength As Integer = Str.Length, QueryLength As Integer = Query.Length
        If QueryLength = 0 Then Return 0
        Do While qp < QueryLength
            Dim sp As Integer = 0, lenMax As Integer = 0, spMax As Integer = 0
            Dim CurrentSourceLength As Integer = Str.Length
            Do While sp < CurrentSourceLength
                Dim len As Integer = 0
                While (qp + len) < QueryLength AndAlso (sp + len) < CurrentSourceLength AndAlso Str(sp + len) = Query(qp + len)
                    len += 1
                End While
                If len > lenMax Then
                    lenMax = len
                    spMax = sp
                End If
                sp += If(len > 0, len, 1)
            Loop
            If lenMax > 0 Then
                Str.Remove(spMax, lenMax)
                Dim IncWeight = (Math.Pow(1.4, 3 + lenMax) - 3.6)
                IncWeight *= 1 + 0.3 * Math.Max(0, 3 - Math.Abs(qp - spMax))
                lenSum += IncWeight
            End If
            qp += If(lenMax > 0, lenMax, 1)
        Loop
        Return (lenSum / QueryLength) * (3 / Math.Sqrt(SourceLength + 15)) * If(QueryLength <= 2, 3 - QueryLength, 1)
    End Function

    ''' <summary>
    ''' 获取多段文本加权后的相似度。
    ''' </summary>
    Private Function SearchSimilarityWeighted(Source As List(Of SearchSource), Query As String) As Double
        Dim TotalWeight As Double = 0
        Dim Sum As Double = 0
        For Each Pair In Source
            If Pair.Aliases IsNot Nothing AndAlso Pair.Aliases.Any Then
                Sum += Pair.Aliases.Max(Function(a) SearchSimilarity(a, Query)) * Pair.Weight
            End If
            TotalWeight += Pair.Weight
        Next
        Return Sum / TotalWeight
    End Function

    ''' <summary>
    ''' 用于搜索的项目。
    ''' </summary>
    Public Class SearchEntry(Of T)
        ''' <summary>
        ''' 该项目对应的源数据。
        ''' </summary>
        Public Item As T
        ''' <summary>
        ''' 该项目用于搜索的文本源。
        ''' </summary>
        Public SearchSource As List(Of SearchSource)
        ''' <summary>
        ''' 相似度。
        ''' </summary>
        Public Similarity As Double
        ''' <summary>
        ''' 是否完全匹配。
        ''' </summary>
        Public AbsoluteRight As Boolean
        Public Overrides Function ToString() As String
            Return Math.Round(Similarity, 3) & " - " & Item.ToString()
        End Function
    End Class

    ''' <summary>
    ''' 单个用于搜索的文本源。
    ''' </summary>
    Public Class SearchSource
        Public Aliases As String()
        Public Weight As Double
        Public Sub New(Aliases As String(), Optional Weight As Double = 1)
            Me.Aliases = Aliases
            Me.Weight = Weight
        End Sub
        Public Sub New(Text As String, Optional Weight As Double = 1)
            Me.Aliases = {Text}
            Me.Weight = Weight
        End Sub
    End Class

    ''' <summary>
    ''' 进行多段文本加权搜索，获取相似度较高的数项结果。
    ''' 这会修改 Entries 中每项的 Similarity 与 AbsoluteRight 字段。
    ''' </summary>
    ''' <param name="MaxBlurCount">返回的最大模糊结果数。</param>
    ''' <param name="MinBlurSimilarity">返回结果要求的最低相似度。</param>
    Public Function Search(Of T)(Entries As List(Of SearchEntry(Of T)), Query As String, Optional MaxBlurCount As Integer = 5, Optional MinBlurSimilarity As Double = 0.1) As List(Of SearchEntry(Of T))
        Dim ResultList As New List(Of SearchEntry(Of T))
        If Not Entries.Any() Then Return ResultList
        Dim QueryParts = Query.Split(" ", True)
        Dim Candidates As New List(Of SearchEntry(Of T))
        For Each Entry In Entries
            Entry.Similarity = SearchSimilarityWeighted(Entry.SearchSource, Query)
            Entry.AbsoluteRight =
                QueryParts.All(
                Function(QueryPart) Entry.SearchSource.Any(
                Function(Source) Source.Aliases.Any(
                Function([Alias]) [Alias].Replace(" ", "").ContainsIgnoreCase(QueryPart))))
            If Entry.AbsoluteRight OrElse Entry.Similarity >= MinBlurSimilarity Then Candidates.Add(Entry)
        Next
        Candidates = Candidates.SortByComparison(Function(Left, Right) If(Left.AbsoluteRight <> Right.AbsoluteRight, Left.AbsoluteRight, Left.Similarity > Right.Similarity))
        Dim BlurCount As Integer = 0
        For Each Entry In Candidates
            If Entry.AbsoluteRight Then
                ResultList.Add(Entry)
            Else
                If BlurCount = MaxBlurCount Then Exit For
                ResultList.Add(Entry)
                BlurCount += 1
            End If
        Next
        Return ResultList
    End Function

End Module
