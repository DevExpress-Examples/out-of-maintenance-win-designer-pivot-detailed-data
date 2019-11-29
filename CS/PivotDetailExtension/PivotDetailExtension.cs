using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using DevExpress.DashboardCommon;
using DevExpress.DashboardWin;
using DevExpress.DashboardWin.Bars;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using DevExpress.XtraPivotGrid;

namespace DesignerSample {
    public class PivotDetailExtension {
        #region Initialization and Registration
        BarCheckItem showDetatilsButton;
        DashboardDesigner targetDesigner;
        DashboardViewer targetViewer;

        public PivotDetailExtension() {
            showDetatilsButton = CreateRibbonButton();
        }

        /// <summary>
        /// Creates the "Display Details" button for the Ribbon
        /// </summary>
        BarCheckItem CreateRibbonButton() {
            BarCheckItem showDetailsItem = new BarCheckItem();
            showDetailsItem.Caption = "Display Details";
            showDetailsItem.ImageOptions.SvgImage = global::PivotDetailExtension.Properties.Resources.Detailed;
            showDetailsItem.ItemClick += showDetailsItem_ItemClick;
            return showDetailsItem;
        }

        /// <summary>
        /// Attaches the Extension to DashboardViewer
        /// </summary>
        public void Attach(DashboardViewer viewer) {
            Detach();
            targetViewer = viewer;
            pivotItemsWithEnabledDetails = new List<string>();
            // Handle Events
            targetViewer.DashboardItemClick += DashboardItemClick;
            targetViewer.DashboardItemControlCreated += DashboardItemControlCreated;
            targetViewer.DashboardLoaded += DashboardLoaded;
        }

        /// <summary>
        /// Attaches the Extension to DashboardDesigner
        /// </summary>
        public void Attach(DashboardDesigner designer) {
            Detach();
            targetDesigner = designer;
            pivotItemsWithEnabledDetails = new List<string>();
            //Add Ribbon Toolbar button
            RibbonControl ribbon = targetDesigner.MenuManager as RibbonControl;
            PivotToolsRibbonPageCategory category = ribbon.PageCategories.OfType<PivotToolsRibbonPageCategory>().First();
            DataRibbonPage page = category.Pages.OfType<DataRibbonPage>().First();
            InteractivitySettingsRibbonPageGroup InteractivityGroup = page.Groups.OfType<InteractivitySettingsRibbonPageGroup>().First();
            InteractivityGroup.ItemLinks.Add(showDetatilsButton);
            // Handle Events
            targetDesigner.DashboardItemClick += DashboardItemClick;
            targetDesigner.DashboardItemControlCreated += DashboardItemControlCreated;
            targetDesigner.DashboardLoaded += DashboardLoaded;
            targetDesigner.DashboardSaving += Designer_DashboardSaving;
            targetDesigner.DashboardItemSelectionChanged += Designer_DashboardItemSelected;
            targetDesigner.Dashboard.ItemCollectionChanged += Dashboard_ItemCollectionChanged;
        }

        /// <summary>
        /// Detaches the Extension from the control
        /// </summary>
        public void Detach() {
            if(targetDesigner != null) {
                RibbonControl ribbon = targetDesigner.MenuManager as RibbonControl;
                PivotToolsRibbonPageCategory category = ribbon.PageCategories.OfType<PivotToolsRibbonPageCategory>().First();
                DataRibbonPage page = category.Pages.OfType<DataRibbonPage>().First();
                InteractivitySettingsRibbonPageGroup InteractivityGroup = page.Groups.OfType<InteractivitySettingsRibbonPageGroup>().First();
                InteractivityGroup.ItemLinks.Remove(showDetatilsButton);

                targetDesigner.DashboardItemClick -= DashboardItemClick;
                targetDesigner.DashboardItemControlCreated -= DashboardItemControlCreated;
                targetDesigner.DashboardSaving -= Designer_DashboardSaving;
                targetDesigner.DashboardLoaded -= DashboardLoaded;
                targetDesigner.DashboardItemSelectionChanged -= Designer_DashboardItemSelected;
                targetDesigner.Dashboard.ItemCollectionChanged -= Dashboard_ItemCollectionChanged;
            }
            if(targetViewer != null) {
                targetViewer.DashboardItemClick -= DashboardItemClick;
                targetViewer.DashboardItemControlCreated -= DashboardItemControlCreated;
                targetViewer.DashboardLoaded += DashboardLoaded;
            }
        }
        #endregion

        #region Designer Business Logic
        /// <summary>
        /// The "Dispaly Details" button's click handler. Enables / Disables the custom functionality.
        /// </summary>
        void showDetailsItem_ItemClick(object sender, ItemClickEventArgs e) {
            if(targetDesigner.SelectedDashboardItem is PivotDashboardItem) {
                if(IsDetailsEnabled(targetDesigner.SelectedDashboardItem.ComponentName))
                    pivotItemsWithEnabledDetails.Remove(targetDesigner.SelectedDashboardItem.ComponentName);
                else
                    pivotItemsWithEnabledDetails.Add(targetDesigner.SelectedDashboardItem.ComponentName);
            }
            UpdateButtonState();
        }

