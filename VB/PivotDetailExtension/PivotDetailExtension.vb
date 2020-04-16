Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows.Forms
Imports System.Xml
Imports System.Xml.Linq
Imports System.Xml.Serialization
Imports DevExpress.DashboardCommon
Imports DevExpress.DashboardWin
Imports DevExpress.XtraBars
Imports DevExpress.XtraBars.Ribbon
Imports DevExpress.XtraPivotGrid

Namespace PivotExtension
	Public Class PivotDetailExtension
		Private Const PropertyName As String = "PivotDetailExtension"

		Private showDetatilsBarButton As BarCheckItem
		Private dashboardControl As IDashboardControl
		Private ReadOnly Property dashboardDesigner() As DashboardDesigner
			Get
				Return TryCast(dashboardControl, DashboardDesigner)
			End Get
		End Property

		#Region "Initialization and Registration"

		''' <summary>
		''' Creates the "Display Details" button for the Ribbon
		''' </summary>
		Private Function CreateRibbonButton() As BarCheckItem
			Dim showDetailsItem As New BarCheckItem()
			showDetailsItem.Caption = "Display Details"
			showDetailsItem.ImageOptions.SvgImage = My.Resources.Detailed
			AddHandler showDetailsItem.ItemClick, AddressOf showDetailsItem_ItemClick
			Return showDetailsItem
		End Function

		''' <summary>
		''' Attaches the Extension to DashboardViewer or DashboardDesigner
		''' </summary>
		Public Sub Attach(ByVal dashboardControl As IDashboardControl)
			Detach()
			Me.dashboardControl = dashboardControl
			' Handle Events
			AddHandler Me.dashboardControl.DashboardItemClick, AddressOf DashboardItemClick
			If dashboardDesigner IsNot Nothing Then
				AddButtonToRibbon()
				AddHandler dashboardDesigner.DashboardCustomPropertyChanged, AddressOf TargetDesigner_DashboardCustomPropertyChanged
				AddHandler dashboardDesigner.DashboardItemSelected, AddressOf DashboardItemSelected

			End If
		End Sub

		''' <summary>
		''' Adds the "Display Details" button to DashboardDesigner's Ribbon
		''' </summary>
		Private Sub AddButtonToRibbon()
			Dim ribbon As RibbonControl = dashboardDesigner.Ribbon
			Dim page As RibbonPage = ribbon.GetDashboardRibbonPage(DashboardBarItemCategory.PivotTools, DashboardRibbonPage.Data)
			Dim group As RibbonPageGroup = page.Groups.OfType(Of DevExpress.DashboardWin.Bars.InteractivitySettingsRibbonPageGroup)().First()
			showDetatilsBarButton = CreateRibbonButton()
			group.ItemLinks.Add(showDetatilsBarButton)
		End Sub

		''' <summary>
		''' Removes the "Display Details" button from DashboardDesigner's Ribbon
		''' </summary>
		Private Sub RemoveButtonFromRibbon()
			Dim ribbon As RibbonControl = dashboardDesigner.Ribbon
			ribbon.Items.Remove(showDetatilsBarButton)
		End Sub


		''' <summary>
		''' Detaches the Extension from the control
		''' </summary>
		Public Sub Detach()
			If dashboardControl Is Nothing Then
				Return
			End If
			If dashboardDesigner IsNot Nothing Then
				RemoveButtonFromRibbon()
				RemoveHandler dashboardDesigner.DashboardCustomPropertyChanged, AddressOf TargetDesigner_DashboardCustomPropertyChanged
				RemoveHandler dashboardDesigner.DashboardItemSelected, AddressOf DashboardItemSelected
			End If
			RemoveHandler dashboardControl.DashboardItemClick, AddressOf DashboardItemClick
			dashboardControl = Nothing
		End Sub
		#End Region

		#Region "Designer Business Logic"

		''' <summary>
		''' Updates the "Dispaly Details" button's state after the custom prioperty has been enabled / disabled. 
		''' </summary>
		Private Sub TargetDesigner_DashboardCustomPropertyChanged(ByVal sender As Object, ByVal e As CustomPropertyChangedEventArgs)
			UpdateButtonState()
		End Sub
		''' <summary>
		''' The "Dispaly Details" button's click handler. Enables / Disables the custom functionality.
		''' </summary>
		Private Sub showDetailsItem_ItemClick(ByVal sender As Object, ByVal e As ItemClickEventArgs)
			If TypeOf dashboardDesigner.SelectedDashboardItem Is PivotDashboardItem Then
				Dim newValue As Boolean = Not IsDetailsEnabled(dashboardDesigner.SelectedDashboardItem)
				Dim status As String = If(IsDetailsEnabled(dashboardDesigner.SelectedDashboardItem), "Enaled", "Disabled")
                Dim historyItem = New CustomPropertyHistoryItem(dashboardDesigner.SelectedDashboardItem, PropertyName, newValue.ToString(), "Detail Popup " & status)
                dashboardDesigner.AddToHistory(historyItem)
			End If
		End Sub

		''' <summary>
		''' Set the Checked/Unchecked state of the "Dispaly Details" button based on the currently selected item.
		''' </summary>
		Private Sub UpdateButtonState()
			If dashboardDesigner.SelectedDashboardItem Is Nothing Then
				Return
			End If
			showDetatilsBarButton.Checked = IsDetailsEnabled(dashboardDesigner.SelectedDashboardItem)
		End Sub

		''' <summary>
		''' Invokes update of the "Dispaly Details" button's state when selecting another item.
		''' </summary>
		Private Sub DashboardItemSelected(ByVal sender As Object, ByVal e As DashboardItemSelectedEventArgs)
			If TypeOf e.SelectedDashboardItem Is PivotDashboardItem Then
				UpdateButtonState()
			End If
		End Sub

		#End Region

		#Region "Business Logic Common for Designer and Viewer"

		''' <summary>
		''' Used to get underlying data and display the DetailData dialog
		''' </summary>
		Private Sub DashboardItemClick(ByVal sender As Object, ByVal e As DashboardItemMouseActionEventArgs)

			If IsDetailsEnabled(e.DashboardItemName) Then
				Dim pivot As PivotGridControl = TryCast(dashboardDesigner.GetUnderlyingControl(e.DashboardItemName), PivotGridControl)
				Dim hi As PivotGridHitInfo = pivot.CalcHitInfo(pivot.PointToClient(Cursor.Position))

				Dim doNotShowDataForThisArea As Boolean = (hi.HitTest = PivotGridHitTest.Value AndAlso hi.ValueInfo.ValueHitTest = PivotGridValueHitTest.ExpandButton) OrElse (hi.HitTest = PivotGridHitTest.None)
				If Not doNotShowDataForThisArea Then
					Using detailForm As New DetailData(e.GetUnderlyingData())
						detailForm.ShowDialog()
					End Using
				End If
			End If
		End Sub

		''' <summary>
		''' Returns a value indicating whether the custom option is enabled for a specific Pivot Item.  
		''' </summary>
		Private Function IsDetailsEnabled(ByVal item As DashboardItem) As Boolean
			Return Convert.ToBoolean(item.CustomProperties.GetValue(PropertyName))
		End Function

		''' <summary>
		''' Returns a value indicating whether the custom option is enabled for a Pivot Item with a specific component name.  
		''' </summary>
		Private Function IsDetailsEnabled(ByVal itemName As String) As Boolean
			Return IsDetailsEnabled(dashboardDesigner.Dashboard.Items(itemName))
		End Function
		#End Region
	End Class
End Namespace
