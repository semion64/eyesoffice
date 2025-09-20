using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _SSU_EyesOffice.Logic
{
    public class Patient
    {
        private string secondName;

        public Patient(int Id, string secondName, string name, DateTime birthDay, bool isFemale, string comments)
        {
            this.Id = Id;
            this.SecondName = secondName;
            this.Name = name;
            this.BirthDate = birthDay;
            this.IsFemale = isFemale;
            this.Comments = comments;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public string SecondName {
            get
            {
                return this.secondName;
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException();
                }

                this.secondName = value;
            }
        }

        public DateTime BirthDate { get; set; }

        public bool IsFemale  { get; set; }

        public string Comments { get; set; }

        public List<Record> Records {get; set;}

        public void Delete()
        {
            foreach (var item in this.Records)
            {
                item.Delete();
            }

            try
            {
                DataBase.Instance.Connect();
                OleDbCommand cmd = new OleDbCommand(
                   @"DELETE FROM Patients WHERE ID = @Id", DataBase.Instance.conn);
                cmd.Parameters.AddWithValue("@Id", this.Id);

                if (cmd.ExecuteNonQuery() <= 0)
                {
                    throw new Exception("Record.DeletePatient null query");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                DataBase.Instance.Disconnect();
            }
        }

        public static List<Patient> SelectAllFromDB()
        {
            OleDbCommand cmd = new OleDbCommand(@"
                SELECT ID, c_second_name, c_name, c_is_female, c_birth_date, c_comments FROM patients
                ORDER BY ID DESC", 
                DataBase.Instance.conn);

            var patients = new List<Patient>();
            try
            { 
                DataBase.Instance.Connect();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        patients.Add(new Patient(
                            (int) reader["ID"],
                            reader["c_second_name"].ToString(),
                            reader["c_name"].ToString(),
                            (DateTime)reader["c_birth_date"],
                            (bool)reader["c_is_female"],
                            reader["c_comments"].ToString()));
                    }
                }

                DataBase.Instance.Disconnect();
            }
            catch (Exception ex)
            {
                Logging.Log(ex, "Couldn't open connection Patient.SelectAllFromDB");
            }

            return patients;
        }

        public static Patient SelectByID(int id)
        {
            OleDbCommand cmd = new OleDbCommand(@"
                SELECT ID, c_second_name, c_name, c_is_female, c_birth_date, c_comments FROM patients WHERE ID = @ID",
                DataBase.Instance.conn);
            cmd.Parameters.AddWithValue("@ID", id);

            try
            {
                DataBase.Instance.Connect();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var patient = new Patient(
                            (int)reader["ID"],
                            reader["c_second_name"].ToString(),
                            reader["c_name"].ToString(),
                            (DateTime)reader["c_birth_date"],
                            (bool)reader["c_is_female"],
                            reader["c_comments"].ToString());
                        DataBase.Instance.Disconnect();
                        return patient;
                    }
                }

            }
            catch (Exception ex)
            {
                Logging.Log(ex, "Couldn't open connection Patient.SelectAllFromDB");
            }
            finally
            {
                DataBase.Instance.Disconnect();
            }

            return null;
        }

        public static void UpdateToDB(int Id, string secondName, string name, bool isFemale, DateTime birthDay, string comments)
        {
            if (string.IsNullOrWhiteSpace(secondName))
            {
                throw new ArgumentException();
            }

            try
            {
                DataBase.Instance.Connect();
                OleDbCommand cmd = new OleDbCommand(
                    @"UPDATE patients SET 
                    c_second_name = @SecondName,
                    c_name = @Name,
                    c_is_female = @IsFemale,
                    c_birth_date = @birthDay,
                    c_comments = @comments WHERE ID = @Id", DataBase.Instance.conn);
                cmd.Parameters.AddWithValue("@SecondName", secondName);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@IsFemale", isFemale);
                cmd.Parameters.AddWithValue("@birthDay", birthDay);
                cmd.Parameters.AddWithValue("@comments", comments);
                cmd.Parameters.AddWithValue("@Id", Id);

                if (cmd.ExecuteNonQuery() <= 0)
                {
                    throw new Exception("Patient.UpdateToDB");
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                DataBase.Instance.Disconnect();
            }
        }

        public static void AddToDB(string secondName, string name, bool isFemale, DateTime birthDay, string comments)
        {
            if (string.IsNullOrWhiteSpace(secondName))
            {
                throw new ArgumentException();
            }

            try
            {
                DataBase.Instance.Connect();
                OleDbCommand cmd = new OleDbCommand(
                    @"INSERT INTO patients(c_second_name, c_name, c_is_female, c_birth_date, c_comments) 
                    VALUES(@SecondName, @Name, @IsFemale, @birthDay, @comments)", DataBase.Instance.conn);
                cmd.Parameters.AddWithValue("@SecondName", secondName);
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@IsFemale", isFemale);
                cmd.Parameters.AddWithValue("@birthDay", birthDay);
                cmd.Parameters.AddWithValue("@comments", comments);

                if (cmd.ExecuteNonQuery() <= 0)
                {
                    throw new Exception("Patient.AddToDB null query");
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                DataBase.Instance.Disconnect();
            }
        }
    }
}
