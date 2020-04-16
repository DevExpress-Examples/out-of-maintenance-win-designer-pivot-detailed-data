Imports System
Imports System.Linq
Imports System.Windows.Forms
Imports DevExpress.DashboardCommon
Imports DevExpress.XtraEditors

Namespace PivotExtension
	Partial Public Class DetailData
		Inherits XtraForm

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
