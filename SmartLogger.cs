//******************************************************************************************
// Copyright © 2017 Wolfgang Foerster (wolfoerster@gmx.de)
//
// This file is part of the SmartLogging project which can be found on github.com
//
// SmartLogging is free software: you can redistribute it and/or modify it under the terms 
// of the GNU General Public License as published by the Free Software Foundation, 
// either version 3 of the License, or (at your option) any later version.
// 
// SmartLogging is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//******************************************************************************************
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Reflection;
using System.IO;

namespace SmartLogging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal,
        None
    }

    public class SmartLogger
    {
        /// <summary>
        /// Initialize logging to either a rolling log file or based on a config file.
        /// </summary>
        /// <param name="rollingFileName">The rolling log file name. If no name is given, configuration is done via config file.</param>
        /// <param name="maxFileSize">The maximum log file size</param>
        /// <param name="maxNumberOfBackups">The maximum number of backup files</param>
        public static void Init(string rollingFileName = null, string maxFileSize = "10MB", int maxNumberOfBackups = 1)
        {
            if (string.IsNullOrWhiteSpace(rollingFileName))
            {
                InitByConfigFile(Assembly.GetCallingAssembly().Location);
            }
            else
            {
                InitRollingFileAppender(rollingFileName, maxFileSize, maxNumberOfBackups);
            }
        }

        private static void InitRollingFileAppender(string fileName, string maximumFileSize, int maxSizeRollBackups)
        {
            Trace.WriteLine($"Configure rolling file appender: fileName = '{fileName}', maximumFileSize = {maximumFileSize}");
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Root.RemoveAllAppenders(); /*Remove any other appenders*/

            var fileAppender = new RollingFileAppender();
            fileAppender.RollingStyle = RollingFileAppender.RollingMode.Size;
            fileAppender.MaxSizeRollBackups = maxSizeRollBackups;
            fileAppender.MaximumFileSize = maximumFileSize;
            fileAppender.StaticLogFileName = true;
            fileAppender.AppendToFile = true;
            fileAppender.LockingModel = new FileAppender.MinimalLock();
            fileAppender.File = fileName;
            //PatternLayout pl = new PatternLayout { ConversionPattern = "%utcdate{yyyy-MM-dd HH:mm:ss.ffffff} %level %logger %message%newline%exception" };
            //--- there is no use in specifying more than 3 digits for the seconds, because the resolution will only be one millisecond!!!
            PatternLayout pl = new PatternLayout { ConversionPattern = "%utcdate{yyyy-MM-dd HH:mm:ss.fff} %level %logger %message%newline%exception" };
            pl.ActivateOptions();
            fileAppender.Layout = pl;
            fileAppender.ActivateOptions();

            log4net.Config.BasicConfigurator.Configure(fileAppender);
        }

        private static void InitByConfigFile(string exe)
        {
            string configFile = exe + ".config";
            if (File.Exists(configFile))
            {
                Trace.WriteLine($"Reading configuration from file: '{configFile}'");
                var fileInfo = new FileInfo(configFile);
                log4net.Config.XmlConfigurator.ConfigureAndWatch(fileInfo);
            }
            else
            {
                Trace.WriteLine($"Configuration file does not exist: '{configFile}'");
            }
        }

        public static bool TraceLogging
        {
            get { return mySmartTraceListener != null; }
            set
            {
                if (value == true && mySmartTraceListener == null)
                {
                    mySmartTraceListener = new SmartTraceListener();
                    Trace.Listeners.Add(mySmartTraceListener);
                }

                if (value == false && mySmartTraceListener != null)
                {
                    Trace.Listeners.Remove(mySmartTraceListener);
                }
            }
        }
        private static SmartTraceListener mySmartTraceListener;

        public static bool InternalLogging
        {
            get { return log4net.Util.LogLog.InternalDebugging; }
            set
            {
                if (log4net.Util.LogLog.InternalDebugging != value)
                {
                    log4net.Util.LogLog.InternalDebugging = value;
                }
            }
        }

        public SmartLogger(string name = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                StackTrace stackTrace = new StackTrace();
                name = stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName;
            }

            log = LogManager.GetLogger(name);

            using (Process process = Process.GetCurrentProcess())
            {
                ProcessId = process.Id;
            }
            AppDomainId = AppDomain.CurrentDomain.Id;
        }

        private ILog log;
        public int AppDomainId;
        public int ProcessId;

        private bool CheckLevel(LogLevel level)
        {
            if (log == null)
                return false;

            switch (level)
            {
                case LogLevel.Debug: return log.IsDebugEnabled;
                case LogLevel.Info: return log.IsInfoEnabled;
                case LogLevel.Warn: return log.IsWarnEnabled;
                case LogLevel.Error: return log.IsErrorEnabled;
                case LogLevel.Fatal: return log.IsFatalEnabled;
            }

            return true;
        }

        public void Smart(string message = null, LogLevel level = LogLevel.Debug, Exception ex = null, [CallerMemberName]string methodName = null)
        {
            if (!CheckLevel(level))
                return;

            int threadId = Thread.CurrentThread.ManagedThreadId;
            string str = string.Format("{0}/{1}/{2} {3} {4}", ProcessId, AppDomainId, threadId, methodName, message);

            switch (level)
            {
                case LogLevel.Debug: log.Debug(str, ex); break;
                case LogLevel.Info: log.Info(str, ex); break;
                case LogLevel.Warn: log.Warn(str, ex); break;
                case LogLevel.Error: log.Error(str, ex); break;
                case LogLevel.Fatal: log.Fatal(str, ex); break;
            }
        }

        public void Smart(Func<string> messageFunc, LogLevel level = LogLevel.Debug, Exception ex = null, [CallerMemberName]string methodName = null)
        {
            if (!CheckLevel(level) || messageFunc == null)
                return;

            Smart(messageFunc(), level, ex, methodName);
        }

        public void Smart(object obj, LogLevel level = LogLevel.Debug, Exception ex = null, [CallerMemberName]string methodName = null)
        {
            if (!CheckLevel(level) || obj == null)
                return;

            Smart(obj.ToString(), level, ex, methodName);
        }

        public void Exception(Exception ex, [CallerMemberName]string methodName = null)
        {
            if (!CheckLevel(LogLevel.Error) || ex == null)
                return;

            Smart("EXCEPTION", LogLevel.Error, ex, methodName);
        }

        public void Start([CallerMemberName]string methodName = null)
        {
            Smart("Start logging", LogLevel.Fatal, null, methodName);
        }
    }
}
