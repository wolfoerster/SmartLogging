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

public class LogEntry
{
    /// <summary>
    /// The creation time of the entry with time zone information.
    /// </summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>
    /// The managed thread id of the calling method.
    /// </summary>
    public string ThreadId { get; set; } = string.Empty;

    /// <summary>
    /// The log level.
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// The log context (aka log category - usually the full class name of the calling method).
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// The name of the calling method.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// The log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
