namespace _SSU_EyesOffice.Forms
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using _SSU_EyesOffice.Logic;
    using Emgu.CV.Structure;
    using Emgu.CV;

    enum Algorithm
    {
        Binarization,
        Gradient,
        BinarizationExpand
    };

    enum Action
    {
        None,
        RegionLeft,
        RegionRight,
        Calibration
    };

    public partial class FormDetect : Form
    {
        private int currentStep = 0;

        private Algorithm algorithm;

        string[] stepTitles = { "Алгоритм распознования", "Область глаз и калибровка", "Преобразование изображения", "Детектирование" };

        private Action action = Action.None;
        bool isMouseDown = false;
        Grabber grabber = new Grabber();
        Record record;

        List<DataStruct> recordData = new List<DataStruct>();

        Rectangle roiLeft, roiRight;
        Point calibPt1, calibPt2;
        int xStart, yStart;
        private double calibFactor = 0.0;
        int movieLength = 0;
        bool isNowDetect = false;
        bool end = false;
        float centerLeft = 0, centerRight = 0, centerLeftY=0, centerRightY=0; //для смещения одного графика отн. другого
        int cameraIndex = -1;
        int timeInterval;
        DateTime startTime;
        int patientId = -1;
        string filePath = "";
        private IntPtr writerVideo;
        private float maxX, minX = 0;

        public FormDetect(Record record = null, int cameraIndex = -1, int patientId = -1)
        {
            this.record = record;
            InitializeComponent();

            this.Show();
            this.UpdateStep();
            
            if (record != null)
            {
                try
                {

                    grabber.SetCapture(record.Path);
                }
                catch (Exception ex)
                {
                    this.Close();
                    Logging.Log(ex, $"FormDetect(); Couldn't set capture for file {record?.Path}");
                    MessageBox.Show("Неверный тип файла или файл не найден", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (cameraIndex >= 0)
            {
                if (patientId <= 0)
                {
                    Logging.Log(null, $"FormDetect(); Incorrect patient id FormDetect.Init");
                    MessageBox.Show("Неверный идентификатор пациента", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                this.patientId = patientId;
                this.cameraIndex = cameraIndex;

                try
                {
                    grabber.SetCapture("", cameraIndex);
                }
                catch (Exception ex)
                {
                    this.Close();
                    Logging.Log(ex, $"FormDetect(); Couldn't set capture for camera {cameraIndex}");
                    MessageBox.Show("Не могу произвести захват с веб-камеры", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            chart1.ChartAreas[0].AxisX.Title = "Время, с";
            chart1.ChartAreas[0].AxisY.Title = "X, мм";
            chart2.ChartAreas[0].AxisX.Title = "X, мм";
            chart2.ChartAreas[0].AxisY.Title = "Y, мм";

            pbxBinB.Visible = pbxBinG.Visible = pbxBinR.Visible = pbxBinH.Visible = pbxBinS.Visible = pbxBinV.Visible = false;
        }

        #region funcs
        private void UpdateStep()
        {
            isNowDetect = false;
            pnlStep1.Visible = pnlStep2.Visible = pnlStep3.Visible = pnlStep3Expand
                .Visible = pnlStep3Gradient.Visible = pnlStep4.Visible = false;
            btnBack.Visible = btnNext.Visible = true;
            timer1.Stop();
            switch (currentStep)
            {
                case 0:
                    {
                        pnlStep1.Visible = true;
                        btnNext.Visible = false;
                        btnBack.Visible = false;
                        pnlStep1.Dock = DockStyle.Fill;
                    }

                    break;
                case 1:
                    {
                        pnlStep2.Visible = true;
                        pnlStep2.Dock = DockStyle.Fill;
                        timer1.Start();
                    }

                    break;
                case 2:
                    {
                        if (algorithm == Algorithm.Gradient)
                        {
                            pnlStep3Gradient.Visible = true;
                            pnlStep3Gradient.Dock = DockStyle.Fill;

                        }
                        else if (algorithm == Algorithm.BinarizationExpand)
                        {
                            pnlStep3Expand.Visible = true;
                            pnlStep3Expand.Dock = DockStyle.Fill;
                        }
                        else if (algorithm == Algorithm.Binarization)
                        {
                            pnlStep3.Visible = true;
                            pnlStep3.Dock = DockStyle.Fill;
                        }

                        timer1.Start();
                    }
                    break;
                case 3:
                    {
                        pnlStep4.Visible = true;
                        btnNext.Visible = false;
                        btnPause.Visible = false;
                        pnlStep4.Dock = DockStyle.Fill;
                        centerLeft = centerRight = 0;
                        CvInvoke.cvReleaseVideoWriter(ref this.writerVideo);
                        centerLeft = centerLeftY = centerRight = centerRightY = 0;
                        maxX = minX = 0;
                        calibFactor = double.Parse(txtCalib.Text) / Math.Sqrt(Math.Pow(calibPt1.X - calibPt2.X, 2) + Math.Pow(calibPt2.Y - calibPt1.Y, 2));

                        if (cameraIndex >= 0)
                        {
                            txtTime.Visible = true;
                            lblminutestitle.Visible = true;
                        }

                        groupMeanMedian.Visible = (algorithm == Algorithm.Binarization || algorithm == Algorithm.BinarizationExpand);

                        chart1.Series[0].Points.Clear();
                        chart1.Series[1].Points.Clear();
                        movieLength = grabber.Length;
                        btnStartDetect.Text = "Начать";
                    }

                    break;
            }

            this.lblLevelIndex.Text = (currentStep + 1).ToString() + ". ";
            this.lblLevel.Text = stepTitles[currentStep];
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (this.CheckStepCorrect())
            {
                this.currentStep++;
                this.UpdateStep();
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (currentStep == 3)
            {
                if (MessageBox.Show("Вы уверены, что хотите прервать детектирование?", "Вы уверены?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }

            this.currentStep--;
            this.UpdateStep();
        }

        private bool CheckStepCorrect()
        {
            switch (currentStep)
            {
                case 1:
                    {
                        if (roiLeft.Width <= 0 || roiLeft.Height <= 0)
                        {
                            MessageBox.Show("Выделите область для левого глаза", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }

                        if (roiRight.Width <= 0 || roiRight.Height <= 0)
                        {
                            MessageBox.Show("Выделите область для правого глаза", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }

                        if (calibPt1 == calibPt2)
                        {
                            MessageBox.Show("Проведите калибровочный отрезок", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }

                        double d;
                        if (!double.TryParse(txtCalib.Text, out d) || d <= 0)
                        {
                            MessageBox.Show("Неверно введено калибровочное значение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                    }

                    break;
                case 2:
                    {
                        if (algorithm == Algorithm.Binarization && tbStep3.Value <= 0)
                        {
                            MessageBox.Show("Выберите порог бинаризации", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                        else if (algorithm == Algorithm.BinarizationExpand)
                        {
                            if (!chboxB.Checked && !chboxG.Checked && !chboxH.Checked && !chboxR.Checked && !chboxS.Checked && !chboxV.Checked)
                            {
                                MessageBox.Show("Выберите хотя бы одну из компонент", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            }

                        }
                        else if (algorithm == Algorithm.Gradient)
                        {

                        }
                    }

                    break;
            }

            return true;
        }

        private void FormDetect_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (currentStep == 3 && !end)
            {
                if (MessageBox.Show("Прогресс будет утерян, вы действительно хотите закрыть форму?", "Вы уверены?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            CvInvoke.cvReleaseVideoWriter(ref this.writerVideo);
            grabber.ReleaseCapture();
        }
        #endregion

        #region step1
        private void btn_1_Binarization_Click(object sender, EventArgs e)
        {
            this.algorithm = Algorithm.Binarization;
            this.currentStep++;
            this.UpdateStep();
        }

        private void btn_1_Gradient_Click(object sender, EventArgs e)
        {
            this.algorithm = Algorithm.Gradient;
            this.currentStep++;
            this.UpdateStep();
        }
        #endregion

        #region step2
        private void btnLeft_Click(object sender, EventArgs e)
        {
            lblStep2Status.Text = "Выделите область левого глаза";
            btnLeft.Checked = true;
            btnRight.Checked = false;
            btnCalib.Checked = false;
            this.action = Action.RegionLeft;
        }

        private void btnRight_Click(object sender, EventArgs e)
        {
            lblStep2Status.Text = "Выделите область правого глаза";
            this.action = Action.RegionRight;
            btnLeft.Checked = false;
            btnRight.Checked = true;
            btnCalib.Checked = false;
        }

        private void btnCalib_Click(object sender, EventArgs e)
        {
            lblStep2Status.Text = "Проведите калибровочный отрезок";
            this.action = Action.Calibration;
            btnLeft.Checked = false;
            btnRight.Checked = false;
            btnCalib.Checked = true;
        }

        private void pbxStep2_MouseUp(object sender, MouseEventArgs e)
        {
            this.isMouseDown = false;
        }

        private void pbxStep2_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouseDown)
            {
                return;
            }

            if (action == Action.RegionLeft)
            {
                roiLeft.X = e.X > xStart ? xStart : e.X;
                roiLeft.Y = e.Y > yStart ? yStart : e.Y;
                roiLeft.Width = Math.Abs(e.X - xStart);
                roiLeft.Height = Math.Abs(e.Y - yStart);
            }
            else if (action == Action.RegionRight)
            {
                roiRight.X = e.X > xStart ? xStart : e.X;
                roiRight.Y = e.Y > yStart ? yStart : e.Y;
                roiRight.Width = Math.Abs(e.X - xStart);
                roiRight.Height = Math.Abs(e.Y - yStart);
            }
            else if (action == Action.Calibration)
            {
                calibPt2 = e.Location;
            }

            if (!timer1.Enabled)
            {
                //var imgClone = grabber.Image.Clone();
                //bitStep2?.Dispose();
                var bitStep2 = new Bitmap(grabber.Image.Bitmap);
                Graphics g = Graphics.FromImage(bitStep2);
                g.DrawRectangle(Pens.Red, roiLeft);
                g.DrawRectangle(Pens.Blue, roiRight);
                g.DrawLine(Pens.Azure, calibPt1, calibPt2);

                pbxStep2.Image = bitStep2;
                g.Dispose();
                //imgClone.Dispose();
            }
        }

        private void pbxStep2_MouseDown(object sender, MouseEventArgs e)
        {
            this.isMouseDown = true;
            switch (action)
            {
                case Action.RegionLeft:
                    {
                        xStart = e.X;
                        yStart = e.Y;
                    }

                    break;
                case Action.RegionRight:
                    {
                        xStart = e.X;
                        yStart = e.Y;
                    }

                    break;
                case Action.Calibration:
                    {
                        calibPt1 = e.Location;
                    }

                    break;
                default:
                    break;
            }
        }

        private void btnStartPauseDetect_Click(object sender, EventArgs e)
        {
            isNowDetect = !isNowDetect;
        }

        private void btnStartPause_Click(object sender, EventArgs e)
        {
            timer1.Enabled = !timer1.Enabled;
        }

        private void tbReduce_Scroll_1(object sender, EventArgs e)
        {
            lblTbReduce.Text = tbReduce.Value.ToString();
        }
        #endregion step2

        #region step 3
        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            pnlStep3.Visible = false;
            pnlStep3Expand.Visible = true;
            pnlStep3Expand.Dock = DockStyle.Fill;
            algorithm = Algorithm.BinarizationExpand;
        }

        private void btnHideExpand_Click(object sender, EventArgs e)
        {
            pnlStep3.Visible = true;
            pnlStep3Expand.Visible = false;
            algorithm = Algorithm.Binarization;
        }

        private void tbStep3_Scroll(object sender, EventArgs e)
        {
            lblTrackBarStep3.Text = tbStep3.Value.ToString();
        }

        private void chbxHideImages_CheckedChanged(object sender, EventArgs e)
        {
            pbxDetectBin.Visible = pbxDetectColor.Visible = !chbxHideImages.Checked;
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            isNowDetect = !isNowDetect;
            btnPause.Text = isNowDetect ? "Пауза" : "Продолжить";
            timer1.Enabled = isNowDetect;
        }

        private void ChooseImageBinarization()
        {
            var color = grabber.Image.Reduce(tbReduce.Value);
            var gray = GrayBin.ToGray(color);

            if (chbxSmooth.Checked)
            {
                GrayBin.Smooth(gray);
            }

            if (chbxNormilize.Checked)
            {
                GrayBin.Normalize(gray);
            }

            var bin = GrayBin.Binarize(gray, tbStep3.Value);

            pbxStep3Gray.Image = new Bitmap(gray.Bitmap);
            GrayBin.InvertInRegions(bin, roiLeft.Reduce(tbReduce.Value), roiRight.Reduce(tbReduce.Value));
            pbxStep3Bin.Image = new Bitmap(bin.Bitmap);

            gray.Dispose();
            bin.Dispose();
            color.Dispose();
        }

        private void ChooseImageBinarizationExpand()
        {
            var components = new List<Image<Gray, byte>>();
            int reduce = tbReduce.Value;
            var small = grabber.Image.Reduce(reduce);
            var hsv = GrayBin.ToHsv(small);
            var rgb = GrayBin.ToRgbChanels(small);

            if (pbxBinR.Visible)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(rgb[0]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(rgb[0]);
                }

                components.Add(GrayBin.Binarize(rgb[0], tbRMax.Value, tbRMin.Value));
                var bin = components.Last().Clone();
                GrayBin.InvertInRegions(bin, roiLeft.Reduce(reduce), roiRight.Reduce(reduce));
                pbxBinR.Image = new Bitmap(bin.Bitmap);
                bin.Dispose();
            }

            if (pbxBinG.Visible)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(rgb[1]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(rgb[1]);
                }

                components.Add(GrayBin.Binarize(rgb[1], tbGMax.Value, tbGMin.Value));
                var bin = components.Last().Clone();
                GrayBin.InvertInRegions(bin, roiLeft.Reduce(reduce), roiRight.Reduce(reduce));
                pbxBinG.Image = new Bitmap(bin.Bitmap);
                bin.Dispose();
            }

            if (pbxBinB.Visible)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(rgb[2]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(rgb[2]);
                }

                components.Add(GrayBin.Binarize(rgb[2], tbBMax.Value, tbBMin.Value));
                var bin = components.Last().Clone();
                GrayBin.InvertInRegions(bin, roiLeft.Reduce(reduce), roiRight.Reduce(reduce));
                pbxBinB.Image = new Bitmap(bin.Bitmap);
                bin.Dispose();
            }

            if (pbxBinH.Visible)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(hsv[0]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(hsv[0]);
                }

                components.Add(GrayBin.Binarize(hsv[0], tbHMax.Value, tbHMin.Value));
                var bin = components.Last().Clone();
                GrayBin.InvertInRegions(bin, roiLeft.Reduce(reduce), roiRight.Reduce(reduce));
                pbxBinH.Image = new Bitmap(bin.Bitmap);
                bin.Dispose();
            }

            if (pbxBinS.Visible)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(hsv[1]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(hsv[1]);
                }

                components.Add(GrayBin.Binarize(hsv[1], tbSMax.Value, tbSMin.Value));
                var bin = components.Last().Clone();
                GrayBin.InvertInRegions(bin, roiLeft.Reduce(reduce), roiRight.Reduce(reduce));
                pbxBinS.Image = new Bitmap(bin.Bitmap);
                bin.Dispose();
            }

            if (pbxBinV.Visible)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(hsv[2]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(hsv[2]);
                }

                components.Add(GrayBin.Binarize(hsv[2], tbVMax.Value, tbVMin.Value));
                var bin = components.Last().Clone();
                GrayBin.InvertInRegions(bin, roiLeft.Reduce(reduce), roiRight.Reduce(reduce));
                pbxBinV.Image = new Bitmap(bin.Bitmap);
                bin.Dispose();
            }

            pbxGrayH.Image = hsv[0].Bitmap;
            pbxGrayS.Image = hsv[1].Bitmap;
            pbxGrayV.Image = hsv[2].Bitmap;
            pbxGrayR.Image = rgb[0].Bitmap;
            pbxGrayG.Image = rgb[1].Bitmap;
            pbxGrayB.Image = rgb[2].Bitmap;

            if (components.Count > 0)
            {
                var result = GrayBin.And(components);

                if (result != null)
                {
                    GrayBin.InvertInRegions(result, roiLeft.Reduce(reduce), roiRight.Reduce(reduce));
                    pbxResult.Image = new Bitmap(result.Bitmap);
                    result.Dispose();
                }
            }

            hsv.Dispose();
            foreach (var item in rgb)
            {
                item.Dispose();
            }

            foreach (var item in components)
            {
                item.Dispose();
            }

            small.Dispose();
        }

        private void ChooseImageGradient()
        {
            int reduce = tbReduce.Value;
            var small = grabber.Image.Reduce(reduce);
            var gray = GrayBin.ToGray(small);
            var hsv = GrayBin.ToHsv(small);
            var rgb = GrayBin.ToRgbChanels(small);
            if (chbxSmoothGradient.Checked)
            {
                GrayBin.Smooth(hsv[0]);
                GrayBin.Smooth(hsv[1]);
                GrayBin.Smooth(hsv[2]);

                GrayBin.Smooth(rgb[0]);
                GrayBin.Smooth(rgb[1]);
                GrayBin.Smooth(rgb[2]);
                GrayBin.Smooth(gray);
            }

            if (chbxNormalizeGradient.Checked)
            {
                GrayBin.Normalize(hsv[0]);
                GrayBin.Normalize(hsv[1]);
                GrayBin.Normalize(hsv[2]);

                GrayBin.Normalize(rgb[0]);
                GrayBin.Normalize(rgb[1]);
                GrayBin.Normalize(rgb[2]);
                GrayBin.Normalize(gray);
            }

            pbxGradientH.Image = hsv[0].Bitmap;
            pbxGradientS.Image = hsv[1].Bitmap;
            pbxGradientV.Image = hsv[2].Bitmap;
            pbxGradientR.Image = rgb[0].Bitmap;
            pbxGradientG.Image = rgb[1].Bitmap;
            pbxGradientB.Image = rgb[2].Bitmap;
            pbxGradientGray.Image = gray.Bitmap;

            hsv.Dispose();
            foreach (var item in rgb)
            {
                item.Dispose();
            }

            gray.Dispose();
            small.Dispose();
        }

        private void ChooseImage()
        {
            if (algorithm == Algorithm.Binarization)
            {
                ChooseImageBinarization();
            }
            else if (algorithm == Algorithm.BinarizationExpand)
            {
                ChooseImageBinarizationExpand();
            }
            else if (algorithm == Algorithm.Gradient)
            {
                ChooseImageGradient();
            }
        }

        private void chboxR_CheckedChanged(object sender, EventArgs e)
        {
            pbxBinR.Visible = chboxR.Checked;
        }
        private void chboxH_CheckedChanged(object sender, EventArgs e)
        {
            pbxBinH.Visible = chboxH.Checked;
        }
        private void chboxS_CheckedChanged(object sender, EventArgs e)
        {
            pbxBinS.Visible = chboxS.Checked;
        }
        private void chboxG_CheckedChanged(object sender, EventArgs e)
        {
            pbxBinG.Visible = chboxG.Checked;
        }
        private void chboxV_CheckedChanged(object sender, EventArgs e)
        {
            pbxBinV.Visible = chboxV.Checked;
        }
        private void chboxB_CheckedChanged(object sender, EventArgs e)
        {
            pbxBinB.Visible = chboxB.Checked;
        }

        private void chbxGraphics_CheckedChanged(object sender, EventArgs e)
        {
            chart1.Visible = chart2.Visible = chbxGraphics.Checked;
        }

        private void FormDetect_Load(object sender, EventArgs e)
        {

        }
        #endregion

        #region step4
        private void btnStartDetect_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Начать детектирование?", "Вы уверены?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (cameraIndex >= 0)
                {
                    if (!int.TryParse(txtTime.Text, out timeInterval) || timeInterval <= 0)
                    {
                        MessageBox.Show("Задайте время в минутах", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Filter = "Avi files (*.avi)|*.avi";
                    sfd.FilterIndex = 1;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        filePath = sfd.FileName;
                        writerVideo = CvInvoke.cvCreateVideoWriter(filePath, CvInvoke.CV_FOURCC('X', 'V', 'I', 'D'), 25, new Size(grabber.Image.Width, grabber.Image.Height), true);
                    }
                    else
                    {
                        return;
                    }

                    timeInterval = timeInterval * 60000;

                    startTime = DateTime.Now;
                }

                grabber.SetFramePos(0);
                timer1.Enabled = true;
                isNowDetect = true;
                btnStartDetect.Text = "Сначала";
                btnPause.Visible = true;
                btnPause.Text = "Пауза";
            }
        }

        private Image<Gray, Byte> GetImageForExpandBinarization(Image<Bgr, Byte> img)
        {
            var components = new List<Image<Gray, byte>>();

            var hsv = GrayBin.ToHsv(img);

            var rgb = GrayBin.ToRgbChanels(img);

            if (chboxR.Checked)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(rgb[0]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(rgb[0]);
                }

                components.Add(GrayBin.Binarize(rgb[0], tbRMax.Value, tbRMin.Value));
            }

            if (chboxG.Checked)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(rgb[1]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(rgb[1]);
                }

                components.Add(GrayBin.Binarize(rgb[1], tbGMax.Value, tbGMin.Value));
            }

            if (chboxB.Checked)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(rgb[2]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(rgb[2]);
                }

                components.Add(GrayBin.Binarize(rgb[2], tbBMax.Value, tbBMin.Value));
            }

            if (chboxH.Checked)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(hsv[0]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(hsv[0]);
                }

                components.Add(GrayBin.Binarize(hsv[0], tbHMax.Value, tbHMin.Value));
            }

            if (chboxS.Checked)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(hsv[1]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(hsv[1]);
                }

                components.Add(GrayBin.Binarize(hsv[1], tbSMax.Value, tbSMin.Value));
            }

            if (chboxV.Checked)
            {
                if (chbxSmoothExpand.Checked)
                {
                    GrayBin.Smooth(hsv[2]);
                }

                if (chbxNormilizeExpand.Checked)
                {
                    GrayBin.Normalize(hsv[2]);
                }

                components.Add(GrayBin.Binarize(hsv[2], tbVMax.Value, tbVMin.Value));
            }

            pbxGrayH.Image = hsv[0].Bitmap;
            pbxGrayS.Image = hsv[1].Bitmap;
            pbxGrayV.Image = hsv[2].Bitmap;
            pbxGrayR.Image = rgb[0].Bitmap;
            pbxGrayG.Image = rgb[1].Bitmap;
            pbxGrayB.Image = rgb[2].Bitmap;

            var result = GrayBin.And(components);

            hsv.Dispose();
            foreach (var item in rgb)
            {
                item.Dispose();
            }

            foreach (var item in components)
            {
                item.Dispose();
            }

            return result;
        }

        private Image<Gray, Byte> DetectGradient(Image<Bgr, Byte> color, out PointF ptLeft, out PointF ptRight)
        {
            Image<Gray, Byte> detectImage = null;
            if (radioGray.Checked)
            {
                detectImage = GrayBin.ToGray(color);
            }
            else if (radioR.Checked)
            {
                var rgb = GrayBin.ToRgbChanels(color);
                detectImage = rgb[0].Clone();
                foreach (var item in rgb)
                {
                    item.Dispose();
                }
            }
            else if (radioG.Checked)
            {
                var rgb = GrayBin.ToRgbChanels(color);
                detectImage = rgb[1].Clone();
                foreach (var item in rgb)
                {
                    item.Dispose();
                }
            }
            else if (radioB.Checked)
            {
                var rgb = GrayBin.ToRgbChanels(color);
                detectImage = rgb[2].Clone();
                foreach (var item in rgb)
                {
                    item.Dispose();
                }
            }
            else if (radioH.Checked)
            {
                var hsv = GrayBin.ToHsv(color);
                detectImage = hsv[0].Clone();
                hsv.Dispose();
            }
            else if (radioS.Checked)
            {
                var hsv = GrayBin.ToHsv(color);
                detectImage = hsv[1].Clone();
                hsv.Dispose();
            }
            else if (radioV.Checked)
            {
                var hsv = GrayBin.ToHsv(color);
                detectImage = hsv[2].Clone();
                hsv.Dispose();
            }

            CvInvoke.cvSetImageROI(detectImage, roiLeft.Reduce(tbReduce.Value));
            ptLeft = Detect.EyeCenter(detectImage);
            CvInvoke.cvResetImageROI(detectImage);
            ptLeft.X += roiLeft.Reduce(tbReduce.Value).X;
            ptLeft.Y += roiLeft.Reduce(tbReduce.Value).Y;

            ptRight = Point.Empty;

            return detectImage;
        }

        private void EndDetect()
        {
            timer1.Enabled = false;
            btnStartDetect.Visible = false;
            btnPause.Visible = false;
            end = true;
            if (record == null)
            {
                try
                {
                    //----Create Record here----
                    Record.AddToDB(this.patientId, filePath, DateTime.Now, txtComments.Text, 0, this.calibFactor);
                    record = new Record(DataBase.Instance.LastId("Records"), patientId, filePath, DateTime.Now, txtComments.Text, 0, this.calibFactor);
                    //--------------------------
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении в БД", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logging.Log(ex, $"EndDetect() Record.AddToDB({this.patientId}, {filePath}, {DateTime.Now}, {txtComments.Text}, {0}, {this.calibFactor})");
                    this.Close();
                    return;
                }
            }

            record.SetData(recordData);

            try
            {
                record.CalibFactor = this.calibFactor;
                record.Comments = this.txtComments.Text;
                record.UpdateDB();
                record.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при сохранении данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.Log(ex, $"EndDetect() record.Save(); {record?.Id}");
                this.Close();
                return;
            }

            MessageBox.Show("Детектирование успешно завершено!", "Выполнено", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Close();
            return;
        }
        
        private void DetectEyes()
        {
            if (isNowDetect)
            {
                int time = cameraIndex >= 0 ? (int)(DateTime.Now - startTime).TotalMilliseconds : grabber.GetTimePos();

                if (cameraIndex >= 0)
                {
                    if ((DateTime.Now - startTime).TotalMilliseconds >= timeInterval)
                    {
                        EndDetect();
                        return;
                    }
                }
                else if (grabber.GetFramePos() >= grabber.Length - 1)
                {
                    EndDetect();
                    return;
                }

                Image<Bgr, Byte> color = null;

                var roiLeftReduce = roiLeft.Reduce(tbReduce.Value);
                var roiRightReduce = roiRight.Reduce(tbReduce.Value);

                if (tbReduce.Value > 1)
                {
                    color = grabber.Image.Clone().Reduce(tbReduce.Value);
                }
                else
                {
                    color = grabber.Image.Clone();
                }

                PointF ptLeft = PointF.Empty, ptRight = PointF.Empty;
                Image<Gray, Byte> detectImage = null;

                if (algorithm == Algorithm.Binarization)
                {
                    detectImage = GrayBin.ToGray(color);
                    if (chbxSmooth.Checked)
                    {
                        GrayBin.Smooth(detectImage);
                    }

                    if (chbxNormilize.Checked)
                    {
                        GrayBin.Normalize(detectImage);
                    }

                    GrayBin.BinarizeImmediate(detectImage, tbStep3.Value);


                }
                else if (algorithm == Algorithm.BinarizationExpand)
                {
                    detectImage = this.GetImageForExpandBinarization(color);
                }
                else if (algorithm == Algorithm.Gradient)
                {
                    detectImage = this.DetectGradient(color, out ptLeft, out ptRight);
                }

                if (algorithm == Algorithm.Binarization || algorithm == Algorithm.BinarizationExpand)
                {
                    if (radioMean.Checked)
                    {
                        ptLeft = Detect.DetectMean(detectImage, roiLeftReduce);
                        ptRight = Detect.DetectMean(detectImage, roiRightReduce);
                    }
                    else
                    {
                        ptLeft = Detect.DetectMedian(detectImage, roiLeftReduce);
                        ptRight = Detect.DetectMedian(detectImage, roiRightReduce);
                    }
                }

                double scaleFactor = (double)tbReduce.Value * calibFactor;

                var ptLeftScale = ptLeft.Scale(scaleFactor);
                var ptRightScale = ptRight.Scale(scaleFactor);
                recordData.Add(new DataStruct(ptLeftScale, ptRightScale, time));

                if (pbxDetectBin.Visible && pbxDetectColor.Visible)
                {
                    if (algorithm == Algorithm.Binarization || algorithm == Algorithm.BinarizationExpand)
                    {
                        GrayBin.InvertInRegions(detectImage, roiLeftReduce, roiRightReduce);
                    }

                    Bitmap colorBit = new Bitmap(color.Bitmap);
                    Bitmap detectBit =new Bitmap(detectImage.Bitmap);

                    var g = Graphics.FromImage(colorBit);
                    g.DrawLine(Pens.Red, new Point(0, (int)ptLeft.Y), new Point(colorBit.Width, (int)ptLeft.Y));
                    g.DrawLine(Pens.Red, new Point((int)ptLeft.X, 0), new Point((int)ptLeft.X, colorBit.Height));
                    g.DrawLine(Pens.Blue, new Point(0, (int)ptRight.Y), new Point(colorBit.Width, (int)ptRight.Y));
                    g.DrawLine(Pens.Blue, new Point((int)ptRight.X, 0), new Point((int)ptRight.X, colorBit.Height));
                    g.DrawRectangle(Pens.Azure, roiLeftReduce);
                    g.DrawRectangle(Pens.Azure, roiRightReduce);

                    pbxDetectColor.Image = colorBit;
                    pbxDetectBin.Image = detectBit;

                    //colorBit.Dispose();
                    //detectBit.Dispose();
                    g.Dispose();
              }

                #region graph
                if (chart1.Visible && chart2.Visible && ptLeft.X > 0 && ptRight.X > 0 && time > 10000)
                {
                    Record.Interpolate(recordData, 10);

                    if (centerLeft == 0 || centerRight == 0)
                    {
                        centerLeft = Record.CenterLeftX(recordData);
                        centerRight = Record.CenterRightX(recordData);
                    }

                    if (minX == 0 || maxX == 0)
                    {
                        minX = recordData[0].left.X - centerLeft;
                        maxX = recordData[0].left.X - centerLeft;
                        foreach (var item in recordData)
                        {
                            if (minX > item.left.X - centerLeft)
                            {
                                minX = item.left.X - centerLeft;
                            }

                            if (maxX < item.left.X - centerLeft)
                            {
                                maxX = item.left.X - centerLeft;
                            }

                            if (minX > item.right.X - centerRight)
                            {
                                minX = item.right.X - centerRight;
                            }

                            if (maxX < item.right.X - centerRight)
                            {
                                maxX = item.right.X - centerRight;
                            }
                        }
                    }

                    if (centerLeftY == 0 || centerRightY == 0)
                    {
                        centerLeftY = Record.CenterLeftY(recordData);
                        centerRightY = Record.CenterRightY(recordData);

                        double minY = 100000000;
                        for (int i = 0; i < recordData.Count; i++)
                        {
                            if ((recordData[i].left.Y - centerLeftY) < minY && recordData[i].left.Y > 0)
                            {
                                minY = recordData[i].left.Y - centerLeftY;
                            }

                            if ((recordData[i].right.Y - centerRightY) < minY && recordData[i].right.Y > 0)
                            {
                                minY = recordData[i].right.Y - centerRightY;
                            }
                        }

                        chart2.ChartAreas[0].AxisY.Minimum = Math.Round(minY, 2);
                    }

                    if ((ptLeftScale.X - centerLeft) >= minX * 1.2 && (ptRightScale.X - centerRight) >= minX * 1.2 && (ptLeftScale.X - centerLeft) <= maxX * 1.2 && (ptRightScale.X - centerRight) <= maxX * 1.2)
                    {
                        chart1.Series[0].Points.AddXY(Math.Round(time / 1000.0, 2), ptLeftScale.X - centerLeft);
                        chart1.Series[1].Points.AddXY(Math.Round(time / 1000.0, 2), ptRightScale.X - centerRight);

                        chart2.Series[0].Points.AddXY(Math.Round(ptLeftScale.X - centerLeft, 2), Math.Round(ptLeftScale.Y - centerLeftY, 2));
                        chart2.Series[1].Points.AddXY(Math.Round(ptRightScale.X - centerRight, 2), Math.Round(ptRightScale.Y - centerRightY, 2));

                        if (chart1.Width < time / 200)
                        {
                            chart1.Width += 1000;
                        }
                    }
                }

                #endregion

                if (cameraIndex >= 0)
                {
                    try
                    {
                        CvInvoke.cvWriteFrame(this.writerVideo, this.grabber.Image);
                    }
                    catch
                    {
                        if (MessageBox.Show("Ошибка при сохранении очередного кадра в видеофайл. Детектирование прервано. Хотите сохранить выполненную часть?", "Ошибка", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                        {
                            this.EndDetect();
                        }
                        else
                        {
                            this.Close();
                        }
                    }

                    try { progressBar1.Value = (int)(((double)time / timeInterval) * 100); } catch { }
                }
                else
                {
                    progressBar1.Value = (int)((1.0 - ((double)(movieLength - grabber.GetFramePos()) / (double)movieLength)) * 100);
                }

                
                color.Dispose();
                detectImage.Dispose();
            }
        }
        #endregion

        //Bitmap bitStep2;
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                grabber.GrabImage();
            }
            catch(Exception ex)
            {
                if (isNowDetect)
                {
                     if (MessageBox.Show("Ошибка при получении изображения из источника захвата. Детектирование прервано. Хотите сохранить выполненную часть?", "Ошибка", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                     {
                         this.EndDetect();
                     }
                     else
                     {
                         this.Close();
                     }

                    return;
                }
                else
                {
                    if (cameraIndex < 0)
                    {
                        grabber.SetFramePos(0);
                        grabber.GrabImage();
                    }
                }
            }

            switch (currentStep)
            {
                case 1:
                    {
                        //bitStep2?.Dispose();
                        var bitStep2 = new Bitmap(grabber.Image.Bitmap);
                        Graphics g = Graphics.FromImage(bitStep2);
                        g.DrawRectangle(Pens.Red, roiLeft);
                        g.DrawRectangle(Pens.Blue, roiRight);
                        g.DrawLine(Pens.Azure, calibPt1, calibPt2);
                        pbxStep2.Image = bitStep2;
                        g.Dispose();
                    }

                    break;
                case 2:
                    {
                        ChooseImage();
                    }

                    break;

                case 3:
                    {
                        this.DetectEyes();
                    }
                    break;
            }
        }
        
        #region notUsed
        private void pnlStep5_Paint(object sender, PaintEventArgs e) { }
        #endregion
    }
}
