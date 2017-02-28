Imports System.ComponentModel
Imports System.IO
Imports DotNet3dsToolkit
Imports SkyEditor.Core.Utilities

Public Class Form1

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Dim fileName As String = "LatestLog.txt"
        Try
            CurrentFile = File.Open(fileName, FileMode.Create, FileAccess.Write)
        Catch ex As IOException
            CurrentFile = Nothing
            lblStatus.Text = "Warning: Unable to open log."
        End Try

        If CurrentFile IsNot Nothing Then
            CurrentWriter = New StreamWriter(CurrentFile)
            CurrentWriter.AutoFlush = True
        End If
    End Sub

    Private Property CurrentFile As Stream
    Private Property CurrentWriter As StreamWriter

    Private Property IsOperating As Boolean = False

    Private Sub btnExtractSourceBrowse_Click(sender As Object, e As EventArgs) Handles btnExtractSourceBrowse.Click
        Dim s As New OpenFileDialog
        s.Filter = "Supported Files|*.3ds;*.cci;*.cxi;*.cia;*.nds;*.srl|Decrypted 3DS ROMs|*.3ds;*.cci|Decrypted CXI Partitions|*.cxi|CIA files|*.cia|Nintendo DS ROMs|*.nds;*.srl|All Files|*.*"
        If s.ShowDialog = DialogResult.OK Then
            txtExtractSource.Text = s.FileName
        End If
    End Sub

    Private Sub btnExtractDestinationBrowse_Click(sender As Object, e As EventArgs) Handles btnExtractDestinationBrowse.Click
        Dim f As New FolderBrowserDialog
        If f.ShowDialog = DialogResult.OK Then
            txtExtractDestination.Text = f.SelectedPath
        End If
    End Sub

    Private Sub btnBuildSourceBrowse_Click(sender As Object, e As EventArgs) Handles btnBuildSourceBrowse.Click
        Dim f As New FolderBrowserDialog
        If f.ShowDialog = DialogResult.OK Then
            txtBuildSource.Text = f.SelectedPath
        End If
    End Sub

    Private Sub btnBuildOutputBrowse_Click(sender As Object, e As EventArgs) Handles btnBuildOutputBrowse.Click
        Dim s As New SaveFileDialog
        s.Filter = "Decrypted 3DS ROMs|*.3ds;*.cci|CIA files|*.cia|0-Key Encryted 3DS ROMs|*.3dz;*.3ds|Nintendo DS ROMs|*.nds;*.srl|All Files|*.*"
        If s.ShowDialog = DialogResult.OK Then
            txtBuildDestination.Text = s.FileName
        End If
    End Sub

    Private Sub btnHansSourceBrowse_Click(sender As Object, e As EventArgs) Handles btnHansSourceBrowse.Click
        Dim f As New FolderBrowserDialog
        If f.ShowDialog = DialogResult.OK Then
            txtHansSource.Text = f.SelectedPath
        End If
    End Sub

    Private Sub btnHansSDBrowse_Click(sender As Object, e As EventArgs) Handles btnHansSDBrowse.Click
        Dim f As New FolderBrowserDialog
        If f.ShowDialog = DialogResult.OK Then
            txtHansSD.Text = f.SelectedPath
        End If
    End Sub

    Private Sub OnConsoleOutputReceived(sender As Object, e As DataReceivedEventArgs)
        If CurrentWriter IsNot Nothing Then
            CurrentWriter.Write(DateTime.Now.ToString)
            CurrentWriter.Write(": ")
            CurrentWriter.WriteLine(e.Data)
        End If
    End Sub

    Private Sub OnUnpackProgressedInternal(sender As Object, e As ProgressReportedEventArgs)
        If e.IsIndeterminate OrElse e.Progress = Single.NaN Then
            pbProgress.Style = ProgressBarStyle.Marquee
        Else
            pbProgress.Style = ProgressBarStyle.Continuous
        End If

        If Not SIngle.IsNaN(e.Progress) Then
            pbProgress.Value = e.Progress * pbProgress.Maximum
        End If

        lblStatus.Text = String.Format("Extracting...")
    End Sub

    Private Sub OnUnpackProgressed(sender As Object, e As ProgressReportedEventArgs)
        If InvokeRequired Then
            Invoke(Sub() OnUnpackProgressedInternal(sender, e))
        Else
            OnUnpackProgressedInternal(sender, e)
        End If
    End Sub

    Private Sub ShowOperatingWarning()
        MessageBox.Show("Please wait until the current operation is complete before starting another.")
    End Sub

    Private Async Sub btnExtract_Click(sender As Object, e As EventArgs) Handles btnExtract.Click
        If IsOperating Then
            ShowOperatingWarning()
            Exit Sub
        End If

        If String.IsNullOrEmpty(txtExtractSource.Text) Then
            MessageBox.Show("Please select a source ROM.")
            Exit Sub
        End If

        If String.IsNullOrEmpty(txtExtractDestination.Text) Then
            MessageBox.Show("Please choose an output directory.")
            Exit Sub
        End If

        Using c As New Converter
            IsOperating = True
            pbProgress.Value = 0
            pbProgress.Style = ProgressBarStyle.Marquee

            AddHandler c.ConsoleOutputReceived, AddressOf OnConsoleOutputReceived
            AddHandler c.UnpackProgressed, AddressOf OnUnpackProgressed

            If rbExtractAuto.Checked Then
                lblStatus.Text = "Extracting (type auto-detected)..."
                Await Task.Run(Async Function() As Task 'Start as a new task to allow running on a new thread right now
                                   Await c.ExtractAuto(txtExtractSource.Text, txtExtractDestination.Text)
                               End Function)
            ElseIf rbExtractCCIDec.Checked Then
                lblStatus.Text = "Extracting as decrypted CCI..."
                Await Task.Run(Async Function() As Task
                                   Await c.ExtractCCI(txtExtractSource.Text, txtExtractDestination.Text)
                               End Function)
            ElseIf rbExtractCXIDec.Checked Then
                lblStatus.Text = "Extracting as decrypted CXI..."
                Await Task.Run(Async Function() As Task
                                   Await c.ExtractCXI(txtExtractSource.Text, txtExtractDestination.Text)
                               End Function)
            ElseIf rbExtractCIA.Checked Then
                lblStatus.Text = "Extracting as decrypted CIA..."
                Await Task.Run(Async Function() As Task
                                   Await c.ExtractCIA(txtExtractSource.Text, txtExtractDestination.Text)
                               End Function)
            ElseIf rbExtractNDS.Checked Then
                lblStatus.Text = "Extracting as NDS ROM..."
                Await Task.Run(Async Function() As Task
                                   Await c.ExtractNDS(txtExtractSource.Text, txtExtractDestination.Text)
                               End Function)
            Else
                MessageBox.Show("Invalid radio button choice.")
            End If

            RemoveHandler c.ConsoleOutputReceived, AddressOf OnConsoleOutputReceived
            RemoveHandler c.UnpackProgressed, AddressOf OnUnpackProgressed

            pbProgress.Value = pbProgress.Maximum
            pbProgress.Style = ProgressBarStyle.Continuous
            lblStatus.Text = "Ready"
            IsOperating = False
        End Using
    End Sub

    Private Async Sub btnBuild_Click(sender As Object, e As EventArgs) Handles btnBuild.Click
        If IsOperating Then
            ShowOperatingWarning()
            Exit Sub
        End If

        If String.IsNullOrEmpty(txtBuildSource.Text) Then
            MessageBox.Show("Please select a source directory.")
            Exit Sub
        End If

        If String.IsNullOrEmpty(txtBuildDestination.Text) Then
            MessageBox.Show("Please choose an output file path.")
            Exit Sub
        End If

        Using c As New Converter
            IsOperating = True
            pbProgress.Value = 0
            pbProgress.Style = ProgressBarStyle.Marquee

            AddHandler c.ConsoleOutputReceived, AddressOf OnConsoleOutputReceived

            If rbBuildAuto.Checked Then
                lblStatus.Text = "Building (auto-detect format)..."
                Await Task.Run(Async Function() As Task 'Start as a new task to allow running on a new thread right now
                                   Await c.BuildAuto(txtBuildSource.Text, txtBuildDestination.Text)
                               End Function)
            ElseIf rbBuildCCIDec.Checked Then
                lblStatus.Text = "Building as decrypted CCI..."
                Await Task.Run(Async Function() As Task
                                   Await c.Build3DSDecrypted(txtBuildSource.Text, txtBuildDestination.Text)
                               End Function)
            ElseIf rbBuildCCI0Key.Checked Then
                lblStatus.Text = "Building as 0-key encrypted CCI..."
                Await Task.Run(Async Function() As Task
                                   Await c.Build3DS0Key(txtBuildSource.Text, txtBuildDestination.Text)
                               End Function)
            ElseIf rbBuildCIA.Checked Then
                lblStatus.Text = "Building as CIA..."
                Await Task.Run(Async Function() As Task
                                   Await c.BuildCia(txtBuildSource.Text, txtBuildDestination.Text)
                               End Function)
            ElseIf rbBuildNDS.Checked Then
                lblStatus.Text = "Building as NDS..."
                Await Task.Run(Async Function() As Task
                                   Await c.BuildNDS(txtBuildSource.Text, txtBuildDestination.Text)
                               End Function)
            Else
                MessageBox.Show("Invalid radio button choice.")
            End If

            RemoveHandler c.ConsoleOutputReceived, AddressOf OnConsoleOutputReceived

            pbProgress.Value = pbProgress.Maximum
            pbProgress.Style = ProgressBarStyle.Continuous
            lblStatus.Text = "Ready"
            IsOperating = False
        End Using
    End Sub

    Private Async Sub btnHan_Click(sender As Object, e As EventArgs) Handles btnHan.Click
        If IsOperating Then
            ShowOperatingWarning()
            Exit Sub
        End If

        If String.IsNullOrEmpty(txtHansSource.Text) Then
            MessageBox.Show("Please select a source directory.")
            Exit Sub
        End If

        If String.IsNullOrEmpty(txtHansSD.Text) Then
            MessageBox.Show("Please choose your 3DS's SD.  If you do not with to output to your SD, you can choose any other directory and copy its contents to the root of your SD later.")
            Exit Sub
        End If

        If String.IsNullOrEmpty(txtHansShortName.Text) Then
            MessageBox.Show("Please enter a short name for your files and shortcut.")
            Exit Sub
        End If

        Using c As New Converter
            IsOperating = True
            pbProgress.Value = 0
            pbProgress.Style = ProgressBarStyle.Marquee

            AddHandler c.ConsoleOutputReceived, AddressOf OnConsoleOutputReceived

            lblStatus.Text = "Building for HANS..."
            Await Task.Run(Async Function() As Task 'Start as a new task to allow running on a new thread right now
                               Await c.BuildHans(txtHansSource.Text, txtHansSD.Text, txtHansShortName.Text)
                           End Function)


            RemoveHandler c.ConsoleOutputReceived, AddressOf OnConsoleOutputReceived

            pbProgress.Value = 1
            pbProgress.Style = ProgressBarStyle.Continuous
            lblStatus.Text = "Ready"
            IsOperating = False
        End Using
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If IsOperating Then
            If Not MessageBox.Show("Are you sure you want to exit before your operation is complete?", Me.Text, MessageBoxButtons.YesNo) = DialogResult.Yes Then
                e.Cancel = True
            End If
        End If

        If Not e.Cancel Then
            CurrentWriter?.Dispose()
            CurrentFile?.Dispose()
        End If
    End Sub
End Class
