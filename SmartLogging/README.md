# SmartLogging

Easy to use .NET file based logging framework.

## Overview

The SmartLogging package exports two main classes: SmartLogger and LogWriter.

### SmartLogger
SmartLogger is used to create log entries with certain log levels.

A log entry contains the following information:

1. creation time of the entry
2. thread id of the calling thread
3. log level
4. log context (the name of the calling class)
5. log method (the name of the calling method)
6. log message

The log message can be a simple string or the JSON representation of an object. 

### LogWriter
LogWriter lets you specify the log file, its maximum allowed size and the minimimum
log level to be processed. In most cases you will be happy with the default settings
of LogWriter, so you won't have to deal with this class.

### Usage
