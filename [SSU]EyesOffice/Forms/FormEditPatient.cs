namespace _SSU_EyesOffice.Forms
{
    using System;
    using System.Windows.Forms;
    using _SSU_EyesOffice.Logic;

    public partial class FormEditPatient : Form
    {
        private int patientID;
        public FormEditPatient(int patientID)
        {
            InitializeComponent();
            
            this.patientID = patientID;

            var patient = Patient.SelectByID(patientID);
            if (patient != null)
            {
                this.txtName.Text = patient.Name;
                this.txtSecondName.Text = patient.SecondName;
                this.radioButtonWoman.Checked = patient.IsFemale;
                this.radioButtonMan.Checked = !patient.IsFemale;
                this.txtComment.Text = patient.Comments;
                this.monthCalendar.SetDate(patient.BirthDate);

                this.Show();
            }
            else
            {
                MessageBox.Show("Данного пациента не существует", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                this.Close();
            }

            txtSecondName.Select();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.txtSecondName.Text))
                {
                    MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Patient.UpdateToDB(
                    this.patientID,
                    this.txtSecondName.Text,
                    this.txtName.Text,
                    this.radioButtonWoman.Checked,
                    this.monthCalendar.SelectionStart,
                    this.txtComment.Text);
                MessageBox.Show("Изменения сохранены", "Выполнено", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обращении к базе данных", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logging.Log(ex, $"void btnAdd_Click(object sender, EventArgs e); Patient.UpdateToDB({this.patientID}, {this.txtSecondName.Text}, {this.txtName.Text}, {this.radioButtonWoman.Checked}, {this.monthCalendar.SelectionStart}, {this.txtComment.Text});");
            }
        }
    }
}
