Option Explicit On
Imports System.Windows.Forms
Imports System.IO
Imports System.Data.OleDb


Class MainWindow
    Private Sub buttonCancel_Click(sender As Object, e As RoutedEventArgs) Handles buttonCancel.Click
        End
    End Sub

    Private Sub buttonPosm_Click(sender As Object, e As RoutedEventArgs) Handles buttonPosm.Click
        Dim folderDlg As New FolderBrowserDialog
        folderDlg.ShowNewFolderButton = True
        If (folderDlg.ShowDialog() = Forms.DialogResult.OK) Then
            textBoxPosm.Text = folderDlg.SelectedPath
            'Dim root As Environment.SpecialFolder = folderDlg.RootFolder
        End If
    End Sub

    Private Sub buttonItpipes_Click(sender As Object, e As RoutedEventArgs) Handles buttonItpipes.Click
        Dim fd As OpenFileDialog = New OpenFileDialog()

        fd.Title = "ITPipes Database"
        fd.InitialDirectory = "C:\"
        fd.Filter = "All files (*.*)|*.*|All files (*.*)|*.*"
        fd.FilterIndex = 2
        fd.RestoreDirectory = True

        If fd.ShowDialog() = Forms.DialogResult.OK Then
            textBoxItpipes.Text = fd.FileName
        End If
    End Sub

    Private Sub buttonOk_Click(sender As Object, e As RoutedEventArgs) Handles buttonOk.Click
        Dim rs
        Dim objStartFolder As New IO.DirectoryInfo(textBoxPosm.Text)
        Dim RSFile As IO.FileInfo() = objStartFolder.GetFiles("POSM.mdb", IO.SearchOption.AllDirectories)

        For Each fi In RSFile
            Dim connStr, objConn, fso, connStrItpipes, objConnItpipes
            connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & fi.FullName & ";"

            objConn = CreateObject("ADODB.Connection")
            fso = CreateObject("Scripting.FileSystemObject")

            objConn.Open(connStr)
            objConn.BeginTrans

            Dim SQL_queryAsset = "(SELECT
                            Field3 As Owner
                            Field4 As Customer
                            Field5 As Drainage_Area
                            Field6 As PO_Number
                            Field7 As ML_Name
                            Field9 AS Street
                            Field10 AS City
                            Field11 AS US_MH
                            Field12 AS US_RimtoInvert
                            Field13 AS US_GradetoInvert
                            Field14 AS US_RimtoGrade
                            Field15 AS DS_MH
                            Field16 AS DS_RimtoInvert
                            Field17 AS DS_GradetoInvert
                            Field18 AS DS_RimtoGrade
                            Field19 AS Asset_Use
                            Field21 AS Pipe_Height
                            Field22 AS Pipe_Width
                            Field23 AS Pipe_Shape
                            Field24 AS Material
                            Field25 AS Lining_Method
                            Field26 AS Joint_Length
                            Field27 AS Section_Length
                            Field29 AS Year_Constructed
                            Field30 AS Year_Renewed
                            Field32 AS Media_Number
                            Field34 AS Sewer_Category
                            Field38 AS Location
                            Field40 AS Location_Details
                            Field57 AS ProjectTitle
                            FROM Session) AS Posm_Asset"
            Dim SQL_queryInspection = "(SELECT
                            Field1 As Operator
                            Field2 As Certificate_Number
                            Field8 As Inspection_Date
                            Field20 AS Inspection_Direction
                            Field28 AS Inspected_Length
                            Field31 AS Flow_Control
                            Field33 AS Reason_of_Inspection
                            Field35 AS Cleaned
                            Field36 AS Clean_Date
                            Field37 AS Weather
                            Field39 AS Additional_Info
                            Field41 AS PACP_Custom_1
                            Field42 AS PACP_Custom_2
                            Field43 AS PACP_Custom_3
                            Field44 AS PACP_Custom_4
                            Field45 AS PACP_Custom_5
                            Field46 AS PACP_Custom_6
                            Field47 AS PACP_Custom_7
                            Field48 AS PACP_Custom_8
                            Field49 AS PACP_Custom_9
                            Field50 AS PACP_Custom_10
                            Field56 AS WO_Number
                            Field53 AS IsImperial
                            Field54 AS Current_Status
                            FROM Session) AS Posm_Inspectiont"
            rs = objConn.execute(SQL_queryAsset)
            rs = objConn.execute(SQL_queryInspection)
            objConn.Close()

            connStrItpipes = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" & textBoxItpipes.Text & ";"

            objConnItpipes = CreateObject("ADODB.Connection")
            objConnItpipes.Open(connStrItpipes)

            Dim SQL_Query_Asset = "INSERT INTO ML (Owner, Customer, Drainage_Area, PO_Number, ML_Name, Street, City, US_MH, US_RimtoInvert, US_GradetoInvert, US_RimtoGrade, DS_MH, DS_RimtoInvert, DS_GradetoInvert, DS_RimtoGrade, Asset_Use, Pipe_Height, Pipe_Width, Pipe_Shape, Material, Lining_Method, Joint_Length, Section_Length, Year_Constructed, Year_Renewed, Media_Number, Sewer_Category, Location, Location_Details, ProjectTitle)
                                    Select Owner, Customer, Drainage_Area, PO_Number, ML_Name, Street, City, US_MH, US_RimtoInvert, US_GradetoInvert, US_RimtoGrade, DS_MH, DS_RimtoInvert, DS_GradetoInvert, DS_RimtoGrade, Asset_Use, Pipe_Height, Pipe_Width, Pipe_Shape, Material, Lining_Method, Joint_Length, Section_Length, Year_Constructed, Year_Renewed, Media_Number, Sewer_Category, Location, Location_Details, ProjectTitle FROM Posm_Asset"
            Dim SQL_Query_Inspection = "INSERT INTO MLI (Operator, Certificate_Number, Inspection_Date, Inspection_Direction, Inspected_Length, Flow_Control, Reason_of_Inspection, Cleaned, Clean_Date, Weather, Additional_Info, PACP_Custom_1, PACP_Custom_2, PACP_Custom_3, PACP_Custom_4, PACP_Custom_5, PACP_Custom_6, PACP_Custom_7, PACP_Custom_8, PACP_Custom_9, PACP_Custom_10, WO_Number, IsImperial, Current_Status)
                                    Select Operator, Certificate_Number, Inspection_Date, Inspection_Direction, Inspected_Length, Flow_Control, Reason_of_Inspection, Cleaned, Clean_Date, Weather, Additional_Info, PACP_Custom_1, PACP_Custom_2, PACP_Custom_3, PACP_Custom_4, PACP_Custom_5, PACP_Custom_6, PACP_Custom_7, PACP_Custom_8, PACP_Custom_9, PACP_Custom_10, WO_Number, IsImperial, Current_Status From Posm_Inspection"
            rs = objConn.execute(SQL_Query_Asset)
            rs = objConn.execute(SQL_Query_Inspection)
            objConnItpipes.Clost()
            RS = Nothing

        Next
    End Sub
End Class

