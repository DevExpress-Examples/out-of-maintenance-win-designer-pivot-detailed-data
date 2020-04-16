using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DevExpress.DashboardCommon;
using DevExpress.DashboardWin;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraPivotGrid;

namespace PivotExtension
{
    public class PivotDetailExtension {
        const string PropertyName = "PivotDetailExtension";

        BarCheckItem showDetatilsBarButton;
        IDashboardControl dashboardControl;
        DashboardDesigner dashboardDesigner
        {
            get { return dashboardControl as DashboardDesigner; }
        }

        #region Initialization and Registration

        /// <summary>
        /// Creates the "Display Details" button for the Ribbon
        /// </summary>
        BarCheckItem CreateRibbonButton() {
            BarCheckItem showDetailsItem = new BarCheckItem();
            showDetailsItem.Caption = "Display Details";
            showDetailsItem.ImageOptions.SvgImage = global::PivotExtension.Properties.Resources.Detailed;
            showDetailsItem.ItemClick += showDetailsItem_ItemClick;
            return showDetailsItem;
        }

        /// <summary>
        /// Attaches the Extension to DashboardViewer or DashboardDesigner
        /// </summary>
        public void Attach(IDashboardControl dashboardControl) {
            Detach();
            this.dashboardControl = dashboardControl;
            // Handle Events
            this.dashboardControl.DashboardItemClick += DashboardItemClick;
            if(dashboardDesigner!= null)
            {
                AddButtonToRibbon();
                dashboardDesigner.DashboardCustomPropertyChanged += TargetDesigner_DashboardCustomPropertyChanged;
                dashboardDesigner.DashboardItemSelected += DashboardItemSelected;

            }
        }

        /// <summary>
        /// Adds the "Display Details" button to DashboardDesigner's Ribbon
        /// </summary>
        void AddButtonToRibbon()
        {
            RibbonControl ribbon = dashboardDesigner.Ribbon;
            RibbonPage page = ribbon.GetDashboardRibbonPage(DashboardBarItemCategory.PivotTools, DashboardRibbonPage.Data);
            RibbonPageGroup group = page.Groups.OfType<DevExpress.DashboardWin.Bars.InteractivitySettingsRibbonPageGroup>().First();
            showDetatilsBarButton = CreateRibbonButton();
            group.ItemLinks.Add(showDetatilsBarButton);
        }

        /// <summary>
        /// Removes the "Display Details" button from DashboardDesigner's Ribbon
        /// </summary>
        void RemoveButtonFromRibbon()
        {
            RibbonControl ribbon = dashboardDesigner.Ribbon;
            ribbon.Items.Remove(showDetatilsBarButton);
        }


        /// <summary>
        /// Detaches the Extension from the control
        /// </summary>
        public void Detach() {
            if (dashboardControl == null) return;
            if(dashboardDesigner != null) {
                RemoveButtonFromRibbon();
                dashboardDesigner.DashboardCustomPropertyChanged -= TargetDesigner_DashboardCustomPropertyChanged;
                dashboardDesigner.DashboardItemSelected -= DashboardItemSelected;
            }
            dashboardControl.DashboardItemClick -= DashboardItemClick;
            dashboardControl = null;
        }
        #endregion

        #region Designer Business Logic

        /// <summary>
        /// Updates the "Dispaly Details" button's state after the custom prioperty has been enabled / disabled. 
        /// </summary>
        private void TargetDesigner_DashboardCustomPropertyChanged(object sender, CustomPropertyChangedEventArgs e)
        {
            UpdateButtonState();
        }
        /// <summary>
        /// The "Dispaly Details" button's click handler. Enables / Disables the custom functionality.
        /// </summary>
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

        /// <summary>
        /// Set the Checked/Unchecked state of the "Dispaly Details" button based on the currently selected item.
        /// </summary>
        void UpdateButtonState() {
            if(dashboardDesigner.SelectedDashboardItem == null) return;
            showDetatilsBarButton.Checked = IsDetailsEnabled(dashboardDesigner.SelectedDashboardItem);
        }

        /// <summary>
        /// Invokes update of the "Dispaly Details" button's state when selecting another item.
        /// </summary>
        void DashboardItemSelected(object sender, DashboardItemSelectedEventArgs e) {
            if(e.SelectedDashboardItem is PivotDashboardItem) {
                UpdateButtonState();
            }
        }

        #endregion

        #region Business Logic Common for Designer and Viewer

        /// <summary>
        /// Used to get underlying data and display the DetailData dialog
        /// </summary>
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

        /// <summary>
        /// Returns a value indicating whether the custom option is enabled for a specific Pivot Item.  
        /// </summary>
        bool IsDetailsEnabled(DashboardItem item) {
            return Convert.ToBoolean(item.CustomProperties.GetValue(PropertyName));
        }

        /// <summary>
        /// Returns a value indicating whether the custom option is enabled for a Pivot Item with a specific component name.  
        /// </summary>
        bool IsDetailsEnabled(string itemName)
        {
            return IsDetailsEnabled(dashboardDesigner.Dashboard.Items[itemName]);
        }
        #endregion
    }
}
