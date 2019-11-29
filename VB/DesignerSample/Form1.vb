Imports DevExpress.XtraEditors

Namespace DesignerSample
	Partial Public Class Form1
		Inherits XtraForm

		Public Sub New()
			InitializeComponent()
			dashboardDesigner1.CreateRibbon()

			' Extension Registration code. 
			' Execute it after the Ribbon has been created and before loading a dashboard.
			Dim extension As New PivotDetailExtension()
			extension.Attach(dashboardDesigner1)
			dashboardDesigner1.LoadDashboard("nwind.xml")
		End Sub
	End Class
End Namespace
