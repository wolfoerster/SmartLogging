﻿//******************************************************************************************
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
using System.Diagnostics;

namespace SmartLogging
{
    public class SmartTraceListener : TraceListener
    {
        private static readonly SmartLogger Log = new SmartLogger();

        public override void Write(string message)
        {
            Log.Smart(message);
        }

        public override void WriteLine(string message)
        {
            Log.Smart(message);
        }
    }
}
