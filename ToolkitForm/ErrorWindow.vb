
Imports System.Text

Partial Public Class ErrorWindow
    Inherits Form
    Public Shared Function ShowErrorDialog(friendlyMessage As String, ex As Exception, allowContinue As Boolean) As DialogResult
        Dim dialog As New ErrorWindow
        dialog.ShowContinue = allowContinue
        dialog.Message = friendlyMessage
        dialog.CurrentError = ex
        Dim result = dialog.ShowDialog()
        If result = DialogResult.Abort Then
            Environment.[Exit](1)
        End If
        Return result
    End Function

    Public Sub New()
        InitializeComponent()
    End Sub

    ''' <summary>
    ''' Gets or sets whether or not the "Continue" button is visible.
    ''' </summary>
    ''' <remarks>For UI exceptions, continuing could be safe.
    ''' For application exceptions, continuing is not possible, so the button should not be shown.</remarks>
    Public Property ShowContinue() As Boolean
        Get
            Return B_Continue.Visible
        End Get
        Set
            B_Continue.Visible = Value
        End Set
    End Property

    ''' <summary>
    ''' Friendly, context-specific method shown to the user.
    ''' </summary>
    ''' <remarks>This property is intended to be a user-friendly context-specific message about what went wrong.
    ''' For example: "An error occurred while attempting to automatically load the save file."</remarks>
    Public Property Message() As String
        Get
            Return L_Message.Text
        End Get
        Set
            L_Message.Text = Value
        End Set
    End Property

    Public Property CurrentError() As Exception
        Get
            Return _error
        End Get
        Set
            _error = Value
            UpdateExceptionDetailsMessage()
        End Set
    End Property
    Private _error As Exception

    Private Sub UpdateExceptionDetailsMessage()
        Dim details = New StringBuilder()
        details.AppendLine("Exception Details:")
        details.AppendLine(CurrentError.ToString())
        details.AppendLine()

        details.AppendLine("Loaded Assemblies:")
        details.AppendLine("--------------------")
        Try
            For Each item In AppDomain.CurrentDomain.GetAssemblies()
                details.AppendLine(item.FullName)
                details.AppendLine(item.Location)
                details.AppendLine()
            Next
        Catch ex As Exception
            details.AppendLine("An error occurred while listing the Loaded Assemblies:")
            details.AppendLine(ex.ToString())
        End Try
        details.AppendLine("--------------------")

        ' Include message in case it contains important information, like a file path.
        details.AppendLine("User Message:")
        details.AppendLine(Message)

        T_ExceptionDetails.Text = details.ToString()
    End Sub

    Private Sub btnCopyToClipboard_Click(sender As Object, e As EventArgs)
        Clipboard.SetText(T_ExceptionDetails.Text)
    End Sub

    Private Sub B_Continue_Click(sender As Object, e As EventArgs)
        DialogResult = DialogResult.OK
        Close()
    End Sub

    Private Sub B_Abort_Click(sender As Object, e As EventArgs)
        DialogResult = DialogResult.Abort
        Close()
    End Sub

End Class