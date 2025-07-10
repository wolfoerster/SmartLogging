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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartLogging;

public static class LogWriter
{
    private const long DefaultFileSize = 16 * 1024 * 1024;
    private const long MinFileSize = 64 * 1024;
    private static readonly ConcurrentQueue<LogEntry> LogEntries = new();
    private static readonly CancellationTokenSource TokenSource = new();
    private static readonly SmartLogger Log = new();
    private static readonly object Locker = new();
    private static long MaxFileSize;
    private static Task WriterTask;

    /// <summary>
    /// Gets the name of the log file.
    /// </summary>
    public static string FileName { get; private set; }

    /// <summary>
    /// Gets or sets the minimum log level which will be processed.
    /// Log entries with a log level smaller than this value will not be processed.
    /// </summary>
    public static LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Optionally initializes the log writer. You only need to call this method,
    /// if you want to change the default log file name or the default maximum log file size.
    /// </summary>
    /// <param name="fileName">The full qualified name of the log file. If this parameter is null
    /// the name of the entry assembly is used for the file name and the extension will be '.log'
    /// and the file will be located in the current user's temporary directory.</param>
    /// <param name="maxFileSize">The maximum size of the log file (default is 16 MB).
    /// If the log file exceeds the maximum size it will be copied to a file who's name is the original
    /// name plus '.log' (e.g. MyApp.log.log) and a new file with the original name is created.</param>
    public static void Init(string fileName = null, long maxFileSize = DefaultFileSize)
    {
        if (WriterTask != null)
            return;

        if (maxFileSize < MinFileSize)
            maxFileSize = MinFileSize;

        if (fileName == null)
        {
            var name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            FileName = Path.Combine(Path.GetTempPath(), $"{name}.log");
        }
        else
        {
            FileName = fileName;
        }

        MaxFileSize = maxFileSize;
        WriterTask = Task.Run(() => WriterLoop());
        Log.None("Start logging");
    }

    /// <summary>
    /// Optionally terminates the log writer and returns when all pending log entries are saved to file
    /// or when the maximum response time in milliseconds is exceeded.
    /// </summary>
    public static bool Exit(long maxResponseTime = 100)
    {
        Log.None("Stop logging");

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

    internal static void Write(object msg, LogLevel level, string context, string methodName)
    {
        if (level < MinimumLogLevel)
            return;

        try
        {
            if (WriterTask == null)
                Init();

            var entry = CreateEntry(msg, level, context, methodName);
            LogEntries.Enqueue(entry);
        }
        catch
        {
        }
    }

    private static LogEntry CreateEntry(object msg, LogLevel level, string context, string methodName) => new()
    {
        Time = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
        ThreadId = Environment.CurrentManagedThreadId,
        Level = level.ToString(),
        Context = context,
        Method = methodName,
        Message = msg.ToJson(),
    };

    private static void WriterLoop()
    {
        var ok = 2;
        var t0 = DateTime.UtcNow;
        var entries = new List<LogEntry>();

        while (ok > 0)
        {
            if (ok == 2)
                Thread.Sleep(30);

            while (LogEntries.TryDequeue(out LogEntry entry))
                entries.Add(entry);

            if (TokenSource.Token.IsCancellationRequested
                || (DateTime.UtcNow - t0).TotalSeconds > 0.5) // every 0.5 seconds
            {
                if (entries.Count > 0)
                {
                    CheckFileSize(FileName);
                    AppendToFile(entries);
                    entries.Clear();
                }

                t0 = DateTime.UtcNow;
            }

            if (TokenSource.Token.IsCancellationRequested)
            {
                --ok;
            }
        }
    }

    private static void AppendToFile(List<LogEntry> entries)
    {
        try
        {
            lock (Locker)
            {
                using StreamWriter sw = File.AppendText(FileName);
                foreach (var entry in entries)
                {
                    var json = ToJson(entry);
                    sw.WriteLine(json);
                }
            }
        }
        catch (Exception exception)
        {
            LogException(exception);
        }
    }

    private static string ToJson(this object value)
    {
        if (value == null)
            return string.Empty;

        if (value is string message)
            return message;

        try
        {
            return JsonConvert.SerializeObject(value, Formatting.None);
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

    private static void CheckFileSize(string fileName)
    {
        if (!File.Exists(fileName))
            return;

        try
        {
            var fileInfo = new FileInfo(fileName);
            if (fileInfo.Length > MaxFileSize)
            {
                var backupName = fileName + ".log";

                lock (Locker)
                {
                    File.Delete(backupName);
                    File.Copy(fileName, backupName);

                    var msg = $"Log file {Path.GetFileName(fileName)} exceeded maximum size of {MaxFileSize.ToStringNumBytes()}. Copied to {Path.GetFileName(backupName)} in directory {Path.GetDirectoryName(backupName)}.";
                    var entry = CreateEntry(msg, LogLevel.None, typeof(LogWriter).FullName, "CheckFileSize");
                    var json = entry.ToJson();
                    File.WriteAllText(fileName, $"{entry.ToJson()}\n");
                }
            }
        }
        catch (Exception exception)
        {
            LogException(exception);
        }
    }

    private static void LogException(Exception exception)
    {
        try
        {
            var fileName = FileName + ".ex.log";
            CheckFileSize(fileName);
            File.AppendAllText(fileName, $"\n{exception.ToString()}\n");
        }
        catch
        {
        }
    }

    public static string ToStringNumBytes(this long i)
    {
        string suffix;
        double readable;
        long absolute_i = (i < 0 ? -i : i);

        if (absolute_i >= 0x1000000000000000) // Exabyte
        {
            suffix = "EB";
            readable = (i >> 50);
        }
        else if (absolute_i >= 0x4000000000000) // Petabyte
        {
            suffix = "PB";
            readable = (i >> 40);
        }
        else if (absolute_i >= 0x10000000000) // Terabyte
        {
            suffix = "TB";
            readable = (i >> 30);
        }
        else if (absolute_i >= 0x40000000) // Gigabyte
        {
            suffix = "GB";
            readable = (i >> 20);
        }
        else if (absolute_i >= 0x100000) // Megabyte
        {
            suffix = "MB";
            readable = (i >> 10);
        }
        else if (absolute_i >= 0x400) // Kilobyte
        {
            suffix = "KB";
            readable = i;
        }
        else
        {
            return i.ToString("0 B"); // Byte
        }

        // divide by 1024 to get fractional value
        readable = (readable / 1024);

        // return formatted number with suffix
        return readable.ToString("0.### ") + suffix;
    }
}
