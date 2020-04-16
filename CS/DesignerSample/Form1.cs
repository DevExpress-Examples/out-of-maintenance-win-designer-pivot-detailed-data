using DevExpress.XtraEditors;
using PivotExtension;

namespace DesignerSample {
    public partial class Form1 : XtraForm {
        public Form1() {
            InitializeComponent();
            dashboardDesigner1.CreateRibbon();

            // Extension Registration code. 
            // Execute it after the Ribbon has been created and before loading a dashboard.
            PivotDetailExtension extension = new PivotDetailExtension();
            extension.Attach(dashboardDesigner1);
            dashboardDesigner1.LoadDashboard("nwind.xml");
        }
    }
}
