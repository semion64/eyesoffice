using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace _SSU_EyesOffice.Logic
{
    public class DataBase
    {
        public OleDbConnection conn;

        private static DataBase instance;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="location">Path to database file</param>
        private DataBase(string location = "db.mdb")
        {
            this.Location = location;
            // Подключаемся к базе данных SQL Server
            this.conn = new OleDbConnection($"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={this.Location}");
        }

        /// <summary>
        /// Use single tone techniques
        /// </summary>
        public static DataBase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DataBase();
                }

                return instance;
            }
        }

        public bool CheckConnection()
        {
            try
            {
                this.Connect();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return false;
            }

            //this.Disconect();
            return true;
        }

        public string Location { get; set; }


        public void Connect()
        {
                     
            try
            {
                this.conn.Open();
            }
            catch(Exception ex)
            {
                throw;
                //throw Global.NewExc("Не могу подключиться к базе данных (Connect)", this);
            }
        }

        public void Disconnect()
        {
            this.conn.Close();
        }

        public int LastId(string table)
        {
            this.Connect();
            int lastId = -1;
            OleDbCommand cmd = new OleDbCommand($"Select TOP 1 ID from {table} ORDER BY ID DESC", this.conn);
            try
            {
                lastId = (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при получении идентификатора (LastId)");
            }
            finally
            {
                this.Disconnect();
            }

            return lastId;
        }
    }
}
