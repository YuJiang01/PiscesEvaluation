using System;
using System.IO;


namespace PiscesValidationYjiang2
{
    internal class Logger
    {
        #region members

        private const string LogArchiveDir = "LogArchive";
        private const int MaxLogFileSizeInBytes = 10 * 1024 * 1024;
        private static StreamWriter _sw;
        private static bool _ready;
        private static string _logFilePath = "PiscesValidationLog.txt";

        public static bool GeneralLogReady
        {
            get { return _ready; }
            set { _ready = value; }
        }

        #endregion

        #region opening

        /// <summary>
        /// </summary>
        /// <returns></returns>
        internal static bool TryOpenLog(string logDir)
        {
            lock (typeof(Logger))
            {
                if (!_logFilePath.Contains(logDir))
                    _logFilePath = Path.Combine(logDir, _logFilePath);
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                _sw = new StreamWriter(_logFilePath, true);
                _ready = true;
                _sw.WriteLine();
                Write(("*************starting PiscesValidationLog.exe**************"), _sw, ref _ready);
                return _ready;
            }
        }

        #endregion

        #region closing

        //note, the try-catches for these methods will all happen upstream, in the output class.
        //(The output class has the io-locker, that will allow any errors to be visibly output & logged to the user)
        /// <summary>
        /// </summary>
        /// <returns></returns>
        internal static bool TryCloseLog()
        {
            lock (typeof(Logger))
            {
                if (_ready)
                {
                    Write(("********************ending *********************"), _sw, ref _ready);
                    _sw.Close();
                    _ready = false;
                }

                return (!_ready);
            }
        }

        #endregion

        #region writing to files

        /////////////////////////////////////////////////////////////////

        internal static bool WriteToLog(string message)
        {
            lock (typeof(Logger))
            {
                return Write(message, _sw, ref _ready);
            }
        }


        internal static bool WriteToLog(string message, params object[] args)
        {
            lock (typeof(Logger))
            {
                return Write(string.Format(message, args), _sw, ref _ready);
            }
        }

        internal static bool WriteExceptionToLog(Exception ex)
        {
            lock (typeof(Logger))
            {
                return WriteToLog("Exception reported:  \n" + ex);
            }
        }

        // /////////////////////////////////////////////////////////////////
        private static bool Write(string message, StreamWriter sw, ref bool ready)
        {
            if (!ready) return false;

            try
            {
                message = message.TrimEnd('\n');

                string dot = message.EndsWith(".") || message.EndsWith("*") ? "" : ".";

                message = string.Format(
                    "{0} {1}  {2}{3}",
                    DateTime.Today.ToShortDateString(),
                    DateTime.Now.ToLongTimeString(),
                    message,
                    dot);

              //  Console.WriteLine(message);

                if ((message.ToLower().Contains("error")) ||
                    (message.ToLower().Contains("exception")))
                    Console.Error.WriteLine(message);

                sw.WriteLine(message);
                sw.Flush();
                return true;
            }
            catch
            {
                ready = false;
                return false;
            }
        }

        // /////////////////////////////////////////////////////////////////
        public static bool WriteEmptyLineToGeneralLog()
        {
            lock (typeof(Logger))
            {
                if (_ready) return false;

                try
                {
                    _sw.WriteLine();
                    _sw.Flush();
                    return true;
                }
                catch
                {
                    _ready = false;
                    return false;
                }
            }
        }

        #endregion

        #region backingup files

        /////////////////////////////////////////////////////////////////
        internal static bool BackUpLog()
        {
            lock (typeof(Logger))
            {
                return BackupLogAsNeeded(_logFilePath, ref _sw, ref _ready);
            }
        }


        private static bool BackupLogAsNeeded(string filename, ref StreamWriter sw, ref bool ready)
        {
            string archiveFileName = String.Empty;

            if ((new FileInfo(filename)).Length > MaxLogFileSizeInBytes)
            {
                if (ready)
                {
                    //Write("\n", sw, ref ready);
                    Write(("**************ending ProbePoolQCLog***************"), sw, ref ready);
                    ready = false;
                    sw.Close();
                    archiveFileName = CreateBackup(filename);
                    sw = new StreamWriter(filename, true);
                    ready = true;
                    Write("\n", sw, ref ready);
                    Write(("*************starting ProbePoolQCLog ***************"), sw, ref ready);

                    return true;
                }
            }

            return false;
        }

        private static string CreateBackup(string fileName)
        {
            string archiveFileName = String.Empty;

            FileInfo fileInfo = new FileInfo(fileName);
            archiveFileName = CreateArchiveFileName(fileName);
            fileInfo.MoveTo(archiveFileName);
            File.SetAttributes(archiveFileName, FileAttributes.Archive | FileAttributes.ReadOnly);

            return archiveFileName;
        }

        private static string CreateArchiveFileName(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);

            if (!Directory.Exists(LogArchiveDir))
                Directory.CreateDirectory(LogArchiveDir);

            //make archive file name
            int d = DateTime.Now.Day;
            int mo = DateTime.Now.Month;
            int y = DateTime.Now.Year;

            int h = DateTime.Now.Hour;
            int mi = DateTime.Now.Minute;
            int s = DateTime.Now.Second;

            string dateTime = string.Format("{0}-{1}-{2}_{3}-{4}-{5}", mo, d, y, h, mi, s);

            string archiveFileName = string.Format("{0}_{1}{2}",
                                                   Path.GetFileNameWithoutExtension(fileName),
                                                   dateTime,
                                                   fileInfo.Extension);

            return Path.Combine(LogArchiveDir, archiveFileName);
        }

        #endregion
    }
}

