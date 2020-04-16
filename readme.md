# How to display detailed data for a clicked Pivot's element

This example shows how to create an extension for the [Dashboard Designer](https://docs.devexpress.com/Dashboard/117006) / [Dashboard Viewer](https://docs.devexpress.com/Dashboard/117122) controls that displays detailed data for a clicked pivot element (a data cell or a field value). This extension adds the **Display Details** button to the ribbon. End users who design dashboards can enable or disable this functionality in the same manner as [Drill-Down](https://docs.devexpress.com/Dashboard/15703), [Master Filtering](https://docs.devexpress.com/Dashboard/15702), or other options.

The detailed data is displayed in a new form ([XtraForm](https://docs.devexpress.com/WindowsForms/114560) with [GridControl](https://docs.devexpress.com/WindowsForms/DevExpress.XtraGrid.GridControl)). You can customize the form and grid in your own ways. For example, you can change the form's [icon](https://docs.devexpress.com/WindowsForms/DevExpress.XtraEditors.XtraForm.IconOptions) or apply one of the [DevExpress skins](https://docs.devexpress.com/WindowsForms/2399/build-an-application/skins).

![](images/pivot-detailed-data.png)

The example contains a solution with two projects:

1.	**PivotDetailExtension** – contains the code related to the extension. This project produces a custom class library (_PivotDetailExtension.dll_) that can be referenced and reused in other Designer / Viewer applications.
2.	**DesignerSample** – is a simple dashboard designer application that demonstrates how to use the extension.


Below you find a step-by-step instruction that shows to create the extension from scratch. You can divide the task implementation to several parts:

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

3. The Dashboard Designer raises the [DashboardDesigner.DashboardItemClick](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardItemClick) event when an end user clicks the expand / collapse button in a row or column or click an empty area. To skip displaying the details dialog in these case, get information about the grid elements located at the clicked point via the [PivotGridControl.CalcHitInfo(Point)](https://docs.devexpress.com/WindowsForms/DevExpress.XtraPivotGrid.PivotGridControl.CalcHitInfo(System.Drawing.Point)) method and check if it is a valid area. Update the DashboardItemClick event with the following logic:

    ```cs
    void DashboardItemClick(object sender, DashboardItemMouseActionEventArgs e) {
            
            if (IsDetailsEnabled(e.DashboardItemName))
            {
                PivotGridControl pivot = dashboardDesigner.GetUnderlyingControl(e.DashboardItemName) as PivotGridControl;
                PivotGridHitInfo hi = pivot.CalcHitInfo(pivot.PointToClient(Cursor.Position));

                bool doNotShowDataForThisArea =
                    (hi.HitTest == PivotGridHitTest.Value && hi.ValueInfo.ValueHitTest == PivotGridValueHitTest.ExpandButton)
                    || (hi.HitTest == PivotGridHitTest.None);
                if (!doNotShowDataForThisArea)
                    using (DetailData detailForm = new DetailData(e.GetUnderlyingData()))
                    {
                        detailForm.ShowDialog();
                    }
            }
        }
    ```

    The _doNotShowDataForThisArea_ variable determines whether an end user clicked the allowed area.

## Integration to Dashboard Designer

4. To allow end users to enable or disable this functionality for a specific Pivot item when designing dashboard, add a custom button to the DashboardDesigner's [Ribbon toolbar](https://docs.devexpress.com/Dashboard/15732/create-the-designer-and-viewer-applications/winforms-designer/ribbon-bars-and-menu/ribbon-ui).

    Find the Page Group to which you can add your custom option. The following code snippet finds the **Pivot** → **Data** → **Interactivity** group:

    ```cs
            RibbonControl ribbon = dashboardDesigner.Ribbon;
            RibbonPage page = ribbon.GetDashboardRibbonPage(DashboardBarItemCategory.PivotTools, DashboardRibbonPage.Data);
            RibbonPageGroup group = page.Groups.OfType<DevExpress.DashboardWin.Bars.InteractivitySettingsRibbonPageGroup>().First();
    ```

5. Create your custom button and add it to the found group.
     ```cs
            showDetatilsBarButton = CreateRibbonButton();
            group.ItemLinks.Add(showDetatilsBarButton);
    ```
    
6. Store information wheter the custom option enabled or disabled. This extension strore this information in items' custom properties.

    On the custom button click, createa a new instance of the CustomPropertyHistoryItem object, specify its setting and pass it to the DashboardDesigner.AddToHistory method:

    ```cs
    void showDetailsItem_ItemClick(object sender, ItemClickEventArgs e) {
            if (dashboardDesigner.SelectedDashboardItem is PivotDashboardItem)
            {
                bool newValue = !IsDetailsEnabled(dashboardDesigner.SelectedDashboardItem);
                string status = IsDetailsEnabled(dashboardDesigner.SelectedDashboardItem) ? "Enaled" : "Disabled";
                var historyItem = new CustomPropertyHistoryItem(
                    dashboardDesigner.SelectedDashboardItem, //Item that property should be changed
                    PropertyName,               //the name of the custom propeprty that should be changed
                    newValue.ToString(),        //new value of the custom propeprty
                    "Detail Popup " + status);  //text displayed in the Undo/Redo button's tooltip
                dashboardDesigner.AddToHistory(historyItem);
            }
        }
    ``` 
    
7. After you added a history item you need to update your custom ribbon button's checked state. for this handle the DashbaordDesigner.DashboardCustomPropertyChanged event:
    ```cs 
        private void TargetDesigner_DashboardCustomPropertyChanged(object sender, CustomPropertyChangedEventArgs e)
        {
            UpdateButtonState();
        }

        void UpdateButtonState() {
            if(dashboardDesigner.SelectedDashboardItem == null) return;
            showDetatilsBarButton.Checked = IsDetailsEnabled(dashboardDesigner.SelectedDashboardItem);
        }

    ``` 

8. To determine whether the custom functionality is enabled for an item check its CustomProperties.
    ```cs
        bool IsDetailsEnabled(DashboardItem item) {
            return Convert.ToBoolean(item.CustomProperties.GetValue(PropertyName));
        }
    ``` 

9. Update your custom ribbon button's checked state dynamically depending on the currently selected item. For this, handle the [DashboardDesigner.DashboardItemSelectionChanged](https://docs.devexpress.com/Dashboard/DevExpress.DashboardWin.DashboardDesigner.DashboardItemSelectionChanged) event.
    
    ```cs
    void Designer_DashboardItemSelected(object sender, DashboardItemSelectionChangedEventArgs e) {
        if(targetDesigner.Dashboard.Items[e.DashboardItemName] is PivotDashboardItem) {
            UpdateButtonState();
        }
    }
    ```
    