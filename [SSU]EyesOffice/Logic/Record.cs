using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _SSU_EyesOffice.Logic
{
    public struct DataStruct
    {
        public  PointF left;
        public  PointF right;
        public  int msecs;

        public DataStruct(PointF left, PointF right, int msecs)
        {
            this.left = left;
            this.right = right;
            this.msecs = msecs;
        }
    }

    public class Record
    {
        public static List<Record> SelectAllFromDB(int patientId)
        {
            OleDbCommand cmd = new OleDbCommand(@"
                SELECT ID, patient_id, path, video_date, comments, reduce_factor, calib_factor FROM Records WHERE patient_id = @PatientId
                ORDER BY ID DESC",
                DataBase.Instance.conn);
            cmd.Parameters.AddWithValue("@PatientId", patientId);
            var records = new List<Record>();
            try
            {
                DataBase.Instance.Connect();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add(new Record(
                            (int)reader["ID"],
                            (int)reader["patient_id"],
                            reader["path"].ToString(),
                            (DateTime)reader["video_date"],
                            reader["comments"].ToString(),
                            (int)reader["reduce_factor"],
                            (double)reader["calib_factor"]));
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

            return records;
        }

        public static void AddToDB(int patientId, string path, DateTime date, string comments = "0", int reduceFactor = 0, double calibFactor = 0)
        {
            try
            {
                DataBase.Instance.Connect();
                OleDbCommand cmd = new OleDbCommand(
                    @"INSERT INTO Records(patient_id, path, video_date, reduce_factor, calib_factor, comments) 
                    VALUES(@PatientId, @Path, @DateT,@ReduceFactor, @CalibFactor, @Comments)", DataBase.Instance.conn);
                cmd.Parameters.AddWithValue("@PatientId", patientId);
                cmd.Parameters.AddWithValue("@Path", path);
                cmd.Parameters.AddWithValue("@DateT", date.ToString("dd.MM.yyyy hh:mm:ss"));
                cmd.Parameters.AddWithValue("@ReduceFactor", reduceFactor);
                cmd.Parameters.AddWithValue("@CalibFactor", calibFactor);
                cmd.Parameters.AddWithValue("@Comments", comments);

                if (cmd.ExecuteNonQuery() <= 0)
                {
                    throw new Exception("Record.AddToDB null query");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                DataBase.Instance.Disconnect();
            }
        }

        public List<DataStruct> data { get; set; } = new List<DataStruct>();
        private List<DataStruct> backUp = null;

        public Record(int id, int patient_id, string path, DateTime date, string comments = "", int reduce_factor = 0, double calib_factor = 0)
        {
            this.Id = id;
            this.PatientId = patient_id;
            this.Path = path;
            this.Date = date;
            this.ReduceFactor = reduce_factor;
            this.CalibFactor = calib_factor;
            this.Comments = comments;
        }

        public DataStruct this[int frameNumber]
        {
            get
            {
                if (frameNumber < 0 || frameNumber >= this.data.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return this.data[frameNumber];
            }

            set
            {
                if (frameNumber < 0 || frameNumber >= this.data.Count)
                {
                    throw new IndexOutOfRangeException();
                }

                this.data[frameNumber] = value;
            }
        }

        public int Length
        {
            get
            {
                return this.data.Count;
            }
        }

        public void Delete()
        {
            try
            {
                DataBase.Instance.Connect();
                OleDbCommand cmd = new OleDbCommand(
                   @"DELETE FROM Records WHERE ID = @Id", DataBase.Instance.conn);
                cmd.Parameters.AddWithValue("@Id", this.Id);

                if (cmd.ExecuteNonQuery() <= 0)
                {
                    throw new Exception("Record.DeleteRecord null query");
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

            try
            {
                File.Delete($"data//{this.Id}.dat");
            }
            catch
            {

            }
        }

        public int Id { get; private set; }

        public int PatientId { get; private set; }

        public string Path { get; set; }

        public DateTime Date { get; private set; }

        public int ReduceFactor { get; private set; }

        public double CalibFactor { get; set; }

        public string Comments { get; set; }

        public void SetData(List<DataStruct> dat)
        {
            this.data = dat;
        }

        public PointF GetLeft(int frame)
        {
            try
            {
                return new PointF((float)(this.data[frame].left.X / this.CalibFactor), (float)(this.data[frame].left.Y / this.CalibFactor));
            }
            catch
            {
                return PointF.Empty;
            }
        }

        public static float CenterLeftX(List<DataStruct> data)
        {
            if (data.Count <= 0)
            {
                return 0;
            }

            var min = data[0].left.X;
            foreach (var item in data.Skip(1))
            {
                if (min > item.left.X && item.left.X > 0)
                {
                    min = item.left.X;
                }
            }


            var max = data[0].left.X;
            foreach (var item in data.Skip(1))
            {
                if (max < item.left.X)
                {
                    max = item.left.X;
                }
            }

            return (max + min) / 2.0F;
        }

        public float CenterLeftX()
        {
            return Record.CenterLeftX(this.data);
        }

        public static float CenterRightX(List<DataStruct> data)
        {
            if (data.Count <= 0)
            {
                return 0;
            }

            var min = data[0].right.X;
            foreach (var item in data.Skip(1))
            {
                if (min > item.right.X && item.right.X > 0)
                {
                    min = item.right.X;
                }
            }

            var max = data[0].right.X;
            foreach (var item in data.Skip(1))
            {
                if (max < item.right.X)
                {
                    max = item.right.X;
                }
            }

            return (max + min) / 2.0F;
        }

        public float CenterRightX()
        {
            return Record.CenterRightX(this.data);
        }

        public static float CenterLeftY(List<DataStruct> data)
        {
            if (data.Count <= 0)
            {
                return 0;
            }

            var min = data[0].left.Y;
            foreach (var item in data.Skip(1))
            {
                if (min > item.left.Y && item.left.Y > 0)
                {
                    min = item.left.Y;
                }
            }


            var max = data[0].left.Y;
            foreach (var item in data.Skip(1))
            {
                if (max < item.left.Y)
                {
                    max = item.left.Y;
                }
            }

            return (max + min) / 2.0F;
        }

        public float CenterLeftY()
        {
            return Record.CenterLeftY(this.data);
        }

        public static float CenterRightY(List<DataStruct> data)
        {
            if (data.Count <= 0)
            {
                return 0;
            }

            var min = data[0].right.Y;
            foreach (var item in data.Skip(1))
            {
                if (min > item.right.Y && item.right.Y > 0)
                {
                    min = item.right.Y;
                }
            }

            var max = data[0].right.Y;
            foreach (var item in data.Skip(1))
            {
                if (max < item.right.Y)
                {
                    max = item.right.Y;
                }
            }

            return (max + min) / 2.0F;
        }

        public float CenterRightY()
        {
            return Record.CenterRightY(this.data);
        }

        public PointF GetRight(int frame)
        {
            try
            {
                return new PointF((float)(this.data[frame].right.X / this.CalibFactor), (float)(this.data[frame].right.Y / this.CalibFactor));
            }
            catch
            {
                return PointF.Empty;
            }
        }

        public void SetLeft(int frame, PointF left)
        {
            var tmp = this.data[frame];
            tmp.left = left;
            this.data[frame] = tmp;
        }

        public void SetRight(int frame, PointF right)
        {
            var tmp = this.data[frame];
            tmp.right = right;
            this.data[frame] = tmp;
        }

        public void UpdateDB()
        {
            try
            {
                DataBase.Instance.Connect();
                OleDbCommand cmd = new OleDbCommand(
                    @"UPDATE Records SET 
                        patient_id = @PatientId,
                        path = @Path, 
                        video_date = @DateT, 
                        reduce_factor = @ReduceFactor, 
                        calib_factor = @CalibFactor, 
                        comments = @Comments
                    WHERE ID = @Id", DataBase.Instance.conn);

                cmd.Parameters.AddWithValue("@PatientId", this.PatientId);
                cmd.Parameters.AddWithValue("@Path", this.Path);
                cmd.Parameters.AddWithValue("@DateT", this.Date.ToString("dd.MM.yyyy hh:mm:ss"));
                cmd.Parameters.AddWithValue("@ReduceFactor", this.ReduceFactor);
                cmd.Parameters.AddWithValue("@CalibFactor", this.CalibFactor);
                cmd.Parameters.AddWithValue("@Comments", this.Comments);
                cmd.Parameters.AddWithValue("@Id", this.Id);

                if (cmd.ExecuteNonQuery() <= 0)
                {
                    throw new Exception("Record.UpdateDB bad query");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                DataBase.Instance.Disconnect();
            }
        }

        public void Save(String fileName = "")
        {
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }

            using (StreamWriter sw = new StreamWriter(string.IsNullOrEmpty(fileName) ? $"data//{this.Id}.dat" : fileName))
            {
                foreach (var item in this.data)
                {
                    sw.WriteLine($"{item.msecs}\t{item.left.X}\t{item.left.Y}\t{item.right.X}\t{item.right.Y}");
                }
            }
        }

        public void Open()
        {
            this.data.Clear();
            using (StreamReader sr = new StreamReader($"data//{this.Id}.dat"))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine().Split('\t');
                    this.data.Add(new DataStruct(
                        new PointF(float.Parse(line[1]), float.Parse(line[2])),
                        new PointF(float.Parse(line[3]), float.Parse(line[4])),
                        int.Parse(line[0])
                        ));
                }
            }
        }

        public void BackUp()
        {
            if (backUp == null)
            {
                throw new NullReferenceException();
            }

            data.Clear();
            foreach (var item in this.backUp)
            {
                data.Add(item);
            }

            backUp = null;
        }

        public bool IsBackupEnabled {
            get
            {
                return backUp != null;
            }
        }

        public static int Interpolate(List<DataStruct> list, double vibrosFactor)
        {
            int cnt = 0;

            float[] dxArr = new float[list.Count - 1];

            for (int i = 0; i < list.Count - 1; i++)
            {
                dxArr[i] = Math.Abs(list[i + 1].left.X - list[i].left.X);
            }

            var mediane_dx = dxArr.Mediane(); // медиана изменения значений между соседними точками

            for (int i = 0; i < list.Count - 1; i++)
            {
                PointF backL = PointF.Empty;
                PointF backR = PointF.Empty;
                bool isVibros = (Math.Abs(list[i + 1].left.X - list[i].left.X) > mediane_dx * vibrosFactor) || (Math.Abs(list[i + 1].right.X - list[i].right.X) > mediane_dx * vibrosFactor);

                if (list[i].left.X == 0 || list[i].right.X == 0 || isVibros)
                {
                    if (i > 0)
                    {
                        backL = list[i - 1].left;
                        backR = list[i - 1].right;
                    }

                    for (int j = i + 1; j < list.Count - 1; j++)
                    {
                        bool isVibros2 = (Math.Abs(list[j + 1].left.X - list[j].left.X) > mediane_dx * vibrosFactor) || (Math.Abs(list[j + 1].right.X - list[j].right.X) > mediane_dx * vibrosFactor);

                        if (list[j].left.X != 0 && list[j].right.X != 0 && (vibrosFactor == 1 || !isVibros2))
                        {
                            for (int k = i; k < j; k++)
                            {
                                cnt++;
                                PointF L, R;
                                if (backL.X == 0)
                                {
                                    L = list[j].left;
                                }
                                else
                                {
                                    L = new PointF((float)(backL.X + list[j].left.X) / 2, (float)(backL.Y + list[j].left.Y) / 2);
                                }

                                if (backR.X == 0)
                                {
                                    R = list[j].right;
                                }
                                else
                                {
                                    R = new PointF((float)(backR.X + list[j].right.X) / 2, (float)(backR.Y + list[j].right.Y) / 2);
                                }

                                list[k] = new DataStruct(L, R, list[k].msecs);
                            }

                            break;
                        }
                    }
                }
            }

            return cnt;
        }

        public int Interpolate(double vibrosFactor = 1)
        {
            backUp = new List<DataStruct>();
            backUp.Clear();
            foreach (var item in this.data)
            {
                backUp.Add(item);
            }

            return Record.Interpolate(this.data, vibrosFactor);
        }

        public float GetFreq()
        {
            int cnt = 0;
            var centerX = this.CenterLeftX();
            for (int i = 1; i < this.data.Count; i++)
            {
                if ((this.data[i - 1].left.X - centerX) * (this.data[i].left.X - centerX) < 0)
                {
                    cnt++;
                }
            }

            return (float)cnt / (float)(2 * (this.data[this.data.Count - 1].msecs / 1000));
        }

        public float GetMaxAmplitudeLeft()
        {
            float min = 0;
            foreach (var item in data)
            {
                if ((min > item.left.X || min == 0))
                {
                    min = item.left.X;
                }
            }

            float max = 0;
            foreach (var item in data)
            {
                if ((max < item.left.X || max == 0))
                {
                    max = item.left.X;
                }
            }

            return max - min;
        }

        public float GetMaxAmplitudeRight()
        {
            float min = 0;
            foreach (var item in data)
            {
                if ((min > item.right.X || min == 0))
                {
                    min = item.right.X;
                }
            }

            float max = 0;
            foreach (var item in data)
            {
                if ((max < item.right.X || max == 0))
                {
                    max = item.right.X;
                }
            }

            return max - min;
        }

        public float GetMaxPeakToPeak(List<float> peaks)
        {
            float min = -1000000;
            float max = -1000000;
            foreach (var item in peaks)
            {
               
                if (min < Math.Abs(item) && item < 0)
                {
                    min = Math.Abs(item);
                }

                if (max < Math.Abs(item) && item > 0)
                {
                    max = Math.Abs(item);
                }
            }

            return max + min;
        }

        public float GetMinPeakToPeak(List<float> peaks)
        {
            float min = 1000000;
            float max = 1000000;
            foreach (var item in peaks)
            {

                if (min > Math.Abs(item) && item < 0)
                {
                    min = Math.Abs(item);
                }

                if (max > Math.Abs(item) && item > 0)
                {
                    max = Math.Abs(item);
                }
            }

            return max + min;
        }

        public float GetMeanAmplitude(List<float> peaks)
        {
           var peakMax = new List<float>();
           var peakMin = new List<float>();

            foreach (var item in peaks)
            {
                if (item > 0)
                {
                    peakMax.Add(item); 
                }

                if (item < 0)
                {
                    peakMin.Add(item);
                }
            }

            if (peakMax.Count > peakMin.Count)
            {
                peakMax.RemoveRange(0, peakMax.Count - peakMin.Count);
            }
            else if (peakMax.Count < peakMin.Count)
            {
                peakMin.RemoveRange(0, peakMin.Count - peakMax.Count);
            }

            return (Math.Abs(peakMin.Sum()) + Math.Abs(peakMax.Sum())) / peakMin.Count;
            
        }

        public List<float> PeaksLeft()
        {
            var arr = new List<float>();
            float max = 0;
            var center = CenterLeftX();
            for (int i = 1; i < data.Count - 1; i++)
            {
                if ((data[i - 1].left.X - center) * (data[i].left.X - center) < 0)
                {
                    arr.Add(max - center);
                    max = 0;
                }
                else
                {
                    if (data[i].left.X - center > 0)
                    { // find max
                        if ((max == 0) || max < data[i].left.X)
                        {
                            max = data[i].left.X;
                        }
                    }
                    else
                    {// min
                        if ((max == 0) || max > data[i].left.X)
                        {
                            max = data[i].left.X;
                        }
                    }

                }
            }

            arr.RemoveAt(0);
            arr.RemoveAt(arr.Count - 1);

            return arr;
        }

        public List<float> PeaksRight()
        {
            var arr = new List<float>();
            float max = 0;
            var center = CenterRightX();
            for (int i = 1; i < data.Count - 1; i++)
            {
                if ((data[i - 1].right.X - center) * (data[i].right.X - center) < 0)
                {
                    arr.Add(max - center);
                    max = 0;
                }
                else
                {
                    if (data[i].right .X - center > 0)
                    { // find max
                        if ((max == 0) || max < data[i].right.X)
                        {
                            max = data[i].right.X;
                        }
                    }
                    else
                    {// min
                        if ((max == 0) || max > data[i].right.X)
                        {
                            max = data[i].right.X;
                        }
                    }

                }
            }

            arr.RemoveAt(0);
            arr.RemoveAt(arr.Count - 1);

            return arr;
        }

    }
}
