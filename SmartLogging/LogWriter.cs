//******************************************************************************************
// Copyright © 2017 - 2025 Wolfgang Foerster (wolfoerster@gmx.de)
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

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SmartLogging;

public static class LogWriter
{
    private static readonly ConcurrentQueue<LogEntry> LogEntries = new();
    private static readonly CancellationTokenSource TokenSource = new();
    private static readonly object Locker = new();
    private static long MaxLength;
    private static Task WriterTask;

    /// <summary>
    /// Gets the name of the log file.
    /// </summary>
    public static string FileName { get; private set; }

    /// <summary>
    /// The minimum log level which will be processed.
    /// Log entries with a log level smaller than this value will not be processed.
    /// </summary>
    public static LogLevel MinimumLogLevel = LogLevel.Information;

    /// <summary>
    /// Optionally initializes the log writer. You only need to call this method,
    /// if you want to change the default log file name or the default maximum log file size.
    /// </summary>
    /// <param name="fileName">The full qualified name of the log file. If this parameter is null
    /// the name of the entry assembly is used for the file name and the extension will be '.log'
    /// and the file will be located in the current user's temporary directory.</param>
    /// <param name="maxLength">The maximum size of the log file (default is 16 MB).
    /// If the log file exceeds the maximum size it will be copied to a file who's name is the original
    /// name plus '.log' (e.g. MyApp.log.log) and a new file with the original name is created.</param>
    public static void Init(string fileName = null, long maxLength = 16 * 1024 * 1024)
    {
        if (WriterTask != null || maxLength < 1024)
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

        MaxLength = maxLength;
        WriterTask = Task.Run(() => WriterLoop());

        var log = new SmartLogger(typeof(SmartLogger));
        log.None("Start logging");
    }

    /// <summary>
    /// Optionally terminates the log writer and returns when all pending log entries are saved to file
    /// or when the maximum response time in milliseconds is exceeded.
    /// </summary>
    public static bool Exit(long maxResponseTime = 100)
    {
        if (maxResponseTime < 100)
            maxResponseTime = 100;

        TokenSource.Cancel();
        var t0 = DateTime.UtcNow;

        while (true)
        {
            if (WriterTask.IsCompleted)
                return true;

            Thread.Sleep(30);

            if ((DateTime.UtcNow - t0).TotalMilliseconds > maxResponseTime)
                return false;
        }
    }

    internal static void Write(object msg, LogLevel level, string className, string methodName)
    {
        if (level < MinimumLogLevel)
            return;

        try
        {
            if (WriterTask == null)
                Init();

            var entry = new LogEntry
            {
                Time = DateTime.UtcNow.ToString("o"),
                ThreadId = Thread.CurrentThread.ManagedThreadId,
                Level = level.ToString(),
                Class = className,
                Method = methodName,
                Message = msg.ToJson(),
            };

            LogEntries.Enqueue(entry);
        }
        catch
        {
        }
    }

    internal static string GetMessage(this Exception exception)
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

    private static void WriterLoop()
    {
        DateTime t0 = DateTime.UtcNow;

        while (true)
        {
            while (LogEntries.TryDequeue(out LogEntry entry))
            {
                AppendToFile(entry);
            }

            Thread.Sleep(30);

            if ((DateTime.UtcNow - t0).TotalSeconds > 10)
            {
                CheckFileSize();
                t0 = DateTime.UtcNow;
            }

            if (TokenSource.Token.IsCancellationRequested && LogEntries.Count == 0)
            {
                break;
            }
        }
    }

    private static void CheckFileSize()
    {
        if (!File.Exists(FileName))
            return;

        try
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
                log.None(new { message = "log file exceeded maximum size", logFileSize = fileInfo.Length, allowedSize = MaxLength, backupName });
            }
        }
        catch (Exception exception)
        {
            try
            {
                var str = exception.GetMessage();
                var name = FileName + ".ex.log";
                File.WriteAllText(name, exception.ToString());
            }
            catch 
            {
            }
        }
    }

    private static void AppendToFile(LogEntry entry)
    {
        try
        {
            lock (Locker)
            {
                using StreamWriter sw = File.AppendText(FileName);
                var json = ToJson(entry);
                sw.WriteLine(json);
            }
        }
        catch (Exception exception)
        {
            var str = GetMessage(exception);
            Trace.WriteLine(str);
        }
    }

    private static string ToJson(this object value)
    {
        if (value == null)
            return "null";

        if (value is string message)
            return message;

        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch
        {
            try
            {
                return value.ToString();
            }
            catch
            {
                return $"SmartLogger Error: cannot convert object of type {value.GetType().FullName} to JSON";
            }
        }
    }
}
