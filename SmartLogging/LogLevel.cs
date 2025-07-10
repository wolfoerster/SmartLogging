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

public enum LogLevel
{
    /// <summary>
    /// Logs that contain the most detailed messages. These messages may contain 
    /// sensitive application data. These messages are disabled by default and
    /// should never be enabled in a production environment.
    /// </summary>
    Verbose,

    /// <summary>
    /// Logs that are used for interactive investigation during development. 
    /// These logs should primarily contain information useful for debugging and have
    /// no long-term value.
    /// </summary>
    Debug,

    /// <summary>
    /// Logs that track the general flow of the application. These 
    //// logs should have long-term value.
    /// </summary>
    Information,

    /// <summary>
    /// Logs that highlight an abnormal or unexpected event in the 
    /// application flow, but do not otherwise cause the application execution to stop.
    /// </summary>
    Warning,

    /// <summary>
    /// Logs that highlight when the current flow of execution is stopped due 
    /// to a failure. These should indicate a failure in the current activity, not an
    /// application-wide failure.
    /// </summary>
    Error,

    /// <summary>
    /// Logs that describe an unrecoverable application or system crash, or a 
    /// catastrophic failure that requires immediate attention.
    /// </summary>
    Fatal,

    /// <summary>
    /// Logs with this priority will be written to disk in any case. This level 
    /// should not be used in production code. It is but being used when the LogWriter
    /// starts working and logs the first message "Start logging" and the last message "Stop logging".
    /// </summary>
    None
}
