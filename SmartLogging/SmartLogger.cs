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

namespace SmartLogging
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class SmartLogger
    {
        private readonly string className;

        public SmartLogger(object context = null)
        {
            if (context == null)
            {
                var stackTrace = new StackTrace();
                var method = stackTrace.GetFrame(1).GetMethod();
                context = method.DeclaringType;
            }

            this.className = GetClassName(context);
        }

        public static LogLevel MinimumLogLevel = LogLevel.Information;

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
            this.Fatal(new { ExceptionMessage = exception.GetMessage(), exception.StackTrace }, methodName);
        }

        public void Write(object msg, LogLevel level, [CallerMemberName] string methodName = null)
        {
            if (level < MinimumLogLevel)
                return;

            try
            {
                var entry = new LogEntry
                {
                    Time = DateTime.UtcNow.ToString("o"),
                    ThreadId = Thread.CurrentThread.ManagedThreadId,
                    Level = level.ToString(),
                    Class = className,
                    Method = methodName,
                    Message = msg.GetString(),
                };

                LogWriter.Add(entry);
            }
            catch
            {
            }
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
    }
}
