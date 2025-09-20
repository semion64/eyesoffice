using _SSU_EyesOffice.Logic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace _SSU_EyesOffice.Forms
{
    public partial class PlotXY : Form
    {
        public static void SavePlotXYToFile(Record record, string file, int width, int height)
        {
            Chart chart = new Chart();
            
            chart.Width = width;
            chart.Height = height;
            chart.Series.Add(new Series());
            chart.Series.Add(new Series());
            chart.ChartAreas.Add(new ChartArea());

            chart.Series[0].ChartType = SeriesChartType.Line;
            chart.Series[1].ChartType = SeriesChartType.Line;
            chart.Series[0].Color = Color.Red;
            chart.Series[1].Color = Color.Blue;

            var centerLeftY = record.CenterLeftY();
            var centerRightY = record.CenterRightY();

            double minY = 100000000;
            foreach(var item in record.data)
            {
                if ((item.left.Y - centerLeftY) < minY && item.left.Y > 0)
                {
                    minY = item.left.Y - centerLeftY;
                }

                if ((item.right.Y - centerRightY) < minY && item.right.Y > 0)
                {
                    minY = item.right.Y - centerRightY;
                }
            }

            chart.ChartAreas[0].AxisY.Minimum = Math.Round(minY, 2);

            var centerLeftX = record.CenterLeftX();
            var centerRightX = record.CenterRightX();

            chart.ChartAreas[0].AxisX.Title = "X, мм";
            chart.ChartAreas[0].AxisY.Title = "Y, мм";
            
            foreach(var item in record.data)
            {
                chart.Series[0].Points.AddXY(Math.Round(item.left.X - centerLeftX, 2), Math.Round(item.left.Y - centerLeftY, 2));
                chart.Series[1].Points.AddXY(Math.Round(item.right.X - centerRightX, 2), Math.Round(item.right.Y - centerRightY, 2));
            }

            chart.SaveImage(file, ChartImageFormat.Png);
        }

        public PlotXY(Record record)
        {
            InitializeComponent();

            var centerLeftY = record.CenterLeftY();
            var centerRightY = record.CenterRightY();

            double minY = 100000000;
            for (int i = 0; i < record.Length; i++)
            {
                if ((record[i].left.Y - centerLeftY) < minY && record[i].left.Y > 0)
                {
                    minY = record[i].left.Y - centerLeftY;
                }

                if ((record[i].right.Y - centerRightY) < minY && record[i].right.Y > 0)
                {
                    minY = record[i].right.Y - centerRightY;
                }
            }
            
            chart1.ChartAreas[0].AxisY.Minimum = Math.Round(minY, 2);

            var centerLeftX = record.CenterLeftX();
            var centerRightX = record.CenterRightX();

            chart1.ChartAreas[0].AxisX.Title = "X, мм";
            chart1.ChartAreas[0].AxisY.Title = "Y, мм";

            for (int i =0; i < record.Length; i++)
            {
                chart1.Series[0].Points.AddXY(Math.Round(record[i].left.X - centerLeftX, 2), Math.Round(record[i].left.Y - centerLeftY, 2));
                chart1.Series[1].Points.AddXY(Math.Round(record[i].right.X - centerRightX, 2), Math.Round(record[i].right.Y - centerRightY, 2));
            }
        }

        private void PlotXY_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Png files (*.png)|*.png";
            sfd.FilterIndex = 1;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                this.chart1.SaveImage(sfd.FileName, System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Png);
            }
        }
    }
}
