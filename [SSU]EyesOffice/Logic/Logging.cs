using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _SSU_EyesOffice.Logic
{
    public static class Logging
    {
        public static void Log(Exception ex, string message = "")
        {
            File.AppendAllText("logger.log", $"[{DateTime.Now}]: {message}{Environment.NewLine}###{Environment.NewLine}{ex.Message}{Environment.NewLine}###{Environment.NewLine}{ex.ToString()}{Environment.NewLine}{Environment.NewLine}############################################################################{Environment.NewLine}--------------{Environment.NewLine}############################################################################{Environment.NewLine}{Environment.NewLine}");
        }
    }
}
