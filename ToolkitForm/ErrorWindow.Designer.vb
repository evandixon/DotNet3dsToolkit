<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ErrorWindow
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.B_Continue = New System.Windows.Forms.Button()
        Me.B_Abort = New System.Windows.Forms.Button()
        Me.B_CopyToClipboard = New System.Windows.Forms.Button()
        Me.L_ProvideInfo = New System.Windows.Forms.Label()
        Me.L_Message = New System.Windows.Forms.Label()
        Me.T_ExceptionDetails = New System.Windows.Forms.TextBox()
        Me.SuspendLayout()
        '
        'B_Continue
        '
        Me.B_Continue.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.B_Continue.Location = New System.Drawing.Point(332, 203)
        Me.B_Continue.Name = "B_Continue"
        Me.B_Continue.Size = New System.Drawing.Size(75, 23)
        Me.B_Continue.TabIndex = 11
        Me.B_Continue.Text = "Continue"
        Me.B_Continue.UseVisualStyleBackColor = True
        '
        'B_Abort
        '
        Me.B_Abort.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.B_Abort.Location = New System.Drawing.Point(413, 203)
        Me.B_Abort.Name = "B_Abort"
        Me.B_Abort.Size = New System.Drawing.Size(75, 23)
        Me.B_Abort.TabIndex = 10
        Me.B_Abort.Text = "Abort"
        Me.B_Abort.UseVisualStyleBackColor = True
        '
        'B_CopyToClipboard
        '
        Me.B_CopyToClipboard.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.B_CopyToClipboard.Location = New System.Drawing.Point(13, 203)
        Me.B_CopyToClipboard.Name = "B_CopyToClipboard"
        Me.B_CopyToClipboard.Size = New System.Drawing.Size(164, 23)
        Me.B_CopyToClipboard.TabIndex = 9
        Me.B_CopyToClipboard.Text = "Copy to Clipboard"
        Me.B_CopyToClipboard.UseVisualStyleBackColor = True
        '
        'L_ProvideInfo
        '
        Me.L_ProvideInfo.AutoSize = True
        Me.L_ProvideInfo.Location = New System.Drawing.Point(10, 38)
        Me.L_ProvideInfo.Name = "L_ProvideInfo"
        Me.L_ProvideInfo.Size = New System.Drawing.Size(269, 13)
        Me.L_ProvideInfo.TabIndex = 8
        Me.L_ProvideInfo.Text = "Please provide this information when reporting this error:"
        '
        'L_Message
        '
        Me.L_Message.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.L_Message.Location = New System.Drawing.Point(10, 11)
        Me.L_Message.Name = "L_Message"
        Me.L_Message.Size = New System.Drawing.Size(478, 27)
        Me.L_Message.TabIndex = 7
        Me.L_Message.Text = "An unknown error has occurred."
        '
        'T_ExceptionDetails
        '
        Me.T_ExceptionDetails.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.T_ExceptionDetails.Location = New System.Drawing.Point(13, 54)
        Me.T_ExceptionDetails.Multiline = True
        Me.T_ExceptionDetails.Name = "T_ExceptionDetails"
        Me.T_ExceptionDetails.ReadOnly = True
        Me.T_ExceptionDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.T_ExceptionDetails.Size = New System.Drawing.Size(475, 143)
        Me.T_ExceptionDetails.TabIndex = 6
        '
        'ErrorWindow
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(499, 236)
        Me.Controls.Add(Me.B_Continue)
        Me.Controls.Add(Me.B_Abort)
        Me.Controls.Add(Me.B_CopyToClipboard)
        Me.Controls.Add(Me.L_ProvideInfo)
        Me.Controls.Add(Me.L_Message)
        Me.Controls.Add(Me.T_ExceptionDetails)
        Me.Name = "ErrorWindow"
        Me.Text = "Error"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Private WithEvents B_Continue As Button
    Private WithEvents B_Abort As Button
    Private WithEvents B_CopyToClipboard As Button
    Private WithEvents L_ProvideInfo As Label
    Private WithEvents L_Message As Label
    Private WithEvents T_ExceptionDetails As TextBox
End Class