        /// <summary>
        /// Set the Checked/Unchecked state of the "Dispaly Details" button based on the currently selected item.
        /// </summary>
        void UpdateButtonState() {
            if(targetDesigner.SelectedDashboardItem == null) return;
            showDetatilsButton.Checked = IsDetailsEnabled(targetDesigner.SelectedDashboardItem.ComponentName);
        }

        /// <summary>
        /// Invokes update of the "Dispaly Details" button's state when selecting another item.
        /// </summary>
        void Designer_DashboardItemSelected(object sender, DashboardItemSelectionChangedEventArgs e) {
            if(targetDesigner.Dashboard.Items[e.DashboardItemName] is PivotDashboardItem) {
                UpdateButtonState();
            }
        }

        /// <summary>
        /// Saves information about PivotGrids with enabled custom option to the dashboard *.xml file (UserData section).
        /// </summary>
        void Designer_DashboardSaving(object sender, DashboardSavingEventArgs e) {
            XmlSerializer xs = new XmlSerializer(typeof(List<string>));
            XDocument xElement = new XDocument();

            using(XmlWriter xw = xElement.CreateWriter())
                xs.Serialize(xw, pivotItemsWithEnabledDetails);
            e.Dashboard.UserData = xElement.Root;
        }

        /// <summary>
        /// Updates information about PivotGrids with enabled custom option after a Pivot Item has been deleted.
        /// </summary>
        void Dashboard_ItemCollectionChanged(object sender, DevExpress.DataAccess.NotifyingCollectionChangedEventArgs<DashboardItem> e) {
            foreach(PivotDashboardItem pivot in e.RemovedItems.OfType<PivotDashboardItem>())
                if(pivotItemsWithEnabledDetails.Contains(pivot.ComponentName))
                    pivotItemsWithEnabledDetails.Remove(pivot.ComponentName);
        }

        #endregion

        #region Business Logic Common for Designer and Viewer

        /// <summary>
        /// Contains ComponentNames of Pivot Items with the enabled custom option
        /// </summary>
        List<string> pivotItemsWithEnabledDetails;

        /// <summary>
        /// Loads information about Pivot Items with the enabled custom option after a Dashboard has been loaded
        /// </summary>
        void DashboardLoaded(object sender, DashboardLoadedEventArgs e) {
            e.Dashboard.ItemCollectionChanged += Dashboard_ItemCollectionChanged;
            pivotItemsWithEnabledDetails = new List<string>();

            if(e.Dashboard.UserData != null) {
                XmlSerializer xs = new XmlSerializer(typeof(List<string>));
                using(XmlReader xr = e.Dashboard.UserData.CreateReader()) {
                    if(xs.CanDeserialize(xr))
                        pivotItemsWithEnabledDetails = xs.Deserialize(xr) as List<string>;
                }
            }
            UpdateButtonState();

        }

        /// <summary>
        /// Returns a value indicating whether the custom option is enabled for a specific Pivot Item.  
        /// </summary>
        bool IsDetailsEnabled(string componentName) {
            return pivotItemsWithEnabledDetails.Contains(componentName);
        }

        #endregion

        #region Action Business Logic

        /// <summary>
        /// Returns a value indicating if the custom logic should be skipped for the current area (empty area and collapse/expand button).
        /// </summary>
        bool doNotShowDataForThisArea = false;

        /// <summary>
        /// Used to handle the underlying Pivot Gid Control's MouseDown event to determine the clicked area
        /// </summary>
        void DashboardItemControlCreated(object sender, DashboardItemControlEventArgs e) {
            if(e.PivotGridControl != null)
                e.PivotGridControl.MouseDown += PivotGridControl_MouseDown;
        }

        /// <summary>
        /// Used to determine the clicked area and skip DashboardItemClick processing if the Expand/Collapse button is pressed
        /// </summary>
        void PivotGridControl_MouseDown(object sender, MouseEventArgs e) {
            PivotGridControl pivot = sender as PivotGridControl;
            PivotGridHitInfo hi = pivot.CalcHitInfo(e.Location);

            doNotShowDataForThisArea =
                (hi.HitTest == PivotGridHitTest.Value && hi.ValueInfo.ValueHitTest == PivotGridValueHitTest.ExpandButton)
                || (hi.HitTest == PivotGridHitTest.None);
        }

        /// <summary>
        /// Used to get underlying data and display the DetailData dialog
        /// </summary>
        void DashboardItemClick(object sender, DashboardItemMouseActionEventArgs e) {
            if(!IsDetailsEnabled(e.DashboardItemName) || doNotShowDataForThisArea) return;

            using(DetailData detailForm = new DetailData(e.GetUnderlyingData())) {
                detailForm.ShowDialog();
            }
        }
        #endregion
    }
}
