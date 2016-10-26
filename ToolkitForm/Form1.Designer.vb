<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
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
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.btnExtract = New System.Windows.Forms.Button()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.rbExtractCIA = New System.Windows.Forms.RadioButton()
        Me.rbExtractCXIDec = New System.Windows.Forms.RadioButton()
        Me.rbExtractCCIDec = New System.Windows.Forms.RadioButton()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.rbExtractAuto = New System.Windows.Forms.RadioButton()
        Me.btnExtractDestinationBrowse = New System.Windows.Forms.Button()
        Me.txtExtractDestination = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.btnExtractSourceBrowse = New System.Windows.Forms.Button()
        Me.txtExtractSource = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TabPage2 = New System.Windows.Forms.TabPage()
        Me.btnBuild = New System.Windows.Forms.Button()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.rbBuildCCI0Key = New System.Windows.Forms.RadioButton()
        Me.rbBuildCIA = New System.Windows.Forms.RadioButton()
        Me.rbBuildCCIDec = New System.Windows.Forms.RadioButton()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.rbBuildAuto = New System.Windows.Forms.RadioButton()
        Me.btnBuildOutputBrowse = New System.Windows.Forms.Button()
        Me.txtBuildDestination = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.btnBuildSourceBrowse = New System.Windows.Forms.Button()
        Me.txtBuildSource = New System.Windows.Forms.TextBox()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.TabPage3 = New System.Windows.Forms.TabPage()
        Me.btnHan = New System.Windows.Forms.Button()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.txtHansShortName = New System.Windows.Forms.TextBox()
        Me.btnHansSDBrowse = New System.Windows.Forms.Button()
        Me.txtHansSD = New System.Windows.Forms.TextBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.btnHansSourceBrowse = New System.Windows.Forms.Button()
        Me.txtHansSource = New System.Windows.Forms.TextBox()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.StatusStrip1 = New System.Windows.Forms.StatusStrip()
        Me.pbProgress = New System.Windows.Forms.ToolStripProgressBar()
        Me.lblStatus = New System.Windows.Forms.ToolStripStatusLabel()
        Me.rbExtractNDS = New System.Windows.Forms.RadioButton()
        Me.rbBuildNDS = New System.Windows.Forms.RadioButton()
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.TabPage2.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.TabPage3.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.StatusStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TabControl1
        '
        Me.TabControl1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Controls.Add(Me.TabPage2)
        Me.TabControl1.Controls.Add(Me.TabPage3)
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(473, 262)
        Me.TabControl1.TabIndex = 0
        '
        'TabPage1
        '
        Me.TabPage1.Controls.Add(Me.btnExtract)
        Me.TabPage1.Controls.Add(Me.GroupBox1)
        Me.TabPage1.Controls.Add(Me.btnExtractDestinationBrowse)
        Me.TabPage1.Controls.Add(Me.txtExtractDestination)
        Me.TabPage1.Controls.Add(Me.Label2)
        Me.TabPage1.Controls.Add(Me.btnExtractSourceBrowse)
        Me.TabPage1.Controls.Add(Me.txtExtractSource)
        Me.TabPage1.Controls.Add(Me.Label1)
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage1.Size = New System.Drawing.Size(465, 236)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "Extract"
        Me.TabPage1.UseVisualStyleBackColor = True
        '
        'btnExtract
        '
        Me.btnExtract.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnExtract.Location = New System.Drawing.Point(6, 205)
        Me.btnExtract.Name = "btnExtract"
        Me.btnExtract.Size = New System.Drawing.Size(75, 23)
        Me.btnExtract.TabIndex = 7
        Me.btnExtract.Text = "Extract"
        Me.btnExtract.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox1.Controls.Add(Me.rbExtractNDS)
        Me.GroupBox1.Controls.Add(Me.rbExtractCIA)
        Me.GroupBox1.Controls.Add(Me.rbExtractCXIDec)
        Me.GroupBox1.Controls.Add(Me.rbExtractCCIDec)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.rbExtractAuto)
        Me.GroupBox1.Location = New System.Drawing.Point(11, 60)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(446, 139)
        Me.GroupBox1.TabIndex = 6
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Options"
        '
        'rbExtractCIA
        '
        Me.rbExtractCIA.AutoSize = True
        Me.rbExtractCIA.Location = New System.Drawing.Point(119, 63)
        Me.rbExtractCIA.Name = "rbExtractCIA"
        Me.rbExtractCIA.Size = New System.Drawing.Size(94, 17)
        Me.rbExtractCIA.TabIndex = 3
        Me.rbExtractCIA.Text = "Decrypted CIA"
        Me.rbExtractCIA.UseVisualStyleBackColor = True
        '
        'rbExtractCXIDec
        '
        Me.rbExtractCXIDec.AutoSize = True
        Me.rbExtractCXIDec.Location = New System.Drawing.Point(119, 86)
        Me.rbExtractCXIDec.Name = "rbExtractCXIDec"
        Me.rbExtractCXIDec.Size = New System.Drawing.Size(248, 17)
        Me.rbExtractCXIDec.TabIndex = 4
        Me.rbExtractCXIDec.Text = "Decrypted CXI (aka what Braindump gives you)"
        Me.rbExtractCXIDec.UseVisualStyleBackColor = True
        '
        'rbExtractCCIDec
        '
        Me.rbExtractCCIDec.AutoSize = True
        Me.rbExtractCCIDec.Location = New System.Drawing.Point(119, 40)
        Me.rbExtractCCIDec.Name = "rbExtractCCIDec"
        Me.rbExtractCCIDec.Size = New System.Drawing.Size(178, 17)
        Me.rbExtractCCIDec.TabIndex = 2
        Me.rbExtractCCIDec.Text = "Decrypted CCI (aka 3DS ROMs)"
        Me.rbExtractCCIDec.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(6, 19)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(107, 13)
        Me.Label3.TabIndex = 0
        Me.Label3.Text = "Source ROM Format:"
        '
        'rbExtractAuto
        '
        Me.rbExtractAuto.AutoSize = True
        Me.rbExtractAuto.Checked = True
        Me.rbExtractAuto.Location = New System.Drawing.Point(119, 17)
        Me.rbExtractAuto.Name = "rbExtractAuto"
        Me.rbExtractAuto.Size = New System.Drawing.Size(47, 17)
        Me.rbExtractAuto.TabIndex = 1
        Me.rbExtractAuto.TabStop = True
        Me.rbExtractAuto.Text = "Auto"
        Me.rbExtractAuto.UseVisualStyleBackColor = True
        '
        'btnExtractDestinationBrowse
        '
        Me.btnExtractDestinationBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnExtractDestinationBrowse.Location = New System.Drawing.Point(382, 32)
        Me.btnExtractDestinationBrowse.Name = "btnExtractDestinationBrowse"
        Me.btnExtractDestinationBrowse.Size = New System.Drawing.Size(75, 23)
        Me.btnExtractDestinationBrowse.TabIndex = 5
        Me.btnExtractDestinationBrowse.Text = "Browse..."
        Me.btnExtractDestinationBrowse.UseVisualStyleBackColor = True
        '
        'txtExtractDestination
        '
        Me.txtExtractDestination.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtExtractDestination.Location = New System.Drawing.Point(101, 34)
        Me.txtExtractDestination.Name = "txtExtractDestination"
        Me.txtExtractDestination.Size = New System.Drawing.Size(275, 20)
        Me.txtExtractDestination.TabIndex = 4
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(8, 37)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(87, 13)
        Me.Label2.TabIndex = 3
        Me.Label2.Text = "Output Directory:"
        '
        'btnExtractSourceBrowse
        '
        Me.btnExtractSourceBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnExtractSourceBrowse.Location = New System.Drawing.Point(382, 6)
        Me.btnExtractSourceBrowse.Name = "btnExtractSourceBrowse"
        Me.btnExtractSourceBrowse.Size = New System.Drawing.Size(75, 23)
        Me.btnExtractSourceBrowse.TabIndex = 2
        Me.btnExtractSourceBrowse.Text = "Browse..."
        Me.btnExtractSourceBrowse.UseVisualStyleBackColor = True
        '
        'txtExtractSource
        '
        Me.txtExtractSource.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtExtractSource.Location = New System.Drawing.Point(101, 8)
        Me.txtExtractSource.Name = "txtExtractSource"
        Me.txtExtractSource.Size = New System.Drawing.Size(275, 20)
        Me.txtExtractSource.TabIndex = 1
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(8, 11)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(72, 13)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Source ROM:"
        '
        'TabPage2
        '
        Me.TabPage2.Controls.Add(Me.btnBuild)
        Me.TabPage2.Controls.Add(Me.GroupBox2)
        Me.TabPage2.Controls.Add(Me.btnBuildOutputBrowse)
        Me.TabPage2.Controls.Add(Me.txtBuildDestination)
        Me.TabPage2.Controls.Add(Me.Label5)
        Me.TabPage2.Controls.Add(Me.btnBuildSourceBrowse)
        Me.TabPage2.Controls.Add(Me.txtBuildSource)
        Me.TabPage2.Controls.Add(Me.Label6)
        Me.TabPage2.Location = New System.Drawing.Point(4, 22)
        Me.TabPage2.Name = "TabPage2"
        Me.TabPage2.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage2.Size = New System.Drawing.Size(465, 236)
        Me.TabPage2.TabIndex = 1
        Me.TabPage2.Text = "Build"
        Me.TabPage2.UseVisualStyleBackColor = True
        '
        'btnBuild
        '
        Me.btnBuild.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnBuild.Location = New System.Drawing.Point(6, 205)
        Me.btnBuild.Name = "btnBuild"
        Me.btnBuild.Size = New System.Drawing.Size(75, 23)
        Me.btnBuild.TabIndex = 15
        Me.btnBuild.Text = "Build"
        Me.btnBuild.UseVisualStyleBackColor = True
        '
        'GroupBox2
        '
        Me.GroupBox2.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox2.Controls.Add(Me.rbBuildNDS)
        Me.GroupBox2.Controls.Add(Me.rbBuildCCI0Key)
        Me.GroupBox2.Controls.Add(Me.rbBuildCIA)
        Me.GroupBox2.Controls.Add(Me.rbBuildCCIDec)
        Me.GroupBox2.Controls.Add(Me.Label4)
        Me.GroupBox2.Controls.Add(Me.rbBuildAuto)
        Me.GroupBox2.Location = New System.Drawing.Point(11, 60)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(446, 139)
        Me.GroupBox2.TabIndex = 14
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Options"
        '
        'rbBuildCCI0Key
        '
        Me.rbBuildCCI0Key.AutoSize = True
        Me.rbBuildCCI0Key.Location = New System.Drawing.Point(119, 63)
        Me.rbBuildCCI0Key.Name = "rbBuildCCI0Key"
        Me.rbBuildCCI0Key.Size = New System.Drawing.Size(189, 17)
        Me.rbBuildCCI0Key.TabIndex = 4
        Me.rbBuildCCI0Key.Text = "0-Key Encrypted CCI (for Gateway)"
        Me.rbBuildCCI0Key.UseVisualStyleBackColor = True
        '
        'rbBuildCIA
        '
        Me.rbBuildCIA.AutoSize = True
        Me.rbBuildCIA.Location = New System.Drawing.Point(119, 86)
        Me.rbBuildCIA.Name = "rbBuildCIA"
        Me.rbBuildCIA.Size = New System.Drawing.Size(94, 17)
        Me.rbBuildCIA.TabIndex = 3
        Me.rbBuildCIA.Text = "Decrypted CIA"
        Me.rbBuildCIA.UseVisualStyleBackColor = True
        '
        'rbBuildCCIDec
        '
        Me.rbBuildCCIDec.AutoSize = True
        Me.rbBuildCCIDec.Location = New System.Drawing.Point(119, 40)
        Me.rbBuildCCIDec.Name = "rbBuildCCIDec"
        Me.rbBuildCCIDec.Size = New System.Drawing.Size(322, 17)
        Me.rbBuildCCIDec.TabIndex = 2
        Me.rbBuildCCIDec.Text = "Decrypted CCI (for Citra or Sky 3DS/Gateway+CFW+Decrypt9)"
        Me.rbBuildCCIDec.UseVisualStyleBackColor = True
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(6, 19)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(105, 13)
        Me.Label4.TabIndex = 1
        Me.Label4.Text = "Output ROM Format:"
        '
        'rbBuildAuto
        '
        Me.rbBuildAuto.AutoSize = True
        Me.rbBuildAuto.Checked = True
        Me.rbBuildAuto.Location = New System.Drawing.Point(119, 17)
        Me.rbBuildAuto.Name = "rbBuildAuto"
        Me.rbBuildAuto.Size = New System.Drawing.Size(47, 17)
        Me.rbBuildAuto.TabIndex = 0
        Me.rbBuildAuto.TabStop = True
        Me.rbBuildAuto.Text = "Auto"
        Me.rbBuildAuto.UseVisualStyleBackColor = True
        '
        'btnBuildOutputBrowse
        '
        Me.btnBuildOutputBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBuildOutputBrowse.Location = New System.Drawing.Point(382, 32)
        Me.btnBuildOutputBrowse.Name = "btnBuildOutputBrowse"
        Me.btnBuildOutputBrowse.Size = New System.Drawing.Size(75, 23)
        Me.btnBuildOutputBrowse.TabIndex = 13
        Me.btnBuildOutputBrowse.Text = "Browse..."
        Me.btnBuildOutputBrowse.UseVisualStyleBackColor = True
        '
        'txtBuildDestination
        '
        Me.txtBuildDestination.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtBuildDestination.Location = New System.Drawing.Point(101, 34)
        Me.txtBuildDestination.Name = "txtBuildDestination"
        Me.txtBuildDestination.Size = New System.Drawing.Size(275, 20)
        Me.txtBuildDestination.TabIndex = 12
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(8, 37)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(70, 13)
        Me.Label5.TabIndex = 11
        Me.Label5.Text = "Output ROM:"
        '
        'btnBuildSourceBrowse
        '
        Me.btnBuildSourceBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBuildSourceBrowse.Location = New System.Drawing.Point(382, 6)
        Me.btnBuildSourceBrowse.Name = "btnBuildSourceBrowse"
        Me.btnBuildSourceBrowse.Size = New System.Drawing.Size(75, 23)
        Me.btnBuildSourceBrowse.TabIndex = 10
        Me.btnBuildSourceBrowse.Text = "Browse..."
        Me.btnBuildSourceBrowse.UseVisualStyleBackColor = True
        '
        'txtBuildSource
        '
        Me.txtBuildSource.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtBuildSource.Location = New System.Drawing.Point(101, 8)
        Me.txtBuildSource.Name = "txtBuildSource"
        Me.txtBuildSource.Size = New System.Drawing.Size(275, 20)
        Me.txtBuildSource.TabIndex = 9
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(8, 11)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(89, 13)
        Me.Label6.TabIndex = 8
        Me.Label6.Text = "Source Directory:"
        '
        'TabPage3
        '
        Me.TabPage3.Controls.Add(Me.btnHan)
        Me.TabPage3.Controls.Add(Me.GroupBox3)
        Me.TabPage3.Controls.Add(Me.btnHansSDBrowse)
        Me.TabPage3.Controls.Add(Me.txtHansSD)
        Me.TabPage3.Controls.Add(Me.Label8)
        Me.TabPage3.Controls.Add(Me.btnHansSourceBrowse)
        Me.TabPage3.Controls.Add(Me.txtHansSource)
        Me.TabPage3.Controls.Add(Me.Label9)
        Me.TabPage3.Location = New System.Drawing.Point(4, 22)
        Me.TabPage3.Name = "TabPage3"
        Me.TabPage3.Padding = New System.Windows.Forms.Padding(3)
        Me.TabPage3.Size = New System.Drawing.Size(465, 207)
        Me.TabPage3.TabIndex = 2
        Me.TabPage3.Text = "HANS"
        Me.TabPage3.UseVisualStyleBackColor = True
        '
        'btnHan
        '
        Me.btnHan.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.btnHan.Location = New System.Drawing.Point(6, 176)
        Me.btnHan.Name = "btnHan"
        Me.btnHan.Size = New System.Drawing.Size(75, 23)
        Me.btnHan.TabIndex = 23
        Me.btnHan.Text = "Build"
        Me.btnHan.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
        Me.GroupBox3.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox3.Controls.Add(Me.Label10)
        Me.GroupBox3.Controls.Add(Me.Label7)
        Me.GroupBox3.Controls.Add(Me.txtHansShortName)
        Me.GroupBox3.Location = New System.Drawing.Point(11, 60)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(446, 110)
        Me.GroupBox3.TabIndex = 22
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Options"
        '
        'Label10
        '
        Me.Label10.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label10.Location = New System.Drawing.Point(6, 42)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(433, 44)
        Me.Label10.TabIndex = 2
        Me.Label10.Text = "This is a short name to be used for the HANS files and Homebrew Launcher shortcut" &
    ".  Shorter names with standard characters are preferable, but the exact requirem" &
    "ents are unknown."
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(6, 22)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(82, 13)
        Me.Label7.TabIndex = 1
        Me.Label7.Text = "Raw File Name:"
        '
        'txtHansShortName
        '
        Me.txtHansShortName.Location = New System.Drawing.Point(90, 19)
        Me.txtHansShortName.Name = "txtHansShortName"
        Me.txtHansShortName.Size = New System.Drawing.Size(100, 20)
        Me.txtHansShortName.TabIndex = 0
        '
        'btnHansSDBrowse
        '
        Me.btnHansSDBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnHansSDBrowse.Location = New System.Drawing.Point(382, 32)
        Me.btnHansSDBrowse.Name = "btnHansSDBrowse"
        Me.btnHansSDBrowse.Size = New System.Drawing.Size(75, 23)
        Me.btnHansSDBrowse.TabIndex = 21
        Me.btnHansSDBrowse.Text = "Browse..."
        Me.btnHansSDBrowse.UseVisualStyleBackColor = True
        '
        'txtHansSD
        '
        Me.txtHansSD.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtHansSD.Location = New System.Drawing.Point(101, 34)
        Me.txtHansSD.Name = "txtHansSD"
        Me.txtHansSD.Size = New System.Drawing.Size(275, 20)
        Me.txtHansSD.TabIndex = 20
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(8, 37)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(51, 13)
        Me.Label8.TabIndex = 19
        Me.Label8.Text = "SD Root:"
        '
        'btnHansSourceBrowse
        '
        Me.btnHansSourceBrowse.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnHansSourceBrowse.Location = New System.Drawing.Point(382, 6)
        Me.btnHansSourceBrowse.Name = "btnHansSourceBrowse"
        Me.btnHansSourceBrowse.Size = New System.Drawing.Size(75, 23)
        Me.btnHansSourceBrowse.TabIndex = 18
        Me.btnHansSourceBrowse.Text = "Browse..."
        Me.btnHansSourceBrowse.UseVisualStyleBackColor = True
        '
        'txtHansSource
        '
        Me.txtHansSource.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtHansSource.Location = New System.Drawing.Point(101, 8)
        Me.txtHansSource.Name = "txtHansSource"
        Me.txtHansSource.Size = New System.Drawing.Size(275, 20)
        Me.txtHansSource.TabIndex = 17
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(8, 11)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(89, 13)
        Me.Label9.TabIndex = 16
        Me.Label9.Text = "Source Directory:"
        '
        'StatusStrip1
        '
        Me.StatusStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.pbProgress, Me.lblStatus})
        Me.StatusStrip1.Location = New System.Drawing.Point(0, 265)
        Me.StatusStrip1.Name = "StatusStrip1"
        Me.StatusStrip1.Size = New System.Drawing.Size(473, 22)
        Me.StatusStrip1.SizingGrip = False
        Me.StatusStrip1.TabIndex = 9
        Me.StatusStrip1.Text = "StatusStrip1"
        '
        'pbProgress
        '
        Me.pbProgress.Maximum = 1
        Me.pbProgress.Name = "pbProgress"
        Me.pbProgress.Size = New System.Drawing.Size(100, 16)
        Me.pbProgress.Style = System.Windows.Forms.ProgressBarStyle.Continuous
        '
        'lblStatus
        '
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(39, 17)
        Me.lblStatus.Text = "Ready"
        '
        'rbExtractNDS
        '
        Me.rbExtractNDS.AutoSize = True
        Me.rbExtractNDS.Location = New System.Drawing.Point(119, 109)
        Me.rbExtractNDS.Name = "rbExtractNDS"
        Me.rbExtractNDS.Size = New System.Drawing.Size(48, 17)
        Me.rbExtractNDS.TabIndex = 5
        Me.rbExtractNDS.TabStop = True
        Me.rbExtractNDS.Text = "NDS"
        Me.rbExtractNDS.UseVisualStyleBackColor = True
        '
        'rbBuildNDS
        '
        Me.rbBuildNDS.AutoSize = True
        Me.rbBuildNDS.Location = New System.Drawing.Point(119, 109)
        Me.rbBuildNDS.Name = "rbBuildNDS"
        Me.rbBuildNDS.Size = New System.Drawing.Size(48, 17)
        Me.rbBuildNDS.TabIndex = 6
        Me.rbBuildNDS.TabStop = True
        Me.rbBuildNDS.Text = "NDS"
        Me.rbBuildNDS.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(473, 287)
        Me.Controls.Add(Me.StatusStrip1)
        Me.Controls.Add(Me.TabControl1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "Form1"
        Me.Text = ".Net 3DS Toolkit GUI"
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.TabPage2.ResumeLayout(False)
        Me.TabPage2.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.TabPage3.ResumeLayout(False)
        Me.TabPage3.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.StatusStrip1.ResumeLayout(False)
        Me.StatusStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabPage1 As TabPage
    Friend WithEvents btnExtract As Button
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents rbExtractCXIDec As RadioButton
    Friend WithEvents rbExtractCCIDec As RadioButton
    Friend WithEvents Label3 As Label
    Friend WithEvents rbExtractAuto As RadioButton
    Friend WithEvents btnExtractDestinationBrowse As Button
    Friend WithEvents txtExtractDestination As TextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents btnExtractSourceBrowse As Button
    Friend WithEvents txtExtractSource As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents TabPage2 As TabPage
    Friend WithEvents btnBuild As Button
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents rbBuildCCI0Key As RadioButton
    Friend WithEvents rbBuildCIA As RadioButton
    Friend WithEvents rbBuildCCIDec As RadioButton
    Friend WithEvents Label4 As Label
    Friend WithEvents rbBuildAuto As RadioButton
    Friend WithEvents btnBuildOutputBrowse As Button
    Friend WithEvents txtBuildDestination As TextBox
    Friend WithEvents Label5 As Label
    Friend WithEvents btnBuildSourceBrowse As Button
    Friend WithEvents txtBuildSource As TextBox
    Friend WithEvents Label6 As Label
    Friend WithEvents TabPage3 As TabPage
    Friend WithEvents btnHan As Button
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents Label7 As Label
    Friend WithEvents txtHansShortName As TextBox
    Friend WithEvents btnHansSDBrowse As Button
    Friend WithEvents txtHansSD As TextBox
    Friend WithEvents Label8 As Label
    Friend WithEvents btnHansSourceBrowse As Button
    Friend WithEvents txtHansSource As TextBox
    Friend WithEvents Label9 As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents pbProgress As ToolStripProgressBar
    Friend WithEvents lblStatus As ToolStripStatusLabel
    Friend WithEvents rbExtractCIA As RadioButton
    Friend WithEvents rbExtractNDS As RadioButton
    Friend WithEvents rbBuildNDS As RadioButton
End Class
