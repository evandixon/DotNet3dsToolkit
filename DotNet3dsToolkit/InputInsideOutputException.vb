Imports System.IO

Public Class InputInsideOutputException
    Inherits IOException

    Public Sub New()
        MyBase.New(My.Resources.Language.ErrorInputFileCannotBeInOutput)
    End Sub

End Class
