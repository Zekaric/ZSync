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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace zsync
{
   [DebuggerDisplay("{Path,nq}, {Type}")]
   class PathData
   {
      public enum PathType
      {
         NONE,
         FILE,
         DIR
      };

      public PathType Type { get; set; } = PathType.NONE;
      public string   Path               = string.Empty;
      public FileInfo Info { get; set; } = null;
   }

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
         List<PathData> spathList,
                        dpathList;
         List<string>   slist,
                        excludeExt,
                        excludeDir;

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
               "[d[folder],...      - List the folders to filter out.\n\n");
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
         _PrepDir(spath, spathList);
         _PrepDir(dpath, dpathList);

         // Do the synchronization/mirroring.
         Console.Write("- Synchonizing.\n");
         _Sync(spath, spathList, dpath, dpathList);

         Console.Write("- Finished.\n");
         return 0;
      }

      // _GetFiles ////////////////////////////////////////////////////////////
      // Recursively fetches all the files in the directory.
      static private List<PathData> _GetFiles(string path, List<String> excludeDir, List<String> excludeExt)
      {
         int            index,
                        dindex,
                        eindex,
                        findex;
         string[]       fileList,
                        dirList;
         List<PathData> pathList,
                        pathListTemp;
         PathData       pathTemp;

         // Ignore recycling bin.
         if (path.IndexOf("$RECYCLE") >= 0)
         {
            return null;
         }

         // Get the directories.
         dirList  = Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly);
         // Get the files in this directory.
         fileList = Directory.GetFiles(      path, "*.*", SearchOption.TopDirectoryOnly);

         pathList = new List<PathData>();

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

            pathTemp      = new PathData();
            pathTemp.Type = PathData.PathType.FILE;
            pathTemp.Path =              fileList[findex];
            pathTemp.Info = new FileInfo(fileList[findex]);
            pathList.Add(pathTemp);
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

               pathTemp      = new PathData();
               pathTemp.Type = PathData.PathType.DIR;
               pathTemp.Path = dirList[index];
               pathList.Add(pathTemp);

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

      // _RemoveDir ///////////////////////////////////////////////////////////
      // Remove a directory.
      static void _RemoveDir(string dir)
      {
         if (!Directory.Exists(dir))
         {
            return;
         }

         try
         {
            Directory.Delete(dir, true);
         }
         catch
         {
            Console.Write("   Remove directory failed.\n");
         }
      }

      // _PrepDir /////////////////////////////////////////////////////////////
      // Sort the path list and fetch the file information for each file.
      static private void _PrepDir(string path, List<PathData> pathList)
      {
         int index;

         // Sort the list.  Put all the directory enters in alpha order at the
         // start of the list.  Then the files in alpha order.  And finally the
         // directory exits at the end.
         pathList.Sort(delegate(PathData a, PathData b)
         {
            // Same path type, compare paths.
            if (a.Type == b.Type)
            {
               return a.Path.CompareTo(b.Path);
            }

            // a and b are not the same types.
            // a is a directory enter, then it will always be before b.
            if (a.Type == PathData.PathType.DIR)
            {
               return -1;
            }

            // a is a file.
            // b is a directory entry, then a will always be after b.
            return 1;
         });

         // Trim the path string to relative path.
         for (index = 0; index < pathList.Count; index++)
         {
            pathList[index].Path = pathList[index].Path.Substring(path.Length);
         }
      }

      // _Sync ////////////////////////////////////////////////////////////////
      // Synch the two directories.
      static private bool _Sync(string spath, List<PathData> spathDataList, string dpath, List<PathData> dpathDataList)
      {
         int            sindex,
                        dindex,
                        result;
         List<PathData> sdir  = new List<PathData>(),
                        ddir  = new List<PathData>(),
                        sfile = new List<PathData>(),
                        dfile = new List<PathData>();
         
         // Break up the lists.
         for (sindex = 0; sindex < spathDataList.Count; sindex++)
         {
            if (spathDataList[sindex].Type == PathData.PathType.DIR)
            {
               sdir.Add(spathDataList[sindex]);
            }
            else
            {
               sfile.Add(spathDataList[sindex]);
            }
         }

         for (dindex = 0; dindex < dpathDataList.Count; dindex++)
         {
            if (dpathDataList[dindex].Type == PathData.PathType.DIR)
            {
               ddir.Add(dpathDataList[dindex]);
            }
            else
            {
               dfile.Add(dpathDataList[dindex]);
            }
         }

         // Stage 1:
         // Create new directories in the destination.
         sindex    =
            dindex = 0;
         for (;;)
         {
            if (sindex == sdir.Count())
            {
               break;
            }

            // We have a source and destination folder to compare.
            if      (sindex < sdir.Count &&
                     dindex < ddir.Count)
            {
               // Compare the folder names.
               result = sdir[sindex].Path.CompareTo(ddir[dindex].Path);
               
               // Folders exist in both, nothing to do.
               if      (result == 0)
               {
                  // Move to the next folders in each list.
                  sindex++;
                  dindex++;
               }
               // Source folder is less alphabetically.  Meaning it doesn't exist  in the
               // destination.  Create the folder.
               else if (result < 0)
               {
                  Console.Write("N\\ " + sdir[sindex].Path + "\n");

                  // Make sure the directory is created first.
                  _MakeDir(dpath + sdir[sindex].Path);

                  sindex++;
               }
               // Destination folder is less than the source folder.  Meaning the destination has
               // folders that doesn't exist in the source.   These will be removed in stage 3.
               else
               {
                  dindex++;
               }
            }
            // There are more source folders than there are destination folders.  Ensure the new
            // folders exist in the destination.
            else if (sindex < sdir.Count)
            {
               Console.Write("N\\ " + sdir[sindex].Path + "\n");

               // Make sure the directory is created first.
               _MakeDir(dpath + sdir[sindex].Path);

               sindex++;
            }
         }

         // Stage 2:
         // Move new files and files that are newer with the same name to the
         // destination.
         // Remove destination files that do not exist in the source list.
         sindex    =
            dindex = 0;
         for (;;)
         {
            // We are done when there is nothing left to check.
            if (sindex == sfile.Count() &&
                dindex == dfile.Count())
            {
               break;
            }

            // Files found with the same name.
            // Only valid if there is anything left in the destination list to check.
            if      (dindex < dfile.Count() &&
                     sindex < sfile.Count())
            { 
               result = sfile[sindex].Path.CompareTo(dfile[dindex].Path);

               // Both files exist in both directories.
               if (result == 0)
               {
                  // For some reason s...[sindex].Info may not have valid data.  It will except.
                  // So this try block is here to catch that.  Maybe something to do with
                  // permissions.
                  try
                  {
                     // Why not just d...LastWriteTimeUtc < s...LastWriteTimeUtc?
                     // The millisecond tick count on my QNAP will be off by a few even for
                     // something that was just copied over.  So I just check down to the 'second'
                     // level and no more.
                     if (dfile[dindex].Info.LastWriteTimeUtc.Year < sfile[sindex].Info.LastWriteTimeUtc.Year                                  ||
                         (dfile[dindex].Info.LastWriteTimeUtc.Year == sfile[sindex].Info.LastWriteTimeUtc.Year                             &&
                          (dfile[dindex].Info.LastWriteTimeUtc.DayOfYear < sfile[sindex].Info.LastWriteTimeUtc.DayOfYear                ||
                           (dfile[dindex].Info.LastWriteTimeUtc.DayOfYear == sfile[sindex].Info.LastWriteTimeUtc.DayOfYear           &&
                            (dfile[dindex].Info.LastWriteTimeUtc.Hour < sfile[sindex].Info.LastWriteTimeUtc.Hour                  ||
                             (dfile[dindex].Info.LastWriteTimeUtc.Hour == sfile[sindex].Info.LastWriteTimeUtc.Hour             &&
                              (dfile[dindex].Info.LastWriteTimeUtc.Minute < sfile[sindex].Info.LastWriteTimeUtc.Minute      ||
                               (dfile[dindex].Info.LastWriteTimeUtc.Minute == sfile[sindex].Info.LastWriteTimeUtc.Minute &&
                                dfile[dindex].Info.LastWriteTimeUtc.Second < sfile[sindex].Info.LastWriteTimeUtc.Second))))))))
                     {
                        // Try to copy the file.
                        try
                        {
                           Console.Write("> " + sfile[sindex].Path + "\n");
                           File.Copy(spath + sfile[sindex].Path, dpath + dfile[dindex].Path, true);
                        }
                        catch
                        {
                           Console.Write("   Copy failed.\n");
                        }
                     }
                  }
                  catch
                  {
                     Console.Write(
                        "   Copy failed.  Permissions?\n");
                  }
                  dindex++;
                  sindex++;
               }
               // Source file is alphabetically less than the next destination file.  Meaning the
               // source file doesn't exist in the destination.  Copy the new file.
               else if (result < 0)
               {
                  // try and copy the new file.
                  try
                  {
                     Console.Write("N  " + sfile[sindex].Path + "\n");

                     File.Copy(spath + sfile[sindex].Path, dpath + sfile[sindex].Path);
                  }
                  catch
                  {
                     Console.Write("   Copy failed.\n");
                  }

                  sindex++;
               }
               // Destination file is alphabetically less than the next source file.  Meaning the
               // destination file doesn't exist in the source.  Remove the destination file.
               else 
               {
                  // Try to delete the file.
                  try
                  {
                     Console.Write("X  " + dfile[dindex].Path + "\n");
                     File.Delete(dpath + dfile[dindex].Path);
                  }
                  catch
                  {
                     Console.Write("   Delete failed.\n");
                  }

                  dindex++;
               }
            }
            // There are more source files than there are destination files.  Copy the new files.
            else if (sindex < sfile.Count())
            {
               // try and copy the new file.
               try
               {
                  Console.Write("N  " + sfile[sindex].Path + "\n");

                  File.Copy(spath + sfile[sindex].Path, dpath + sfile[sindex].Path);
               }
               catch
               {
                  Console.Write("   Copy failed.\n");
               }

               sindex++;
            }
            // There are more destination files than there are source files.  Remove the extra 
            // destination files.
            else
            {
               // Try to delete the file.
               try
               {
                  Console.Write("X  " + dfile[dindex].Path + "\n");
                  File.Delete(dpath + dfile[dindex].Path);
               }
               catch
               {
                  Console.Write("   Delete failed.\n");
               }

               dindex++;
            }
         }

         // Stage 3:
         // Remove destination folders, which should now be empty.  Done in reverse so that we do
         // the deepest level folders first.
         sindex = sdir.Count - 1;
         dindex = ddir.Count - 1;
         for (;;)
         {
            if (dindex == -1)
            {
               break;
            }

            // We have a source and destination folder to compare.
            if      (sindex >= 0 &&
                     dindex >= 0)
            {
               // Compare the folder names.
               result = sdir[sindex].Path.CompareTo(ddir[dindex].Path);

               // Folders exist in both, nothing to do.
               if (result == 0)
               {
                  // Move to the next folders in each list.
                  sindex--;
                  dindex--;
               }
               // Source folder is greater alphabetically.  Meaning it doesn't exist  in the
               // destination.  Nothing to do.
               else if (result > 0)
               {
                  sindex--;
               }
               // Destination folder is less than the source folder.  Meaning the destination has
               // folders that doesn't exist in the source.   Remove folder.
               else
               {
                  Console.Write("X\\ " + ddir[dindex].Path + "\n");

                  // Remove the directory.
                  _RemoveDir(dpath + ddir[dindex].Path);

                  dindex--;
               }
            }
            // Ignore all the remaining destination folders.  These folders will be removed in
            // stage 3.
            else
            {
               Console.Write("X\\ " + ddir[dindex].Path + "\n");

               // Remove the directory.
               _RemoveDir(dpath + ddir[dindex].Path);

               dindex--;
            }
         }

         return true;
      }
   }
}
