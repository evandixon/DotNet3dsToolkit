Imports System.Threading

Friend NotInheritable Class Program
    Private Sub New()
    End Sub

    ''' <summary>
    ''' The main entry point for the application.
    ''' </summary>
    <STAThread>
    Public Shared Sub Main()
        ' Redirect standard output and error 


        ' Add the event handler for handling UI thread exceptions to the event.
        AddHandler Application.ThreadException, AddressOf UIThreadException

        ' Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)

        ' Add the event handler for handling non-UI thread exceptions to the event. 
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf CurrentDomain_UnhandledException

        ' Run the application
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New Form1)
    End Sub

    ' Handle the UI exceptions by showing a dialog box, and asking the user whether or not they wish to abort execution.
    Private Shared Sub UIThreadException(sender As Object, t As ThreadExceptionEventArgs)
        Dim result As DialogResult = DialogResult.Cancel
        Try
            ' Todo: make this translatable
            ErrorWindow.ShowErrorDialog("An unhandled exception has occurred." & vbLf & "You can continue running the program, but please report this error.", t.Exception, True)
        Catch
            Try
                ' Todo: make this translatable
                MessageBox.Show("A fatal error has occurred, and the details could not be displayed.  Please report this to the author.", "Error", MessageBoxButtons.OK, MessageBoxIcon.[Stop])
            Finally
                Application.[Exit]()
            End Try
        End Try

        ' Exits the program when the user clicks Abort.
        If result = DialogResult.Abort Then
            Application.[Exit]()
        End If
    End Sub

    ' Handle the UI exceptions by showing a dialog box, and asking the user whether
    ' or not they wish to abort execution.
    ' NOTE: This exception cannot be kept from terminating the application - it can only 
    ' log the event, and inform the user about it. 
    Private Shared Sub CurrentDomain_UnhandledException(sender As Object, e As UnhandledExceptionEventArgs)
        Try
            Dim ex = DirectCast(e.ExceptionObject, Exception)
            ' Todo: make this translatable
            ErrorWindow.ShowErrorDialog("An unhandled exception has occurred." & vbLf & "The program must now close.", ex, False)
        Catch
            Try
                ' Todo: make this translatable
                MessageBox.Show("A fatal non-UI error has occurred, and the details could not be displayed.  Please report this to the author.", "Error", MessageBoxButtons.OK, MessageBoxIcon.[Stop])
            Finally
                Application.[Exit]()
            End Try
        End Try
    End Sub
End Class