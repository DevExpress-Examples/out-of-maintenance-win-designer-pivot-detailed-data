using System;
using System.Linq;
using System.Windows.Forms;
using DevExpress.DashboardCommon;
using DevExpress.XtraEditors;

namespace PivotExtension
{
    public partial class DetailData : XtraForm {
        public DetailData() {
            InitializeComponent();
        }
        public DetailData(DashboardUnderlyingDataSet data) {
            InitializeComponent();
            gridControl1.DataSource = data;
            gridView1.PopulateColumns();
        }
    }
}
