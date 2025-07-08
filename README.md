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
4. log context (the name of the calling class)
5. log method (the name of the calling method)
6. log message

The log message is a simple string or the JSON representation of an object. 

### LogWriter
LogWriter is a static class, so there is only one instance per application. 
LogWriter lets you specify the log file, its maximum allowed size and the minimimum
log level to be processed. In most cases you will be happy with the default settings
of LogWriter, so you won't have to deal with this class.

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

The resulting log entry will contain all of the 6 properties mentioned above. 
The log context (class name) is extracted from the declaration of the logger 
and the method name is extracted from the actual logging statement.

Even if you call any of the SmartLogger's methods without parameter, e.g.

`Log.Warning()`

you will have a log entry which shows the time, the class name and the method name.

#### Using Anonymous Objects
Consider a method like this:

`void Something(string name, double age)`

And you call it like this:

`Something("asd", "123");`

If you log the parameters like this:

`Log.Information(new {name, age})`

You will get a log entry with this message:

"{ "name": "asd",  "age": 123 }"

#### LogWriter
If you don't explicitely call the (one and only) LogWriter, your log entries will 
be written to the current user's temporary directory into a file called 
*"name of your application.log*", the maximum file size will be 16 MB and the 
minimum log level will be *Information*.

If you want to change the default behaviour, you can call

`LogWriter.Init(string fileName, long maxLength)`

and set the minimum log level with e.g.

`LogWriter.MinimumLogLevel = LogLevel.Warning`

If the log file exceeds the maximum size of 16 MB or the size specified by 
*LogWriter.Init()* the current log file is copied to a file called *"MyApp.log.log"*
if the original file is called *"MyApp.log"* and a new file *"MyApp.log"* is created.

#### LogWriter.Exit()
When your application terminates the LogWriter might not yet be done with writing
all log entries to disk. Make a call to *"LogWriter.Exit()"* to ensure that all 
entries are processed.