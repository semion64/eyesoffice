namespace _SSU_EyesOffice
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
    using System.IO;
    using System.Windows.Forms.DataVisualization.Charting;
    using Word = Microsoft.Office.Interop.Word;

    public partial class FormMain : Form
    {
        private List<Patient> patients;

        private Grabber grabber = new Grabber();

        private int currentFrame = 0;

        private Record currentRecord = null;

        private bool isNoVideo = false;

        public FormMain()
        {
            InitializeComponent();
            UpdatePatientsList();

            chart1.ChartAreas[0].CursorX.Position = 0;
            chart1.ChartAreas[0].CursorX.LineColor = Color.Green;
            chart1.ChartAreas[0].CursorX.LineWidth = 2;
            chart1.ChartAreas[0].AxisX.Title = "№ кадра";
            chart1.ChartAreas[0].AxisY.Title = "X, мм";
            chart1.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart1.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart1.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Gray;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Gray;

            chart2.ChartAreas[0].CursorX.Position = 0;
            chart2.ChartAreas[0].CursorX.LineColor = Color.Green;
            chart2.ChartAreas[0].CursorX.LineWidth = 2;
            chart2.ChartAreas[0].AxisX.Title = "№ кадра";
            chart2.ChartAreas[0].AxisY.Title = "Y, мм";
            chart2.ChartAreas[0].AxisX.MajorGrid.LineColor = Color.Gray;
            chart2.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Gray;

            chart2.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            chart2.ChartAreas[0].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;

            File.Delete($"{Directory.GetCurrentDirectory()}//temp.jpeg");
            FormMain_Resize(null, null);
        }

        private void UpdatePatientsList()
        {
            this.EnabledPatientButtons(false);
            this.ShowPatientRecords(false);
            this.btnAnalize.Enabled = this.btnDeleteRecord.Enabled = btnChangeVideo.Enabled = false;

            patients = new List<Patient>();
            patients = Patient.SelectAllFromDB();

            this.listBoxPatients.Items.Clear();

            foreach (var item in patients)
            {
                this.listBoxPatients.Items.Add($"{item.SecondName} {item.Name}");
            }

            if (listBoxPatients.Items.Count > 0)
            {
                listBoxPatients.SelectedIndex = 0;
            }
        }

        private void btnAddPatient_Click(object sender, EventArgs e)
        {
            var formAddPatinet = new Forms.FormAddPatient();
            formAddPatinet.Show();
            formAddPatinet.FormClosed += FormAddPatinet_FormClosed;
        }

        private void FormAddPatinet_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.UpdatePatientsList();
        }

        private void btnEditPatient_Click(object sender, EventArgs e)
        {
            if (listBoxPatients.SelectedIndex < 0)
            {
                return;
            }

            var formEditPatinet = new Forms.FormEditPatient(this.patients[listBoxPatients.SelectedIndex].Id);
            formEditPatinet.FormClosed += FormEditPatinet_FormClosed;
        }

        private void FormEditPatinet_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.UpdatePatientsList();
        }

        private void listBoxPatients_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.ShowPatientRecords(false);
            this.btnAnalize.Enabled = this.btnDeleteRecord.Enabled = btnChangeVideo.Enabled = false;
            if (listBoxPatients.SelectedIndex < 0)
            {
                return;
            }

            var patient = this.patients[listBoxPatients.SelectedIndex];
            lblCurrentPatientName.Text = patient.Name;
            lblCurrentPatientSecondName.Text = patient.SecondName;
            lblCurrentPatientBirthDate.Text = patient.BirthDate.ToShortDateString();
            lblCurrentPatientSex.Text = patient.IsFemale ? "женский" : "мужской";
            lblCurrentPatientComments.Text = patient.Comments;

            this.EnabledRecordButtons(true);
            this.EnabledPatientButtons(true);

            try
            {
                this.patients[listBoxPatients.SelectedIndex].Records = Record.SelectAllFromDB(this.patients[listBoxPatients.SelectedIndex].Id);
            }
            catch (Exception ex)
            {
                Logging.Log(ex, $"listBoxPatients_SelectedIndexChanged(object sender, EventArgs e)  Record.SelectAllFromDB()");
                MessageBox.Show("Ошибка запроса к базе данных записей пациента", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.ShowPatientRecords(false);
                return;
            }

            this.dataGrid.Rows.Clear();

            foreach (var item in this.patients[listBoxPatients.SelectedIndex].Records)
            {
                this.dataGrid.Rows.Add(new string[] { item.Id.ToString(), item.Path, item.Date.ToString() });
            }

            this.ShowPatientRecords(true);

            dataGrid_SelectionChanged(null, null);
        }

        private void btnDeletePatient_Click(object sender, EventArgs e)
        {
            if (listBoxPatients.SelectedIndex < 0)
            {
                return;
            }

            var patient = patients[listBoxPatients.SelectedIndex];
            if (MessageBox.Show($"Вы уверены, что хотите удалить пациента: [{patient.Id}] {patient.Name} {patient.SecondName}? Все записи с ним также будут удалены", "Вы уверены?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    patients[listBoxPatients.SelectedIndex].Delete();
                }
                catch (Exception ex)
                {
                    Logging.Log(ex, $"btnDeletePatient_Click(object sender, EventArgs e); patients[{listBoxPatients.SelectedIndex}].Delete();");
                    MessageBox.Show("Ошибка при удалении", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Пациент удален", "Удалено", MessageBoxButtons.OK, MessageBoxIcon.Information);

                listBoxPatients.Items.Remove(listBoxPatients.SelectedItem);
                this.UpdatePatientsList();
            }

        }

        private void btnFromVideo_Click(object sender, EventArgs e)
        {
            if (listBoxPatients.SelectedIndex < 0)
            {
                return;
            }

            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Record.AddToDB(
                        this.patients[listBoxPatients.SelectedIndex].Id,
                        ofd.FileName,
                        DateTime.Now);
                    listBoxPatients_SelectedIndexChanged(null, null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при добавлении видео", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Logging.Log(ex, $"btnFromVideo_Click(object sender, EventArgs e);  Record.AddToDB();");
                }
            }
        }

        private void dataGrid_SelectionChanged(object sender, EventArgs e)
        {
            this.ShowRecordDetail(false);
            currentFrame = 0;

            isNoVideo = false;

            if (dataGrid.CurrentRow == null
                || dataGrid.CurrentRow.Index < 0
                || patients[listBoxPatients.SelectedIndex].Records.Count == 0
                || dataGrid.CurrentRow.Index >= patients[listBoxPatients.SelectedIndex].Records.Count
                || listBoxPatients.SelectedIndex < 0)
            {
                btnAnalize.Enabled = btnChangeVideo.Enabled = btnDeleteRecord.Enabled = false;
                return;
            }

            this.btnAnalize.Enabled = btnChangeVideo.Enabled = this.btnDeleteRecord.Enabled = true;

            this.currentRecord = patients[listBoxPatients.SelectedIndex].Records[dataGrid.CurrentRow.Index];


            if (!File.Exists($"data//{currentRecord.Id}.dat"))
            {
                return;
            }

            try
            {
                currentRecord.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Некорректный файл с данными о детектировании", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.Log(ex, $"dataGrid_SelectionChanged(object sender, EventArgs e), currentRecord.Open() recordID: {currentRecord?.Id}");
                return;
            }

            if (currentRecord.Length <= 0)
            {
                MessageBox.Show("Видео ещё не было анализировано", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                grabber.ReleaseCapture();
                grabber.SetCapture(currentRecord.Path);
            }
            catch (Exception ex)
            {
                //Logging.Log(ex, $"dataGridSelection_change setCapture for file: {currentRecord?.Path}");
                isNoVideo = true;
                grabber.ReleaseCapture();
                // return;
            }

            lblID.Text = currentRecord.Id.ToString();
            lblPath.Text = string.IsNullOrWhiteSpace(currentRecord.Comments) ? (isNoVideo ? "Видео файл не найден или неверный формат" : currentRecord.Path) : currentRecord.Comments;
            lblDate.Text = currentRecord.Date.ToString("dd.MM.yyyy");

            ShowFrame(0);

            RedrawGraph(currentRecord);
            ShowRecordDetail(true);
        }

        private void RedrawGraph(Record record)
        {
            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();

            chart2.Series[0].Points.Clear();
            chart2.Series[1].Points.Clear();

            chart2.Width = record.Length;
            chart1.Width = record.Length;

            var minLeft = record.CenterLeftX();
            var minRight = record.CenterRightX();

            var minLeftY = record.CenterLeftY();
            var minRightY = record.CenterRightY();
            int i = 0;
            foreach(var item in record.data)
            {
                chart1.Series[0].Points.AddXY(i, item.left.X - minLeft);
                chart1.Series[1].Points.AddXY(i, item.right.X - minRight);

                chart2.Series[0].Points.AddXY(i, item.left.Y - minLeftY);
                chart2.Series[1].Points.AddXY(i, item.right.Y - minRightY);
                i++;
            }
        }

        private void ShowFrame(int frame)
        {
            if (isNoVideo)
            {
                pbxColorImage.Visible = false;
                return;
            }

            if (frame < 0 || frame >= currentRecord.Length)
            {
                MessageBox.Show("Выход за границы разпознанных данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                grabber.SetFramePos(frame);
                grabber.GrabImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Выход за границы видео файла", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var left = currentRecord.GetLeft(frame);
            var right = currentRecord.GetRight(frame);

            var colorBit = new Bitmap(grabber.Image.Bitmap);
            var g = Graphics.FromImage(colorBit);
            g.DrawLine(Pens.Red, new Point(0, (int)left.Y), new Point(colorBit.Width, (int)left.Y));
            g.DrawLine(Pens.Red, new Point((int)left.X, 0), new Point((int)left.X, colorBit.Height));
            g.DrawLine(Pens.Blue, new Point(0, (int)right.Y), new Point(colorBit.Width, (int)right.Y));
            g.DrawLine(Pens.Blue, new Point((int)right.X, 0), new Point((int)right.X, colorBit.Height));

            pbxColorImage.Image = colorBit;

            g.Dispose();

            chart1.ChartAreas[0].CursorX.SetCursorPosition(frame);
            chart2.ChartAreas[0].CursorX.SetCursorPosition(frame);
            statusFrame.Text = frame.ToString();
            statusLeft.Text = $"x={Math.Round((currentRecord[currentFrame].left.X - currentRecord.CenterLeftX()), 2).ToString()}  y={Math.Round((currentRecord[currentFrame].left.Y - currentRecord.CenterLeftY()), 2).ToString()}";
            statusRight.Text = $"x={Math.Round((currentRecord[currentFrame].right.X - currentRecord.CenterRightX()), 2).ToString()}  y={Math.Round((currentRecord[currentFrame].right.Y - currentRecord.CenterRightY()), 2).ToString()}";
            statusTime.Text = ((float)currentRecord[currentFrame].msecs / (float)1000).ToString() + " s";

            pbxColorImage.Visible = true;
        }

        private void Form_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Visible = true;
            if (listBoxPatients.SelectedIndex < 0)
            {
                return;
            }

            try
            {
                this.patients[listBoxPatients.SelectedIndex].Records = Record.SelectAllFromDB(this.patients[listBoxPatients.SelectedIndex].Id);
                this.listBoxPatients_SelectedIndexChanged(null, null);
            }
            catch
            {
                MessageBox.Show("Произошла ошибка, обновите список записей", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnFromCamera_Click(object sender, EventArgs e)
        {
            pnlCameraIndex.Visible = true;
        }

        private void btnCameraDetect_Click(object sender, EventArgs e)
        {
            if (this.listBoxPatients.SelectedIndex < 0)
            {
                return;
            }

            int cameraIndex;

            if (!int.TryParse(txtCameraIndex.Text, out cameraIndex) || cameraIndex < 0)
            {
                MessageBox.Show("Введите число больше нуля", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Forms.FormDetect form = new Forms.FormDetect(null, cameraIndex, this.patients[this.listBoxPatients.SelectedIndex].Id);
                form.FormClosed += Form_FormClosed;
                if (form.Visible)
                {
                    this.Visible = false;
                }
            }
            catch (Exception ex)
            {
                this.Visible = true;
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            btnPrev.Enabled = true;
            if (currentFrame + 1 < grabber.Length - 1)
            {
                ShowFrame(++currentFrame);
            }
            else
            {
                btnNext.Enabled = false;
            }
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            btnNext.Enabled = true;
            if (currentFrame - 1 >= 0)
            {
                ShowFrame(--currentFrame);
            }
            else
            {
                btnPrev.Enabled = false;
            }
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            chart1.ChartAreas[0].CursorX.SetCursorPixelPosition(new Point(e.X, e.Y), true);
            chart2.ChartAreas[0].CursorX.SetCursorPixelPosition(new Point(e.X, e.Y), true);
            var x = chart1.ChartAreas[0].CursorX.Position;
            if ((int)x >= grabber.Length - 1 || x < 0)
            {
                MessageBox.Show("Выход за границы файла", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentFrame = (int)x;
            ShowFrame(currentFrame);
        }

        private void pbxColorImage_MouseClick(object sender, MouseEventArgs e)
        {
            if (currentRecord == null)
            {
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                currentRecord.SetLeft(currentFrame, ((PointF)(e.Location)).Scale(currentRecord.CalibFactor));
                chart1.Series[0].Points.Remove(chart1.Series[0].Points[currentFrame]);
                chart1.Series[0].Points.InsertXY(currentFrame, currentFrame, ((PointF)e.Location).Scale(currentRecord.CalibFactor).X - currentRecord.CenterLeftX());

                chart2.Series[0].Points.Remove(chart2.Series[0].Points[currentFrame]);
                chart2.Series[0].Points.InsertXY(currentFrame, currentFrame, ((PointF)e.Location).Scale(currentRecord.CalibFactor).Y - currentRecord.CenterLeftY());
            }
            else if (e.Button == MouseButtons.Left)
            {
                currentRecord.SetRight(currentFrame, ((PointF)e.Location).Scale(currentRecord.CalibFactor));
                chart1.Series[1].Points.Remove(chart1.Series[1].Points[currentFrame]);
                chart1.Series[1].Points.InsertXY(currentFrame, currentFrame, ((PointF)e.Location).Scale(currentRecord.CalibFactor).X - currentRecord.CenterRightX());

                chart2.Series[1].Points.Remove(chart2.Series[1].Points[currentFrame]);
                chart2.Series[1].Points.InsertXY(currentFrame, currentFrame, ((PointF)e.Location).Scale(currentRecord.CalibFactor).Y - currentRecord.CenterRightY());
            }

            this.ShowFrame(currentFrame);

            try
            {
                currentRecord.Save();
            }
            catch (Exception ex)
            {
                Logging.Log(ex, $"pbxColorImage_MouseClick(object sender, MouseEventArgs e) recordID: {currentRecord?.Id}");
                MessageBox.Show("Не могу записать значения в файл", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnCalc_Click(object sender, EventArgs e)
        {
            if (currentRecord == null)
            {
                return;
            }

            double vybrosy;
            if (!double.TryParse(txtVibrosy.Text, out vybrosy) || vybrosy <= 0)
            {
                MessageBox.Show("Введите корректное значение фактора интерполяции", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cnt = currentRecord.Interpolate(vybrosy);

            try
            {
                currentRecord.Save();
                MessageBox.Show($"Было изменено значений: {cnt}", "Выполнено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logging.Log(ex, $"btnCalc_Click(object sender, EventArgs e) recordID: {currentRecord?.Id}");
                MessageBox.Show("Не могу записать значения в файл", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            this.ShowFrame(0);
            this.RedrawGraph(currentRecord);
        }

        private void btnAnalize_Click(object sender, EventArgs e)
        {
            if (listBoxPatients.SelectedIndex < 0 || dataGrid.CurrentCell.RowIndex < 0)
            {
                return;
            }

            var record = patients[listBoxPatients.SelectedIndex].Records[dataGrid.CurrentCell.RowIndex];

            try
            {
                Forms.FormDetect form = new Forms.FormDetect(record);
                form.FormClosed += Form_FormClosed;
                if (form.Visible)
                {
                    this.Visible = false;
                }
            }
            catch (Exception ex)
            {
                this.Visible = true;
            }
        }

        private void btnDeleteRecord_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите удалить данную запись?", "Вы уверены?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    currentRecord.Delete();
                }
                catch (Exception ex)
                {
                    Logging.Log(ex, $"btnDeleteRecord_Click(object sender, EventArgs e) recordID: {currentRecord?.Id}");
                    MessageBox.Show("Ошибка при удалении", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Запись удалена", "Удалено", MessageBoxButtons.OK, MessageBoxIcon.Information);

                patients[listBoxPatients.SelectedIndex].Records.Remove(patients[listBoxPatients.SelectedIndex].Records[dataGrid.CurrentRow.Index]);

                dataGrid.Rows.Remove(dataGrid.SelectedRows[0]);

                if (dataGrid.RowCount > 0)
                {
                    dataGrid.CurrentCell = dataGrid.Rows[0].Cells[0];
                    dataGrid.Rows[0].Selected = true;
                    this.dataGrid_SelectionChanged(null, null);
                }
                else
                {
                    this.listBoxPatients_SelectedIndexChanged(null, null);
                }

            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.grabber.ReleaseCapture();
        }

        #region append funcs

        private void ShowMainForm(object sender, FormClosedEventArgs e)
        {
            this.Visible = true;
        }

        private void HideMainForm(object sender, EventArgs e)
        {
            this.Visible = false;
        }

        private void EnabledVideoProcessButtons(bool enabled)
        {
            this.btnCalc.Enabled = this.btnNext.Enabled = btnPrev.Enabled = this.btnSave.Enabled = this.btnReport.Visible = this.btnRedrawGraph.Enabled = btnXYShow.Enabled = btnSmooth.Enabled = btnBackUp.Enabled = txtVibrosy.Enabled = enabled;
        }

        private void EnabledRecordButtons(bool enabled)
        {
            //this.btnAnalize.Enabled = this.btnDeleteRecord.Enabled = enabled;
            if (!enabled)
            {
                this.EnabledVideoProcessButtons(false);
            }
        }

        private void EnabledPatientButtons(bool enabled)
        {
            this.btnDeletePatient.Enabled = this.btnEditPatient.Enabled = enabled;
            if (!enabled)
            {
                this.EnabledRecordButtons(false);
            }
        }

        private void ShowPatientRecords(bool visible)
        {
            if (!visible)
            {
                this.ShowRecordDetail(false);
            }

            this.EnabledRecordButtons(visible);
            this.pnlMain.Visible = visible;
        }

        private void ShowRecordDetail(bool visible)
        {
            if (visible)
            {
                this.ShowPatientRecords(true);
            }

            this.EnabledVideoProcessButtons(visible);
            this.splitContainer1.Panel2.Visible = visible;
        }


        private void FormMain_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (!splitContainer1.Panel2.Visible || currentRecord == null || currentRecord.Length <= 2)
            {
                return;
            }

            if (e.KeyCode == Keys.Left)
            {
                if (currentFrame <= 0)
                {
                    return;
                }

                btnNext.Enabled = true;
                ShowFrame(--currentFrame);
            }
            else
            {
                if (e.KeyCode == Keys.Right)
                {
                    if (currentFrame + 1 >= currentRecord.Length)
                    {
                        return;
                    }

                    btnPrev.Enabled = true;
                    ShowFrame(++currentFrame);
                }
            }
        }

        #endregion

        private void btnBack_Click(object sender, EventArgs e)
        {
            pnlCameraIndex.Visible = false;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (currentRecord != null)
            {
                SaveFileDialog sfd = new SaveFileDialog();
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        currentRecord.Save(sfd.FileName);
                        MessageBox.Show("Данные экспортированы в текстовый файл", "Выполнено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(ex, $"btnSave_Click(object sender, EventArgs e) currentRecord.Save({sfd.FileName})");
                        MessageBox.Show("Ошибка при сохранении", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

            }
        }

        private void btnRedrawGraph_Click(object sender, EventArgs e)
        {
            if (currentRecord == null)
            {
                return;
            }

            this.RedrawGraph(currentRecord);
        }

        private void btnSmooth_Click(object sender, EventArgs e)
        {
            if (currentRecord == null)
            {
                return;
            }

            int cnt = currentRecord.Interpolate();

            try
            {
                currentRecord.Save();
                MessageBox.Show($"Сглаживание выполнено", "Выполнено", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logging.Log(ex, $"btnSmooth_Click(object sender, EventArgs e) recordID: {currentRecord?.Id}");
                MessageBox.Show("Не могу записать значения в файл", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            this.ShowFrame(0);
            this.RedrawGraph(currentRecord);
        }

        private void btnBackUp_Click(object sender, EventArgs e)
        {
            if (currentRecord.IsBackupEnabled && MessageBox.Show("Вы действительно хотите установить значения, которые были перед последней интерполяцией?", "Вы уверены?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                currentRecord.BackUp();
                try
                {
                    currentRecord.Save();
                    MessageBox.Show("Восстановлены значения перед последней интерполяцией");
                }
                catch (Exception ex)
                {
                    Logging.Log(ex, $"btnBackUp_Click(object sender, EventArgs e) recordID: {currentRecord?.Id}");
                    MessageBox.Show("Не могу записать значения в файл", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                this.ShowFrame(0);
                this.RedrawGraph(currentRecord);
            }
        }

        private void btnXYShow_Click(object sender, EventArgs e)
        {
            Forms.PlotXY plot = new Forms.PlotXY(currentRecord);
            plot.Show();
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Width = pnlCenterDockFill.Width;
            if (pnlCenterDockFill.VerticalScroll.Visible)
            {
                splitContainer1.Width -= 20;
            }

            splitContainer1.Height = pnlMain.Height - statusStrip1.Height;
        }

        private void btnChangeVideo_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                currentRecord.Path = ofd.FileName;
                currentRecord.UpdateDB();
                dataGrid_SelectionChanged(null, null);
            }
        }

        private void ReplaceWordStub(string stubToReplace, string text, Word.Document wordDocumetn)
        {
            var range = wordDocumetn.Content;
            range.Find.ClearFormatting();
            range.Find.Execute(FindText: stubToReplace, ReplaceWith: text);
        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Docx files (*.docx)|*.docx";
            sfd.FilterIndex = 1;

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            var wordApp = new Word.Application();

            try
            {
                wordApp.Visible = false;

                var wordDocument = wordApp.Documents.Open($"{Directory.GetCurrentDirectory()}//data//temp.docx");

                var patient = patients[listBoxPatients.SelectedIndex];

                var leftPeaks = currentRecord.PeaksLeft();
                var rightPeaks = currentRecord.PeaksRight();
                var maxL = currentRecord.GetMaxPeakToPeak(leftPeaks);
                var maxR = currentRecord.GetMaxPeakToPeak(rightPeaks);
                var minL = currentRecord.GetMinPeakToPeak(leftPeaks);
                var minR = currentRecord.GetMinPeakToPeak(rightPeaks);

                this.ReplaceWordStub("{SecondName}", patient.SecondName, wordDocument);
                this.ReplaceWordStub("{Name}", patient.Name, wordDocument);
                this.ReplaceWordStub("{BirthDay}", patient.BirthDate.ToString("dd.MM.yyyy"), wordDocument);
                this.ReplaceWordStub("{Sex}", patient.IsFemale ? "женский" : "мужской", wordDocument);
                this.ReplaceWordStub("{Comments}", patient.Comments, wordDocument);
                this.ReplaceWordStub("{Date}", $"[{currentRecord.Id}] {currentRecord.Date.ToString("dd.MM.yyyy")}", wordDocument);
                this.ReplaceWordStub("{Path}", currentRecord.Path, wordDocument);
                this.ReplaceWordStub("{VideoComments}", currentRecord.Comments, wordDocument);
                this.ReplaceWordStub("{MaxAmpl}", $"Левый: {Math.Round(maxL, 2)} мм; Правый: {Math.Round(maxR, 2)} мм", wordDocument);
                this.ReplaceWordStub("{MinAmpl}", $"Левый: {Math.Round(minL, 2)} мм; Правый: {Math.Round(minR, 2)} мм", wordDocument);
                this.ReplaceWordStub("{MeanAmpl}", $"Левый: {Math.Round(currentRecord.GetMeanAmplitude(leftPeaks),2)} мм \n Правый: {Math.Round(currentRecord.GetMeanAmplitude(rightPeaks),2)} мм", wordDocument);
                this.ReplaceWordStub("{Freq}", $"{Math.Round(currentRecord.GetFreq(), 3)} Гц", wordDocument);

                wordDocument.SaveAs2(sfd.FileName);

                var cellRange = wordDocument.Tables[2].Cell(1, 1).Range;
                this.SaveChartToFile(chart1, $"{Directory.GetCurrentDirectory()}//temp.jpeg", currentRecord.data, x => x.X, currentRecord.CenterLeftX(), currentRecord.CenterRightX(), 900);
                var bit = Bitmap.FromFile($"{Directory.GetCurrentDirectory()}//temp.jpeg");
                bit.RotateFlip(RotateFlipType.Rotate270FlipNone);
                bit.Save($"{Directory.GetCurrentDirectory()}//temp.jpeg");
                bit.Dispose();
                cellRange.InlineShapes.AddPicture($"{Directory.GetCurrentDirectory()}//temp.jpeg", Type.Missing, Type.Missing, Type.Missing);
                File.Delete($"{Directory.GetCurrentDirectory()}//temp.jpeg");

                cellRange = wordDocument.Tables[2].Cell(1, 2).Range;
                this.SaveChartToFile(chart2, $"{Directory.GetCurrentDirectory()}//temp.jpeg", currentRecord.data, x => x.Y, currentRecord.CenterLeftY(), currentRecord.CenterRightY(), 900);
                bit = Bitmap.FromFile($"{Directory.GetCurrentDirectory()}//temp.jpeg");
                bit.RotateFlip(RotateFlipType.Rotate270FlipNone);
                bit.Save($"{Directory.GetCurrentDirectory()}//temp.jpeg");
                bit.Dispose();
                cellRange.InlineShapes.AddPicture($"{Directory.GetCurrentDirectory()}//temp.jpeg", Type.Missing, Type.Missing, Type.Missing);
                File.Delete($"{Directory.GetCurrentDirectory()}//temp.jpeg");

                cellRange = wordDocument.Tables[3].Cell(1, 1).Range;
                Forms.PlotXY.SavePlotXYToFile(currentRecord, $"{Directory.GetCurrentDirectory()}//temp.jpeg", 600, 600);
                cellRange.InlineShapes.AddPicture($"{Directory.GetCurrentDirectory()}//temp.jpeg", Type.Missing, Type.Missing, Type.Missing);
                File.Delete($"{Directory.GetCurrentDirectory()}//temp.jpeg");

                wordApp.Visible = true;
            }
            catch (Exception ex)
            {
                Logging.Log(ex, "btnReport_Click(object sender, EventArgs e)");
                MessageBox.Show("Ошибка при создании отчета", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                wordApp.Quit();
            }
            finally
            {
            }
        }

        private void SaveChartToFile(Chart chart, string file, List<DataStruct> record, Func<PointF, float> predicate, float centerLeft, float centerRight, int width = 0)
        {
            var oldWidth = chart.Width;
            if (width > 0 && oldWidth > width)
            {
               chart.Width = width;
            }

            chart.Series[0].Points.Clear();
            chart.Series[1].Points.Clear();


            foreach (var item in record)
            {
                chart.Series[0].Points.AddXY(Math.Round(item.msecs / 1000.0, 3), predicate(item.left) - centerLeft);
                chart.Series[1].Points.AddXY(Math.Round(item.msecs / 1000.0, 3), predicate(item.right) - centerRight);
            }

            chart.ChartAreas[0].AxisX.Title = "Время, c";
            chart.SaveImage(file, ChartImageFormat.Jpeg);

            chart.Series[0].Points.Clear();
            chart.Series[1].Points.Clear();

            chart.Width = oldWidth;

            int i = 0;
            foreach (var item in record)
            {
                chart.Series[0].Points.AddXY(i, predicate(item.left) - centerLeft);
                chart.Series[1].Points.AddXY(i++, predicate(item.right) - centerRight);
            }

            chart.ChartAreas[0].AxisX.Title = "№ кадра";
        }

        private void btnChartXSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Png files (*.jpeg)|*.jpeg";
            sfd.FilterIndex = 1;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                this.SaveChartToFile(chart1, sfd.FileName, currentRecord.data, x => x.X, currentRecord.CenterLeftX(), currentRecord.CenterRightX());
            }
        }

        private void btnChartYSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Png files (*.jpeg)|*.jpeg";
            sfd.FilterIndex = 1;
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                this.SaveChartToFile(chart1, sfd.FileName, currentRecord.data, x => x.Y, currentRecord.CenterLeftY(), currentRecord.CenterRightY());
            }
        }
    }
}
