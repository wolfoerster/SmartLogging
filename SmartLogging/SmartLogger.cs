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

namespace SmartLogging;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public class SmartLogger
{
    private readonly string className;

    /// <summary>
    /// Creates a new SmartLogger with an optional log context.
    /// The log context can be a string, a type or the instance of an object.
    /// It is used to set the 'Class' property of each log entry made by this logger.
    /// <para/>
    /// 1. if 'context' is a string, this string will be used as class name
    /// <para/>
    /// 2. if 'context' is a type, the full typename will be used as class name
    /// <para/>
    /// If 'context' is an instance of an object, the object's type will go into 2.
    /// <para/>
    /// If 'context' is null, the type of the method which calls this ctor will go into 2.
    /// </summary>
    /// <param name="context">The log context.</param>
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

    /// <summary>
    /// Creates a log entry with level Verbose.
    /// </summary>
    public void Verbose(object msg = null, [CallerMemberName] string methodName = null)
    {
        this.Write(msg, LogLevel.Verbose, methodName);
    }

    /// <summary>
    /// Creates a log entry with level Debug.
    /// </summary>
    public void Debug(object msg = null, [CallerMemberName] string methodName = null)
    {
        this.Write(msg, LogLevel.Debug, methodName);
    }

    /// <summary>
    /// Creates a log entry with level Information.
    /// </summary>
    public void Information(object msg = null, [CallerMemberName] string methodName = null)
    {
        this.Write(msg, LogLevel.Information, methodName);
    }

    /// <summary>
    /// Creates a log entry with level Warning.
    /// </summary>
    public void Warning(object msg = null, [CallerMemberName] string methodName = null)
    {
        this.Write(msg, LogLevel.Warning, methodName);
    }

    /// <summary>
    /// Creates a log entry with level Error.
    /// </summary>
    public void Error(object msg = null, [CallerMemberName] string methodName = null)
    {
        this.Write(msg, LogLevel.Error, methodName);
    }

    /// <summary>
    /// Creates a log entry with level Fatal.
    /// </summary>
    public void Fatal(object msg = null, [CallerMemberName] string methodName = null)
    {
        this.Write(msg, LogLevel.Fatal, methodName);
    }

    /// <summary>
    /// Creates a log entry with level None.
    /// </summary>
    public void None(object msg = null, [CallerMemberName] string methodName = null)
    {
        this.Write(msg, LogLevel.None, methodName);
    }

    /// <summary>
    /// Creates a log entry with the specified level.
    /// </summary>
    public void Write(object msg, LogLevel level, [CallerMemberName] string methodName = null)
    {
        LogWriter.Write(msg, level, className, methodName);
    }
}
