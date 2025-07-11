# SmartLogging
Easy to use .NET file based logging framework using JSON.

## Overview
The SmartLogging package exports two main classes: SmartLogger and LogWriter.

### SmartLogger
SmartLogger is used to create log entries with certain log levels.

A log entry contains the following information:

1. creation time of the entry
2. thread id of the calling thread
3. log level (a value between 0 and 6)
4. log context (usually the name of the calling class)
5. log method (usually the name of the calling method)
6. log message (a simple string or the JSON representation of an object)

### LogWriter
LogWriter is a static class, so there is only one instance per application. 
LogWriter lets you specify the log file, its maximum allowed size and the minimimum
log level to be processed. In most cases you will be happy with the default settings
of LogWriter, so you won't have to deal with this class except for one method:

### LogWriter.Exit()

The LogWriter cashes log entries before they are written to disk. So if your
application terminates unexpectedly there might be a few entries in the cache
which you will not see in the log file. To avoid this call `LogWriter.Exit()`
when your application is about to stop.

### LogLevel
The enum LogLevel defines 7 levels:

***Verbose (0)***: Logs that contain the most detailed messages. These messages may 
contain sensitive application data. These messages are disabled by default and 
should never be enabled in a production environment.

***Debug (1)***: Logs that are used for interactive investigation during development. 
These logs should primarily contain information useful for debugging and have 
no long-term value.

***Information (2)***: Logs that track the general flow of the application. These 
logs should have long-term value.

***Warning (3)***: Logs that highlight an abnormal or unexpected event in the 
application flow, but do not otherwise cause the application execution to stop.

***Error (4)***: Logs that highlight when the current flow of execution is stopped due 
to a failure. These should indicate a failure in the current activity, not an 
application-wide failure.

***Fatal (5)***: Logs that describe an unrecoverable application or system crash, or a 
catastrophic failure that requires immediate attention.

***None (6)***: Logs with this priority will be written to disk in any case. This level 
should not be used in production code. It is but being used when the LogWriter 
starts working and logs the first message "Start logging".

### Usage
You will use a single SmartLogger in each class which has to create log entries.

You can declare this logger as static so every instance of this class will share
the same logger. Since there is only one LogWriter per application this is fine. 
So in your class you should declare something like this:

`private static readonly SmartLogger Log = new();`

And when you want to create a log entry inside a method you will call:

`Log.Information("this message is for you");`

The resulting log entry looks like this:

`{"Time":"2025-07-10T08:55:55.3391580Z","ThreadId":1,"Level":"Information",
"Context":"TestApp.Program","Method":"Main","Message":"this message is for you"}`

The log context is extracted from the declaration of the logger 
and the method name is extracted from the actual logging statement.

Even if you call any of the SmartLogger's methods without parameter, e.g.

`Log.Warning()`

you will have a log entry which shows the time, the class name and the method name.

### Logging Objects
The nice thing about SmartLogger is that it not only logs simple strings but also 
objects. If you do a call like this:

`Log.Information(DateTime.Now);`

then the JSON-serialized DateTime object will appear as message in your log entry:

`{"Time":"2025-07-09T12:49:18.9541781Z","ThreadId":1,"Level":"Information",
"Context":"TestApp.Program","Method":"Main",
"Message":"\"2025-07-09T14:49:18.9412736+02:00\""}`

This is especially usefull when logging parameters of a method.
Consider the following method:

`void DoSomething(string name, double age)`

If a logger can only log simple strings you would have to do this:

`Log.Information($"name={name},age={age}");`

With SmartLogger you can simply do this:

`Log.Information(new {name, age})`

You will get this log entry:

`{"Time":"2025-07-09T13:23:22.9126806Z",
"ThreadId":1,"Level":"Information","Context":"TestApp.Program",
"Method":"DoSomething","Message":"{\"name\":\"asd\",\"age\":123}"}`

### LogWriter Details
If you don't explicitely call the (one and only) LogWriter's Init() method,
your log entries will be written to the current user's temporary directory 
into a file called *"name of your application.log*", the maximum file size 
will be 16 MB and the minimum log level will be *Information*.

If you want to change the default behaviour, you can call

`LogWriter.Init(string fileName, long maxSize)`

and set the minimum log level with e.g.

`LogWriter.MinimumLogLevel = LogLevel.Warning`

Note that the lowest accepted maxSize is 64 kB and the highest is 64 MB.

If the log file exceeds the maximum size the current log file is copied to a 
file called *"MyApp.log.log"* if the original file is called *"MyApp.log"* 
and the file *"MyApp.log"* is cleared.
