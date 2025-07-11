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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartLogging;

public static class LogWriter
{
    private const long MinMaxFileSize = 64 * 1024;
    private const long MaxMaxFileSize = 64 * 1024 * 1024;
    private const long DefaultFileSize = 16 * 1024 * 1024;
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
    /// if you want to change the default log file name or the default maximum log file size of 16 MB.
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
        {
            Log.None("Attemp was made to call Init() repeatedly");
            return;
        }

        FileName = fileName;
        MaxFileSize = Math.Min(Math.Max(maxFileSize, MinMaxFileSize), MaxMaxFileSize);

        if (FileName == null)
        {
            var name = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            FileName = Path.Combine(Path.GetTempPath(), $"{name}.log");
        }

        try
        {
            LogDirectly("Start logging");
        }
        catch (Exception ex)
        {
            throw new ArgumentException("The specified file name is invalid.", fileName, ex);
        }

        WriterTask = Task.Run(() => WriterLoop());
    }

    /// <summary>
    /// Optionally terminates the log writer and returns when all pending log entries are saved to file
    /// or when the maximum response time in milliseconds is exceeded.
    /// </summary>
    public static bool Exit(long maxResponseTime = 100)
    {
        if (maxResponseTime < 100)
            maxResponseTime = 100;

        var ok = true;
        var t0 = DateTime.UtcNow;
        TokenSource.Cancel();
        LogDirectly("Stop logging");

        while (!WriterTask.IsCompleted)
        {
            Thread.Sleep(30);

            if ((DateTime.UtcNow - t0).TotalMilliseconds > maxResponseTime)
            {
                ok = false;
                break;
            }
        }

        var msg = ok ? "all entries are processed" : "some entries might not be processed";
        LogDirectly($"Logging stopped - {msg}");
        return ok;
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
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
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
        var entries = new List<string>();

        while (ok > 0)
        {
            if (ok == 2)
                Thread.Sleep(30);

            while (LogEntries.TryDequeue(out LogEntry entry))
                entries.Add(entry.ToJson());

            if (TokenSource.Token.IsCancellationRequested
                || (DateTime.UtcNow - t0).TotalSeconds > 0.5) // every 0.5 seconds
            {
                if (entries.Count > 0)
                {
                    try
                    {
                        CheckFileSize(FileName);
                        AppendToFile(entries);
                        entries.Clear();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }

                t0 = DateTime.UtcNow;
            }

            if (TokenSource.Token.IsCancellationRequested)
            {
                --ok;
            }
        }
    }

    private static void AppendToFile(List<string> lines)
    {
        // don't catch exceptions!
        lock (Locker)
        {
            using StreamWriter sw = File.AppendText(FileName);
            foreach (var line in lines)
            {
                sw.WriteLine(line);
            }
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

        var fileInfo = new FileInfo(fileName);
        if (fileInfo.Length > MaxFileSize)
        {
            var backupName = fileName + ".log";

            // don't catch exceptions!
            lock (Locker)
            {
                File.Copy(fileName, backupName, true);

                var msg = $"Log file {Path.GetFileName(fileName)} exceeded maximum size of {MaxFileSize.ToStringNumBytes()}. Copied to {Path.GetFileName(backupName)} in directory {Path.GetDirectoryName(backupName)}.";
                var entry = CreateEntry(msg, LogLevel.None, typeof(LogWriter).FullName, "CheckFileSize");
                File.WriteAllText(fileName, $"{entry.ToJson()}\n");
            }
        }
    }

    private static void LogDirectly(string msg, [CallerMemberName] string methodName = null)
    {
        var entry = CreateEntry(msg, LogLevel.None, typeof(LogWriter).FullName, methodName);
        AppendToFile([entry.ToJson()]);
    }

    private static string ToStringNumBytes(this long i)
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
