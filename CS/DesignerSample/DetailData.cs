using DevExpress.DashboardCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesignerSample
{
    public partial class DetailData : Form
    {
        public DetailData()
        {
            InitializeComponent();
        }

        public DetailData(DashboardUnderlyingDataSet data)
        {
            InitializeComponent();
            gridControl1.DataSource = data;
            gridView1.PopulateColumns();
        }
    }
}
