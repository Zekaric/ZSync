/******************************************************************************
file:       ZSync
author:     Robbert de Groot
copyright:  2012, Robbert de Groot

description:
Synchronize two folders.
******************************************************************************/

/******************************************************************************
MIT License

Copyright (c) 2012, Robbert de Groot

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
******************************************************************************/

// Using //////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace zsync
{
   class Program
   {
      private const int indexSRC = 0;
      private const int indexDST = 1;

      // Main /////////////////////////////////////////////////////////////////
      static int Main(string[] args)
      {
         int            index,
                        eindex;
         string         spath,
                        dpath;
         string[]       option = { "", "" };
         List<string>   spathList,
                        dpathList,
                        slist,
                        excludeExt,
                        excludeDir;
         List<FileInfo> sList,
                        dList;

         // Print the header.
         Console.Write(
            "==============================================================================\n" +
            "zsync\n" +
            "==============================================================================\n\n");

         // Not enough arguments then inform the user on how to use the program.
         if (args.Length < 2)
         {
            Console.Write(
               "zsync [sourcePath] [destinationPath] [-e[ext],... [-d[folder],...\n\n" +
               "[sourcePath]        - Path of the source.  Include drive. Eg: D:\\, C:\\temp\\\n" +
               "[destinationPath]   - Path of the destination.\n" +
               "[e[ext],...         - List the file extensions to filter out.\n" +
               "[d[folder[,...      - List the folders to filter out.\n\n");
            return 0;
         }

         // Get the source and destination paths.
         spath     = args[indexSRC];
         dpath     = args[indexDST];
         option[0] = "";
         option[1] = "";
         if (args.Length >= 3)
         {
            option[0] = args[2];
         }
         if (args.Length >= 4)
         {
            option[1] = args[3];
         }

         // Make sure there is a terminating \
         if (spath.Substring(spath.Length - 1).CompareTo("\\") != 0)
         {
            spath += "\\";
         }
         if (dpath.Substring(dpath.Length - 1).CompareTo("\\") != 0)
         {
            dpath += "\\";
         }

         // Check if the directories exist.
         if (!Directory.Exists(spath))
         {
            Console.Write(
               "ERROR: Source directory doesn't exist.\n" +
               args[indexSRC] +
               "\n");
            return 1;
         }

         if (!Directory.Exists(dpath))
         {
            Console.Write(
               "ERROR: Destination direction doesn't exist.\n" +
               args[indexDST] +
               "\n");
            return 2;
         }

         // Get the extension excludes
         excludeExt = null;
         excludeDir = null;
         for (index = 0; index < 2; index++)
         {
            if (string.IsNullOrEmpty(option[index]))
            {
               continue;
            }

            // Break the list of extensions or folders apart.
            slist    = option[index].Split(',').ToList<String>();
            // First one in the list will have the -e or -d, remove that.
            slist[0] = slist[0].Substring(2);

            switch (option[index][1])
            {
            case 'd':
               excludeDir = slist;
               break;

            case 'e':
               excludeExt = slist;

               // Prep the regular expression.
               for (eindex = 0; eindex < excludeExt.Count; eindex++)
               {
                  excludeExt[eindex] = "\\." + excludeExt[eindex] + "$";
               }
               break;
            }
         }

         // Fetch all the files found in the two directories.
         Console.Write("- Reading source tree.      ");
         spathList = _GetFiles(spath, excludeDir, excludeExt);
         Console.Write("File Count: " + spathList.Count() + "\n");

         Console.Write("- Reading destination tree. ");
         dpathList = _GetFiles(dpath, null, null);
         Console.Write("File Count: " + dpathList.Count() + "\n");

         // Sort them and get the file information for each file.
         Console.Write("- Prepping trees.\n");
         sList = _PrepDir(spath, spathList);
         dList = _PrepDir(dpath, dpathList);

         // Do the synchronization/mirroring.
         Console.Write("- Synchonizing.\n");
         _Sync(spath, spathList, sList, dpath, dpathList, dList);

         Console.Write("- Finished.\n");
         return 0;
      }

      // _GetFiles ////////////////////////////////////////////////////////////
      // Recursively fetches all the files in the directory.
      static private List<string> _GetFiles(string path, List<String> excludeDir, List<String> excludeExt)
      {
         int          index,
                      dindex,
                      eindex,
                      findex;
         string[]     fileList,
                      dirList;
         List<string> pathList,
                      pathListTemp;

         // Ignore recycling bin.
         if (path.IndexOf("$RECYCLE") >= 0)
         {
            return null;
         }

         // Get the directories.
         dirList  = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly);
         // Get the files in this directory.
         fileList = Directory.GetFiles(      path, "*.*", SearchOption.TopDirectoryOnly);

         pathList = new List<string>();

         // Append the local files to the path list.
         for (findex = 0; findex < fileList.Length; findex++)
         {
            if (excludeExt != null)
            {
               for (eindex = 0; eindex < excludeExt.Count; eindex++)
               {
                  if (Regex.Match(fileList[findex], excludeExt[eindex]).Success)
                  {
                     break;
                  }
               }

               // We found a match so don't include this file.
               if (eindex != excludeExt.Count)
               {
                  continue;
               }
            }

            pathList.Add(fileList[findex]);
         }

         // For all directories.
         for (index = 0; index < dirList.Length; index++)
         {
            try
            {
               // Ignore certain directories.
               if (excludeDir != null)
               {
                  for (dindex = 0; dindex < excludeDir.Count; dindex++) 
                  {
                     if (dirList[index].IndexOf(excludeDir[dindex]) != -1)
                     {
                        break;
                     }
                  }

                  // We found a match so don't include this directory.
                  if (dindex != excludeDir.Count)
                  {
                     continue;
                  }
               }

               // Get their files.
               pathListTemp = _GetFiles(dirList[index], excludeDir, excludeExt);

               pathList.AddRange(pathListTemp);
            }
            catch
            {
            }
         }

         return pathList;
      }

      // _MakeDir /////////////////////////////////////////////////////////////
      // Create a directory if it is missing.
      static bool _MakeDir(string dir)
      {
         // Pop the filename
         string dirPopped;
         int    index;

         // Oh swell, the directory exists.
         if (Directory.Exists(dir))
         {
            return true;
         }

         // Pop off a directory and check again.
         index     = dir.LastIndexOf('\\');
         dirPopped = dir.Substring(0, index);
         _MakeDir(dirPopped);

         // Create the directory.
         Directory.CreateDirectory(dir);

         return true;
      }

      // _PrepDir /////////////////////////////////////////////////////////////
      // Sort the path list and fetch the file information for each file.
      static private List<FileInfo> _PrepDir(string path, List<string> pathList)
      {
         int            index;
         FileInfo       fileInfo;
         List<FileInfo> fileList = new List<FileInfo>();

         pathList.Sort();
         for (index = 0; index < pathList.Count(); index++)
         {
            fileInfo = new FileInfo(pathList[index]);

            fileList.Add(fileInfo);
            pathList[index] = pathList[index].Substring(path.Length);
         }

         return fileList;
      }

      // _Sync ////////////////////////////////////////////////////////////////
      // Synch the two directories.
      static private bool _Sync(string spath, List<string> spathList, List<FileInfo> sList, string dpath, List<string> dpathList, List<FileInfo> dList)
      {
         int sindex,
             dindex;

         sindex    =
            dindex = 0;
         for (; ; )
         {
            // We are done when there is nothing left to check.
            if (sindex == spathList.Count() &&
                dindex == dpathList.Count())
            {
               break;
            }

            // Files found with the same name.
            // Only valid if there is anything left in the destination list to check.
            if      (dindex <  dpathList.Count() &&
                     sindex != spathList.Count() &&
                dpathList[dindex].CompareTo(spathList[sindex]) == 0)
            {
               // For some reason sList[sindex] may not have valid data.  It will except.
               // So this try block is here to catch that.  Maybe something to do with permissions.
               try
               {
                  // Why not just dList[dindex].LastWriteTimeUtc < sList[sindex].LastWriteTimeUtc?
                  // The millisecond tick count on my QNAP will be off by a few even for something that was 
                  // just copied over.  So I just check down to the 'second' level and no more.
                  if (dList[dindex].LastWriteTimeUtc.Year < sList[sindex].LastWriteTimeUtc.Year                                  ||
                      (dList[dindex].LastWriteTimeUtc.Year == sList[sindex].LastWriteTimeUtc.Year                             &&
                       (dList[dindex].LastWriteTimeUtc.DayOfYear < sList[sindex].LastWriteTimeUtc.DayOfYear                ||
                        (dList[dindex].LastWriteTimeUtc.DayOfYear == sList[sindex].LastWriteTimeUtc.DayOfYear           &&
                         (dList[dindex].LastWriteTimeUtc.Hour < sList[sindex].LastWriteTimeUtc.Hour                  ||
                          (dList[dindex].LastWriteTimeUtc.Hour == sList[sindex].LastWriteTimeUtc.Hour             &&
                           (dList[dindex].LastWriteTimeUtc.Minute < sList[sindex].LastWriteTimeUtc.Minute      ||
                            (dList[dindex].LastWriteTimeUtc.Minute == sList[sindex].LastWriteTimeUtc.Minute &&
                             dList[dindex].LastWriteTimeUtc.Second < sList[sindex].LastWriteTimeUtc.Second))))))))
                  {
                     // Try to copy the file.
                     try
                     {
                        Console.Write("> " + spathList[sindex] + "\n");
                        File.Copy(spath + spathList[sindex], dpath + dpathList[dindex], true);
                     }
                     catch
                     {
                        Console.Write("   Copy Failed.\n");
                     }
                  }
               }
               catch
               {
                  Console.Write(
                     "ERROR: Received an exception on valid data.  Ignoring.\n" +
                     " file: " + spathList[sindex] + "\n");
               }
               dindex++;
               sindex++;
            }
            // Destination file is no longer around.
            else if (dindex < dpathList.Count()       &&
                     (sindex == spathList.Count()  ||
                      dpathList[dindex].CompareTo(spathList[sindex]) < 0))
            {
               // Try to delete the file.
               try
               {
                  Console.Write("X " + dpathList[dindex] + "\n");
                  File.Delete(dpath + dpathList[dindex]);
               }
               catch
               {
                  Console.Write("   Delete failed.\n");
               }
               dindex++;
            }
            // Source file is not on the destination.
            else
            {
               // try and copy the new file.
               try
               {
                  Console.Write("N " + spathList[sindex] + "\n");

                  // Make sure the directory is created first.
                  _MakeDir(Path.GetDirectoryName(dpath + spathList[sindex]));

                  File.Copy(spath + spathList[sindex], dpath + spathList[sindex]);
               }
               catch
               {
                  Console.Write("   Copy failed.\n");
               }
               sindex++;
            }
         }

         return true;
      }
   }
}
