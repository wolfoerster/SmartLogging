//******************************************************************************************
// Copyright © 2017 - 2021 Wolfgang Foerster (wolfoerster@gmx.de)
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

namespace SmartLogging
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class SmartLogger
    {
        private static readonly long MaxLength = 8 * 1024 * 1024; // new log file at 8 MB
        private static readonly ConcurrentQueue<LogEntry> LogEntries = new ConcurrentQueue<LogEntry>();
        private static readonly object Locker = new Object();
        private static Task writerTask;
        private readonly string className;
        private readonly int appDomainId;
        private readonly int processId;

        public SmartLogger(object context = null)
        {
            if (context == null)
            {
                var stackTrace = new StackTrace();
                var method = stackTrace.GetFrame(1).GetMethod();
                context = method.DeclaringType;
            }

            this.className = GetClassName(context);

            using (var process = Process.GetCurrentProcess())
            {
                this.processId = process.Id;
                this.appDomainId = AppDomain.CurrentDomain.Id;
            }
        }

        public static LogLevel MinimumLogLevel = LogLevel.Information;

        public static string FileName { get; private set; }

        public static void Init(string fileName = null)
        {
            if (writerTask != null)
                return;

            if (fileName == null)
            {
                var name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                FileName = Path.Combine(Path.GetTempPath(), $"{name}.log");
            }
            else
            {
                FileName = fileName;
            }

            writerTask = Task.Run(() => WriterLoop());

            var log = new SmartLogger(typeof(SmartLogger));
            log.None("Start logging");
        }

        public static void Flush()
        {
            // TODO: each logger has a WriterLoop which is processing log entries and which may not be done yet
            Thread.Sleep(30);
        }

        public void Verbose(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Verbose, methodName);
        }

        public void Debug(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Debug, methodName);
        }

        public void Information(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Information, methodName);
        }

        public void Warning(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Warning, methodName);
        }

        public void Error(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Error, methodName);
        }

        public void Fatal(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.Fatal, methodName);
        }

        public void None(object msg = null, [CallerMemberName] string methodName = null)
        {
            this.Write(msg, LogLevel.None, methodName);
        }

        public void Exception(Exception exception, [CallerMemberName] string methodName = null)
        {
            this.Fatal(new { ExceptionMessage = GetMessage(exception), exception.StackTrace }, methodName);
        }

        public void Write(object msg, LogLevel level, [CallerMemberName] string methodName = null)
        {
            if (level < MinimumLogLevel)
                return;

            try
            {
                if (writerTask == null)
                    Init();

                var entry = CreateLogEntry(msg, level, methodName);
                LogEntries.Enqueue(entry);
            }
            catch
            {
            }
        }

        private LogEntry CreateLogEntry(object msg, LogLevel level, string methodName)
        {
            string threadIds = string.Format("{0}/{1}/{2}", this.processId, this.appDomainId, Thread.CurrentThread.ManagedThreadId);

            return new LogEntry
            {
                Time = DateTime.UtcNow.ToString("o"),
                ThreadIds = threadIds,
                Level = level.ToString(),
                Class = this.className,
                Method = methodName,
                Message = GetString(msg),
            };
        }

        private static string GetString(object value)
        {
            if (value is string str)
                return str;

            return ToJson(value);
        }

        private static string ToJson(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.None);
        }

        private static string GetMessage(Exception exception)
        {
            var sb = new StringBuilder();
            sb.Append(exception.Message);

            exception = exception.InnerException;
            while (exception != null)
            {
                sb.Append(" InnerException: ");
                sb.Append(exception.Message);
                exception = exception.InnerException;
            }

            return sb.ToString();
        }

        private static string GetClassName(object context)
        {
            if (context is Type type)
                return type.FullName;

            if (context is string className)
            {
                if (string.IsNullOrWhiteSpace(className))
                    return "-?-";

                return className;
            }

            return context.GetType().FullName;
        }

        #region WriterLoop

        private static void WriterLoop()
        {
            DateTime t0 = DateTime.UtcNow;
            while (true)
            {
                while (LogEntries.TryDequeue(out LogEntry entry))
                {
                    try
                    {
                        lock (Locker)
                        {
                            using (StreamWriter sw = File.AppendText(FileName))
                            {
                                var json = ToJson(entry);
                                sw.WriteLine(json);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        var str = GetMessage(exception);
                        Trace.WriteLine(str);
                    }
                }

                Thread.Sleep(30);

                if ((DateTime.UtcNow - t0).TotalSeconds > 10)
                {
                    try
                    {
                        CheckFileSize();
                    }
                    catch (Exception exception)
                    {
                        var str = GetMessage(exception);
                        Trace.WriteLine(str);
                    }

                    t0 = DateTime.UtcNow;
                }
            }
        }

        private static void CheckFileSize()
        {
            if (File.Exists(FileName))
            {
                var fileInfo = new FileInfo(FileName);
                if (fileInfo.Length > MaxLength)
                {
                    var backupName = FileName + ".log";

                    lock (Locker)
                    {
                        File.Delete(backupName);
                        File.Move(FileName, backupName);
                    }

                    var log = new SmartLogger(typeof(SmartLogger));
                    log.None(new { logFileSize = fileInfo.Length, allowedSize = MaxLength, backupName });
                }
            }
        }

        #endregion WriterLoop
    }
}
