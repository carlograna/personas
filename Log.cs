using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Configuration;

namespace PersonLoad
{
    class Log
    {
        public Log(string path)
        {
            int i = 0;
            while (File.Exists(path))
            {
                i++;
                path = String.Format(@"{0}\{1}_{2}{3}", Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path), i.ToString(), Path.GetExtension(path));
            }

            FilePath = path;
            fs = File.Create(FilePath);
            sw = new StreamWriter(fs);
            sb = new StringBuilder();
        }

        private StringBuilder sb;
        private StreamWriter sw { get; set; }
        private FileStream fs { get; set; }

        public string FilePath { get; set; }
        
        
        //private static StreamWriter logWriter;

        public void CreateFile(string path)
        {
            try
            {
                int i = 0;
                while(File.Exists(path))
                {
                    i++;
                    path = String.Format("{0}_{1}", path, i.ToString());
                }

                FilePath = path;
                FileStream fs = File.Create(FilePath);
                sw = new StreamWriter(fs);
            }
            catch
            { throw; }
        }

        //public void WriteLine(string msg)
        //{
        //    sb.AppendLine(msg);

        //    if (sb.ToString().Length > 10000)
        //    {
        //        sw.Write(sb.ToString());
        //        sb.Clear();
        //    }               
                
        //        //sw.WriteLine(msg);
        //}

        //public void LastWrite(){
        //    if (sb.ToString().Length > 0)
        //    {
        //        sw.WriteLine(sb.ToString());
        //    }              
        //}

        public void WriteLine(string msg)
        {
            sw.WriteLine(msg);
        }
            

        public void Close()
        {
            if (sw == null)
                sw = File.AppendText(FilePath);

            sw.Close();
            sw.Dispose();
        }


        //public void Exit(string msg, ExitCode exitCode)
        //{
        //    try
        //    {
        //        bool fileExists = File.Exists(filePath);
        //        if (fileExists)
        //        {
        //            sw.WriteLine(msg);
        //        }
        //    }
        //    finally
        //    {
        //        Close();
        //        //EmailNotification((int)exitCode);
                
        //    }
        //}
    }
}
