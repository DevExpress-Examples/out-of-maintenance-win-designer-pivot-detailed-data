Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Windows.Forms
Imports System.Xml
Imports System.Xml.Linq
Imports System.Xml.Serialization
Imports DevExpress.DashboardCommon
Imports DevExpress.DashboardWin
Imports DevExpress.DashboardWin.Bars
Imports DevExpress.XtraBars
Imports DevExpress.XtraBars.Ribbon
Imports DevExpress.XtraPivotGrid

Namespace DesignerSample
	Public Class PivotDetailExtension
		#Region "Initialization and Registration"
		Private showDetatilsButton As BarCheckItem
		Private targetDesigner As DashboardDesigner
		Private targetViewer As DashboardViewer

		Public Sub New()
			showDetatilsButton = CreateRibbonButton()
		End Sub

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
		''' Attaches the Extension to DashboardViewer
		''' </summary>
		Public Sub Attach(ByVal viewer As DashboardViewer)
			Detach()
			targetViewer = viewer
			pivotItemsWithEnabledDetails = New List(Of String)()
			' Handle Events
			AddHandler targetViewer.DashboardItemClick, AddressOf DashboardItemClick
			AddHandler targetViewer.DashboardItemControlCreated, AddressOf DashboardItemControlCreated
			AddHandler targetViewer.DashboardLoaded, AddressOf DashboardLoaded
		End Sub

		''' <summary>
		''' Attaches the Extension to DashboardDesigner
		''' </summary>
		Public Sub Attach(ByVal designer As DashboardDesigner)
			Detach()
			targetDesigner = designer
			pivotItemsWithEnabledDetails = New List(Of String)()
			'Add Ribbon Toolbar button
			Dim ribbon As RibbonControl = TryCast(targetDesigner.MenuManager, RibbonControl)
			Dim category As PivotToolsRibbonPageCategory = ribbon.PageCategories.OfType(Of PivotToolsRibbonPageCategory)().First()
			Dim page As DataRibbonPage = category.Pages.OfType(Of DataRibbonPage)().First()
			Dim InteractivityGroup As InteractivitySettingsRibbonPageGroup = page.Groups.OfType(Of InteractivitySettingsRibbonPageGroup)().First()
			InteractivityGroup.ItemLinks.Add(showDetatilsButton)
			' Handle Events
			AddHandler targetDesigner.DashboardItemClick, AddressOf DashboardItemClick
			AddHandler targetDesigner.DashboardItemControlCreated, AddressOf DashboardItemControlCreated
			AddHandler targetDesigner.DashboardLoaded, AddressOf DashboardLoaded
			AddHandler targetDesigner.DashboardSaving, AddressOf Designer_DashboardSaving
			AddHandler targetDesigner.DashboardItemSelectionChanged, AddressOf Designer_DashboardItemSelected
			AddHandler targetDesigner.Dashboard.ItemCollectionChanged, AddressOf Dashboard_ItemCollectionChanged
		End Sub

		''' <summary>
		''' Detaches the Extension from the control
		''' </summary>
		Public Sub Detach()
			If targetDesigner IsNot Nothing Then
				Dim ribbon As RibbonControl = TryCast(targetDesigner.MenuManager, RibbonControl)
				Dim category As PivotToolsRibbonPageCategory = ribbon.PageCategories.OfType(Of PivotToolsRibbonPageCategory)().First()
				Dim page As DataRibbonPage = category.Pages.OfType(Of DataRibbonPage)().First()
				Dim InteractivityGroup As InteractivitySettingsRibbonPageGroup = page.Groups.OfType(Of InteractivitySettingsRibbonPageGroup)().First()
				InteractivityGroup.ItemLinks.Remove(showDetatilsButton)

				RemoveHandler targetDesigner.DashboardItemClick, AddressOf DashboardItemClick
				RemoveHandler targetDesigner.DashboardItemControlCreated, AddressOf DashboardItemControlCreated
				RemoveHandler targetDesigner.DashboardSaving, AddressOf Designer_DashboardSaving
				RemoveHandler targetDesigner.DashboardLoaded, AddressOf DashboardLoaded
				RemoveHandler targetDesigner.DashboardItemSelectionChanged, AddressOf Designer_DashboardItemSelected
				RemoveHandler targetDesigner.Dashboard.ItemCollectionChanged, AddressOf Dashboard_ItemCollectionChanged
			End If
			If targetViewer IsNot Nothing Then
				RemoveHandler targetViewer.DashboardItemClick, AddressOf DashboardItemClick
				RemoveHandler targetViewer.DashboardItemControlCreated, AddressOf DashboardItemControlCreated
				AddHandler targetViewer.DashboardLoaded, AddressOf DashboardLoaded
			End If
		End Sub
		#End Region

		#Region "Designer Business Logic"
		''' <summary>
		''' The "Dispaly Details" button's click handler. Enables / Disables the custom functionality.
		''' </summary>
		Private Sub showDetailsItem_ItemClick(ByVal sender As Object, ByVal e As ItemClickEventArgs)
			If TypeOf targetDesigner.SelectedDashboardItem Is PivotDashboardItem Then
				If IsDetailsEnabled(targetDesigner.SelectedDashboardItem.ComponentName) Then
					pivotItemsWithEnabledDetails.Remove(targetDesigner.SelectedDashboardItem.ComponentName)
				Else
					pivotItemsWithEnabledDetails.Add(targetDesigner.SelectedDashboardItem.ComponentName)
				End If
			End If
			UpdateButtonState()
		End Sub

		''' <summary>
		''' Set the Checked/Unchecked state of the "Dispaly Details" button based on the currently selected item.
		''' </summary>
		Private Sub UpdateButtonState()
			If targetDesigner.SelectedDashboardItem Is Nothing Then
				Return
			End If
			showDetatilsButton.Checked = IsDetailsEnabled(targetDesigner.SelectedDashboardItem.ComponentName)
		End Sub

		''' <summary>
		''' Invokes update of the "Dispaly Details" button's state when selecting another item.
		''' </summary>
		Private Sub Designer_DashboardItemSelected(ByVal sender As Object, ByVal e As DashboardItemSelectionChangedEventArgs)
			If TypeOf targetDesigner.Dashboard.Items(e.DashboardItemName) Is PivotDashboardItem Then
				UpdateButtonState()
			End If
		End Sub

		''' <summary>
		''' Saves information about PivotGrids with enabled custom option to the dashboard *.xml file (UserData section).
		''' </summary>
		Private Sub Designer_DashboardSaving(ByVal sender As Object, ByVal e As DashboardSavingEventArgs)
			Dim xs As New XmlSerializer(GetType(List(Of String)))
			Dim xElement As New XDocument()

			Using xw As XmlWriter = xElement.CreateWriter()
				xs.Serialize(xw, pivotItemsWithEnabledDetails)
			End Using
			e.Dashboard.UserData = xElement.Root
		End Sub

		''' <summary>
		''' Updates information about PivotGrids with enabled custom option after a Pivot Item has been deleted.
		''' </summary>
		Private Sub Dashboard_ItemCollectionChanged(ByVal sender As Object, ByVal e As DevExpress.DataAccess.NotifyingCollectionChangedEventArgs(Of DashboardItem))
			For Each pivot As PivotDashboardItem In e.RemovedItems.OfType(Of PivotDashboardItem)()
				If pivotItemsWithEnabledDetails.Contains(pivot.ComponentName) Then
					pivotItemsWithEnabledDetails.Remove(pivot.ComponentName)
				End If
			Next pivot
		End Sub

		#End Region

		#Region "Business Logic Common for Designer and Viewer"

		''' <summary>
		''' Contains ComponentNames of Pivot Items with the enabled custom option
		''' </summary>
		Private pivotItemsWithEnabledDetails As List(Of String)

		''' <summary>
		''' Loads information about Pivot Items with the enabled custom option after a Dashboard has been loaded
		''' </summary>
		Private Sub DashboardLoaded(ByVal sender As Object, ByVal e As DashboardLoadedEventArgs)
			AddHandler e.Dashboard.ItemCollectionChanged, AddressOf Dashboard_ItemCollectionChanged
			pivotItemsWithEnabledDetails = New List(Of String)()

			If e.Dashboard.UserData IsNot Nothing Then
				Dim xs As New XmlSerializer(GetType(List(Of String)))
				Using xr As XmlReader = e.Dashboard.UserData.CreateReader()
					If xs.CanDeserialize(xr) Then
						pivotItemsWithEnabledDetails = TryCast(xs.Deserialize(xr), List(Of String))
					End If
				End Using
			End If
			UpdateButtonState()

		End Sub

		''' <summary>
		''' Returns a value indicating whether the custom option is enabled for a specific Pivot Item.  
		''' </summary>
		Private Function IsDetailsEnabled(ByVal componentName As String) As Boolean
			Return pivotItemsWithEnabledDetails.Contains(componentName)
		End Function

		#End Region

		#Region "Action Business Logic"

		''' <summary>
		''' Returns a value indicating if the custom logic should be skipped for the current area (empty area and collapse/expand button).
		''' </summary>
		Private doNotShowDataForThisArea As Boolean = False

		''' <summary>
		''' Used to handle the underlying Pivot Gid Control's MouseDown event to determine the clicked area
		''' </summary>
		Private Sub DashboardItemControlCreated(ByVal sender As Object, ByVal e As DashboardItemControlEventArgs)
			If e.PivotGridControl IsNot Nothing Then
				AddHandler e.PivotGridControl.MouseDown, AddressOf PivotGridControl_MouseDown
			End If
		End Sub

		''' <summary>
		''' Used to determine the clicked area and skip DashboardItemClick processing if the Expand/Collapse button is pressed
		''' </summary>
		Private Sub PivotGridControl_MouseDown(ByVal sender As Object, ByVal e As MouseEventArgs)
			Dim pivot As PivotGridControl = TryCast(sender, PivotGridControl)
			Dim hi As PivotGridHitInfo = pivot.CalcHitInfo(e.Location)

			doNotShowDataForThisArea = (hi.HitTest = PivotGridHitTest.Value AndAlso hi.ValueInfo.ValueHitTest = PivotGridValueHitTest.ExpandButton) OrElse (hi.HitTest = PivotGridHitTest.None)
		End Sub

		''' <summary>
		''' Used to get underlying data and display the DetailData dialog
		''' </summary>
		Private Sub DashboardItemClick(ByVal sender As Object, ByVal e As DashboardItemMouseActionEventArgs)
			If Not IsDetailsEnabled(e.DashboardItemName) OrElse doNotShowDataForThisArea Then
				Return
			End If

			Using detailForm As New DetailData(e.GetUnderlyingData())
				detailForm.ShowDialog()
			End Using
		End Sub
		#End Region
	End Class
End Namespace
