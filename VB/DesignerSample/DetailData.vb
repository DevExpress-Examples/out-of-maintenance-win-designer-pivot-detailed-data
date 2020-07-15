Imports DevExpress.DashboardCommon
Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Data
Imports System.Drawing
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms

Namespace DesignerSample
	Partial Public Class DetailData
		Inherits Form

		Public Sub New()
			InitializeComponent()
		End Sub

		Public Sub New(ByVal data As DashboardUnderlyingDataSet)
			InitializeComponent()
			gridControl1.DataSource = data
			gridView1.PopulateColumns()
		End Sub
	End Class
End Namespace
