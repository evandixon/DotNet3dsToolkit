Public Class BuildOptions
    Public Property SourceDirectory As String
    Public Property DestinationROM As String
    Public Property CompressCodeBin As Boolean

    Public Property RomFSDirName As String = "RomFS"
    Public Property ExeFSDirName As String = "ExeFS"
    Public Property ExeFSHeaderName As String = "HeaderExeFS.bin"
    Public Property ExheaderName As String = "ExHeader.bin"
    Public Property LogoLZName As String = "LogoLZ.bin"
    Public Property PlainRGNName As String = "PlainRGN.bin"
    Public Property Partition0HeaderName As String = "HeaderNCCH0.bin"
    Public Property Partition1HeaderName As String = "HeaderNCCH1.bin"
    Public Property Partition2HeaderName As String = "HeaderNCCH2.bin"
    Public Property Partition6HeaderName As String = "HeaderNCCH6.bin"
    Public Property Partition7HeaderName As String = "HeaderNCCH7.bin"
    Public Property ExtractedManualDirName As String = "Manual"
    Public Property ExtractedDownloadPlayDirName As String = "DownloadPlay"
    Public Property N3DSUpdateDirName As String = "N3DSUpdate"
    Public Property O3DSUpdateDirName As String = "O3DSUpdate"
    Public Property RootHeaderName As String = "HeaderNCCH.bin"
End Class
