Public Class MissingFileException
    Inherits IO.IOException

    Public Sub New(path As String)
        MyBase.New(String.Format(My.Resources.Language.ErrorMissingFile, path))
        Me.Path = path
    End Sub

    Public Property Path As String
End Class
