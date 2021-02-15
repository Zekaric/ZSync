
# Zekaric : Sync


**`Author:   `** Robbert de Groot

**`Copyright:`** 2012, Robbert de Groot

**`License:  `** MIT

## Description:


A simply directory synchronization program.  It will make a directory exactly match another directory.

## Use:


zsync [sourcePath] [destinationPath] [-e[.ext]+]

**[sourcePath]**<br>
    Path of the source directory.  Include drive letter. Eg: D:\\, C:\\temp\\

**[destinationPath]**<br>
    Path of the destination.  Include drive letter.

**[e[.ext]+]**<br>
    List the file extensions to filter out.  No spaces between extensions.  Extensions include the '.'.


## Note:


The reason I wrote this tool is because I just needed something simple to synchronize my working directories on the hard drive of may main machine to a directory on a Network Attached Storage unit (NAS).  I found a lot of tools that would do this out there but a lot have become overly complicated and not what I am looking for.
