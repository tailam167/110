using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace QuanLyHocSinh
{
    public partial class frmQuanLyHocSinh : Form
    {
        public frmQuanLyHocSinh()
        {
            InitializeComponent();
            ketnoi();
            
           // cbChon.SelectedIndex = 0;
        }//LOAD SAU KHI TAO 
        private void LoadAfterConvert()
        {
            double[][] matrix = new double[dtgvDanhsachdiem.Rows.Count - 1][];
            CovertDataInMatrix(matrix);
            int[] clustering = Cluster(matrix, Convert.ToInt32(cbChon.SelectedItem.ToString()));
            Console.WriteLine("Phân nhóm cuối cùng ở dạng nội bộ:\n");
        
            ShowClustered(matrix, clustering, Convert.ToInt32(cbChon.SelectedItem.ToString()), 1);
        }
        private void ketnoi()
        {
            try
            {
                SqlConnection kn = new SqlConnection(@"Data Source=TSWO\SQLEXPRESS;Initial Catalog=QuanlyHocSinh2017;Integrated Security=True");
                kn.Open();
                string sql = "SELECT *FROM dbo.bangdiem";
                SqlCommand commandsql = new SqlCommand(sql, kn);
                SqlDataAdapter com = new SqlDataAdapter(commandsql);
                DataTable table = new DataTable();
                com.Fill(table);
                dtgvDanhsachdiem.DataSource = table;
            }
            catch
            {
                MessageBox.Show("loi ket noi vui long kiem tra lai!");
            }
            finally
            {
                SqlConnection kn = new SqlConnection(@"Data Source=TSWO\SQLEXPRESS;Initial Catalog=QuanlyHocSinh;Integrated Security=True");
                kn.Close();
            }
        }

        #region Convert
        //CUM
        public int[] Cluster(double[][] rawData, int numClusters)
        {

            double[][] data = Normalized(rawData);
            bool changed = true;//đã có một sự thay đổi trong ít nhất một phân công cluster

            bool success = true;//có thể được tính toán tất cả các phương tiện (không có cụm số không)

            int[] clustering = InitClustering(data.Length, numClusters, 0);//khởi tạo ngẫu nhiên
            double[][] means = Allocate(numClusters, data[0].Length);// ngan nhat

            int maxCount = data.Length * 10;//ktra du lieu
            int ct = 0;
            while (changed == true && success == true && ct < maxCount)
            {
                ++ct;//k-means thường hội tụ rất nhanh
                success = UpdateMeans(data, clustering, means);
                //tính cluster mới có nghĩa là nếu có thể. không có hiệu lực nếu thất bại
                changed = UpdateClustering(data, clustering, means);
                //(lại) gán tuples cho các cụm. không có hiệu lực nếu thất bại
            }

            return clustering;
        }
        //CHUAN HOA
        private double[][] Normalized(double[][] rawData)
        {
            //tạo một bản sao của dữ liệu đầu vào
            double[][] result = new double[rawData.Length - 1][];
            for (int i = 0; i < rawData.Length - 1; ++i)
            {
                result[i] = new double[rawData[i].Length];
                Array.Copy(rawData[i], result[i], rawData[i].Length);
            }

            for (int j = 0; j < result[0].Length; ++j) // moi cot
            {
                double colSum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    colSum += result[i][j];
                double mean = (colSum / result.Length);
                double sum = 0.0;
                for (int i = 0; i < result.Length; ++i)
                    sum += (result[i][j] - mean) * (result[i][j] - mean);
                double sd = sum / result.Length;
                for (int i = 0; i < result.Length; ++i)
                    result[i][j] = (result[i][j] - mean) / sd;
            }
            return result;
        }
        //CUM TRONG NO
        private int[] InitClustering(int numTuples, int numClusters, int randomSeed)
        {

            Random random = new Random(randomSeed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i)//đảm bảo mỗi cụm có ít nhất một bộ
                clustering[i] = i;
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters);
            return clustering;
        }
        //CHI DINH
        private double[][] Allocate(int numClusters, int numColumns)
        {
           // bộ chi dinh ma trận thuận tiện cho Cluster
            double[][] result = new double[numClusters][];
            for (int k = 0; k < numClusters; ++k)
                result[k] = new double[numColumns];
            return result;
        }
        //CAI DAI LAI MEANS
        private bool UpdateMeans(double[][] data, int[] clustering, double[][] means)
        {

            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false;//clustering false ko thay doi means

            //cập nhật, zero-out có nghĩa là nó có thể được sử dụng như ma trận đầu
            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] = 0.0;

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data[i].Length; ++j)
                    means[cluster][j] += data[i][j];//tích lũy tổng
            }

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length; ++j)
                    means[k][j] /= clusterCounts[k];
            return true;
        }
        //CAI DAT LAI TAP HOP CUM
        private bool UpdateClustering(double[][] data, int[] clustering, double[][] means)
        {
            //gán mỗi bộ để một cụm(gần nhất nghĩa)
            // trả về false nếu không có sự phân công quyền nào thay đổi HO ORC
            // nếu việc phân bổ lại sẽ dẫn đến việc phân cụm nơi
            // một hoặc nhiều cụm không có bộ.

            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length];//kết quả đề xuất
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters];// khoảng cách từ hàng tuple tới mỗi trung bình

            for (int i = 0; i < data.Length; ++i)//di qua tung  tuple
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = Distance(data[i], means[k]);

                int newClusterID = MinIndex(distances);
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID; // update
                }
            }

            if (changed == false)
                return false;//không thay đổi để bảo lãnh và không cập nhật clustering [] []

            // kiểm tra clustering được đề xuất [] cluster counts
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false;

            Array.Copy(newClustering, clustering, newClustering.Length); // update
            return true; //nhóm tốt và ít nhất một sự thay đổi
        }
        //KHOANG CACH
        private double Distance(double[] tuple, double[] mean)
        {
            //Khoảng cách Euclide giữa hai vectơ cho UpdateClustering ()
           // xem xét các lựa chọn thay thế như Manhattan khoảng cách

            double sumSquaredDiffs = 0.0;
            for (int j = 0; j < tuple.Length; ++j)
                sumSquaredDiffs += Math.Pow((tuple[j] - mean[j]), 2);
            return Math.Sqrt(sumSquaredDiffs);
        }
        //CHI SO KC MIN
        private int MinIndex(double[] distances)
        {
            //chỉ số giá trị nhỏ nhất trong mảng
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }

        //HIEN THI CUM
        void ShowClustered(double[][] data, int[] clustering, int numClusters, int decimals)
        {
            #region Cách 1
            StringBuilder sr = new StringBuilder();
            sr.AppendLine("   BAN CHON VOI SO CUM : " + numClusters);
            sr.AppendLine("MaHs -Toan-Ly -Van-Hoa- Su-Sinh-Anh-CNghe-Dia ");
            for (int k = 0; k < numClusters; ++k)
            {
                
                sr.AppendLine("=======================================");
                for (int i = 0; i < data.Length - 1; ++i)
                {
                    int clusterID = clustering[i];
                    if (clusterID != k) continue;
                    //sr.Append(i.ToString().PadLeft(3) + " ");
                    for (int j = 0; j < data[i].Length; ++j)
                    {
                        if (data[i][j] >= 0.0) sr.Append ("  ");
                        sr.Append(data[i][j] + " |");
                    }
                    sr.AppendLine("   ");
                   
                }
                sr.AppendLine("=======================================");
                sr.AppendLine("=============CUM TIEP THEO=============");
                
                
            }
                 sr.AppendLine("============= KET THUC !!!=============");
            txtPhanNhom.Text = sr.ToString();
            #endregion
        }
        
        #endregion
        //TAO DU KIEU MA TRAN AO
        void CovertDataInMatrix(double[][] matrix)
        {
            for (int i = 0; i < dtgvDanhsachdiem.RowCount - 1; i++) // i la hang`
            {
                for (int j = 0; j < dtgvDanhsachdiem.ColumnCount; j++) // j la cot
                {
                    if (j != 1 && j != 2)
                        matrix[i] = new double[] {

                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[0].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[2].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[3].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[4].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[5].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[6].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[7].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[8].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[9].Value.ToString()),
                            double.Parse(dtgvDanhsachdiem.Rows[i].Cells[10].Value.ToString()),
                            
                };

                }
            }
        }
        private void dtgvDanhsachdiem_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        //CLICK CHUOT VAO DTGV
        int index;
        private void dtgvDanhsachdiem_Click(object sender, EventArgs e)
        {
            index = dtgvDanhsachdiem.CurrentRow.Index;
            tbtMahocsinh.Text = dtgvDanhsachdiem.Rows[index].Cells[0].Value.ToString();
            tbtLop.Text = dtgvDanhsachdiem.Rows[index].Cells[1].Value.ToString();
            tbtSinh.Text = dtgvDanhsachdiem.Rows[index].Cells[2].Value.ToString();
            tbtToan.Text = dtgvDanhsachdiem.Rows[index].Cells[3].Value.ToString();
            tbtVan.Text = dtgvDanhsachdiem.Rows[index].Cells[4].Value.ToString();
            tbtĐia.Text = dtgvDanhsachdiem.Rows[index].Cells[5].Value.ToString();
            tbtSu.Text = dtgvDanhsachdiem.Rows[index].Cells[6].Value.ToString();
            tbtLy.Text = dtgvDanhsachdiem.Rows[index].Cells[7].Value.ToString();
            tbtHoa.Text = dtgvDanhsachdiem.Rows[index].Cells[8].Value.ToString();
            tbtCnghe.Text = dtgvDanhsachdiem.Rows[index].Cells[9].Value.ToString();
            tbtAnh.Text = dtgvDanhsachdiem.Rows[index].Cells[10].Value.ToString();
            
        }
      
        private void btnXem_Click(object sender, EventArgs e)
        {
            LoadAfterConvert();
        }
       
       
        private void btnThoat_Click(object sender, EventArgs e)
        {

            DialogResult tl = MessageBox.Show("ban co chac thoat ko!!!", "thông báo",

         MessageBoxButtons.YesNo,
         MessageBoxIcon.Question);
            if (tl == DialogResult.Yes)
                Application.Exit();
            else
                cbChon.Focus();
        }

        private void cbChon_SelectedIndexChanged(object sender, EventArgs e)
        {
            double[][] matrix = new double[dtgvDanhsachdiem.RowCount][];
            CovertDataInMatrix(matrix);
            int[] clustering = Cluster(matrix, Convert.ToInt32(cbChon.SelectedItem.ToString()));
            ShowClustered(matrix, clustering, Convert.ToInt32(cbChon.SelectedItem.ToString()), 1);
        }

        private void tbtMahocsinh_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbtHoa_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtPhanNhom_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void frmQuanLyHocSinh_Load(object sender, EventArgs e)
        {

        }

        private void tbtLop_TextChanged(object sender, EventArgs e)
        {

        }

        private void tbtHoa_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void tbtĐia_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
