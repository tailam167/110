using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QuanLyHocSinh
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void txtDangnhap_TextChanged(object sender, EventArgs e)
        {

            
        }

        private void btnThoat_Click(object sender, EventArgs e)
        {

            DialogResult tl = MessageBox.Show("co chăc ko vay!!!", "thông báo",

         MessageBoxButtons.YesNo,
         MessageBoxIcon.Question);
            if (tl == DialogResult.Yes)
                Application.Exit();
            else
                txtDangnhap.Focus();
        }

        private void btnDangnhap_Click(object sender, EventArgs e)
        {
            if (txtDangnhap.Text.Trim().Equals("long") &&
                      txtMatkhau.Text.Trim().Equals("123"))
            {
                MessageBox.Show("Đăng nhap thanh cong!!", "thong bao",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Information);
                this.Close();
            }
            else
            {
                MessageBox.Show("Đăng nhap that bại!!", "thong bao",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Error);
                txtDangnhap.Focus();
            }
        }
    }
}
