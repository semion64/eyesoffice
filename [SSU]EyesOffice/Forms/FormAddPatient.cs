namespace _SSU_EyesOffice.Forms
{
    using System;
    using System.Windows.Forms;

    public partial class FormAddPatient : Form
    {
        public FormAddPatient()
        {
            InitializeComponent();
            txtSecondName.Select();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSecondName.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Logic.Patient.AddToDB(txtSecondName.Text, txtName.Text, radioButtonWoman.Checked, monthCalendar.SelectionRange.Start, txtComment.Text);

                MessageBox.Show("Пользователь успешно создан", "Создание пользователя", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.Close();
            }
            catch(Exception ex)
            {
                Logic.Logging.Log(ex, $" btnAdd_Click(object sender, EventArgs e) Logic.Patient.AddToDB({txtSecondName.Text}, {txtName.Text}, {radioButtonWoman.Checked}, {monthCalendar.SelectionRange.Start}, {txtComment.Text})");
                MessageBox.Show("Произошла ошибка при записи в БД", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
