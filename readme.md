# How to display detailed data for a clicked Pivot's element

This example shows how to create an extension for the [Dashboard Designer](https://docs.devexpress.com/Dashboard/117006/create-the-designer-and-viewer-applications/winforms-designer) / [Dashboard Viewer](https://docs.devexpress.com/Dashboard/117122/create-the-designer-and-viewer-applications/winforms-viewer) controls that displays detailed data for a clicked pivot element (a data cell or a field value). This extension add the **Display Details** button to the ribbon, that behaves like default interactivity options. So that end users who design dashboards can enable or disable this functionality in the same manner as [Drill-Down](https://docs.devexpress.com/Dashboard/15703/create-dashboards/create-dashboards-in-the-winforms-designer/interactivity/drill-down), [Master Filtering](https://docs.devexpress.com/Dashboard/15702/create-dashboards/create-dashboards-in-the-winforms-designer/interactivity/master-filtering), or other options.

![](images/pivot-detailed-data.png)

The example contains a solution with two projects:

1.	**PivotDetailExtension** – contains the code related to the extension. This project produces a custom class library (_PivotDetailExtension.dll_) that can be referenced and reused in other Designer / Viewer applications.
2.	**DesignerSample** – is a simple dashboard designer application that demonstrates how to use the extension.

This approach has the following limitations: 
1.	When you duplicate the Pivot item, the state of the **Display Details** option is ignored.
2.	The **Display Details** option's changes cannot be reverted by the "Undo" and "Redo" Ribbon buttons. 

Below you find a step-by-step instruction to create the extension from scratch. You can divide the task implementation to several parts:

## Interactivity action code

1. Create a form that displays detailed data from the clicked item. This extension uses [XtraForm](https://docs.devexpress.com/WindowsForms/114560/controls-and-libraries/forms-and-user-controls/xtraform) with [GridControl](https://docs.devexpress.com/WindowsForms/DevExpress.XtraGrid.GridControl).

2. Handle the [DashboardDesigner.DashboardItemClick](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardItemClick) event to get information about the clicked element. Call the [DashboardItemMouseHitTestEventArgs.GetUnderlyingData](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardItemMouseHitTestEventArgs.GetUnderlyingData) method to obtain the element's underlying data and display them in your custom form.

    ```cs
    void DashboardItemClick(object sender, DashboardItemMouseActionEventArgs e) {
        …
        using(DetailData detailForm = new DetailData(e.GetUnderlyingData())) {
            detailForm.ShowDialog();
        }
    }
    ```

3. The Dashboard Designer raises the [DashboardDesigner.DashboardItemClick](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardItemClick) event when an end user clicks the expand / collapse button in a row or column or click an empty area. To skip displaying the details dialog in these case, handle the [DashboardDesigner.DashboardItemControlCreated](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardItemControlCreated) event with the following logic:

    ```cs
    bool doNotShowDataForThisArea = false;
    void PivotGridControl_MouseDown(object sender, MouseEventArgs e) {
        PivotGridControl pivot = sender as PivotGridControl;
        PivotGridHitInfo hi = pivot.CalcHitInfo(e.Location);

        doNotShowDataForThisArea =
            (hi.HitTest == PivotGridHitTest.Value && hi.ValueInfo.ValueHitTest == PivotGridValueHitTest.ExpandButton)
            || (hi.HitTest == PivotGridHitTest.None);
    }
    ```

    The _doNotShowDataForThisArea_ variable determines whether an end user clicked the allowed area.

## Integration to Dashboard Designer

4. To allow end users to enable or disable this functionality for a specific Pivot item when designing dashboard, add a custom button to the DashboardDesigner's [Ribbon toolbar](https://docs.devexpress.com/Dashboard/15732/create-the-designer-and-viewer-applications/winforms-designer/ribbon-bars-and-menu/ribbon-ui).

    Find the Page Group to which you can add your custom option. The following code snippet finds the **Pivot** → **Data** → **Interactivity** group:

    ```cs
    RibbonControl ribbon = targetDesigner.MenuManager as RibbonControl;
    PivotToolsRibbonPageCategory category = ribbon.PageCategories.OfType<PivotToolsRibbonPageCategory>().First();
    DataRibbonPage page = category.Pages.OfType<DataRibbonPage>().First();
    InteractivitySettingsRibbonPageGroup InteractivityGroup = page.Groups.OfType<InteractivitySettingsRibbonPageGroup>().First();
    ```

5. Create your custom button and add it to the found group.

6. Store information about items with enabled custom functionality. This extension uses a simple list of strings to store component names of items. As a variant, you can implement your own storage.

    On the custom button click, add or remove the currently selected dashboard item's name to the storage:

    ```cs
    List<string> pivotItemsWithEnabledDetails;
    private void ShowDetatilsItem_ItemClick(object sender, ItemClickEventArgs e)     {
        if(targetDesigner.SelectedDashboardItem is PivotDashboardItem)         {
            if (isDetailsEnabled(targetDesigner.SelectedDashboardItem.ComponentName))
                pivotItemsWithEnabledDetails.Remove(targetDesigner.SelectedDashboardItem.ComponentName);
            else
                pivotItemsWithEnabledDetails.Add(targetDesigner.SelectedDashboardItem.ComponentName);
        }
        UpdateButtonState();
    }
    ``` 

7. Determine whether the custom functionality is enabled for an item by checking its name in the storage. Add this check to the _DashboardItemClick_ event handler.

    You can aldo update your custom ribbon button's checked sate dynamically depending on the currently selected item. For this, handle the [DashboardDesigner.DashboardItemSelectionChanged](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardItemSelectionChanged) event.
    
    ```cs
    void Designer_DashboardItemSelected(object sender, DashboardItemSelectionChangedEventArgs e) {
        if(targetDesigner.Dashboard.Items[e.DashboardItemName] is PivotDashboardItem) {
            UpdateButtonState();
        }
    }

    void UpdateButtonState() {
        if(targetDesigner.SelectedDashboardItem == null) return;
        showDetatilsButton.Checked = IsDetailsEnabled(targetDesigner.SelectedDashboardItem.ComponentName);
    }
    ```


## Save / load custom information

8. The next step is to save information about items with enabled option to a dashboard file. To add custom information to a dashboard file when saving it, handle the [DashboardDesigner.DashboardSaving](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardSaving) event. The following code snippet serializes the collection of component names to _XDocument_ and passes it to the _UserData_ section intended for storing custom data: 
    ```cs
    private void Designer_DashboardSaving(object sender, DashboardSavingEventArgs e)         {
        XmlSerializer xs = new XmlSerializer(typeof(List<string>));
        XDocument xElement = new XDocument();

        using (XmlWriter xw = xElement.CreateWriter())
            xs.Serialize(xw, pivotItemsWithEnabledDetails);
        e.Dashboard.UserData = xElement.Root;
    }
    ```
9.	To read this data further when opening a dashboard file, handle the [DashboardDesigner.DashboardLoaded](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardLoaded) event and deserialize data from UserData section in a similar manner.
