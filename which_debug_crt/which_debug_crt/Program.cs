using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace which_debug_crt
{
    /*
     * This app recursively traverses a root path (hard-coded below), invoking the VS linker if it
     * finds a .lib or .obj file during traversal.  Specifically it calls:
     * 
     * link.exe /dump /directives your_file.lib
     * 
     * It gathers the std out from link.exe and searches for "Microsoft.VC90.DebugCRT" or perhaps just "DebugCRT".
     * 
     * The whole point:
     * 
     * You have a large app, built against various 3rd party libraries and you are getting an SxS issue such
     * that you believe that one or more libraries in the project is built against debug runtime libraries of 
     * Visual Studio 2005 or 2008, and not rebuilt with Visual Studio 2010.
     * 
     * Helpful:
     * http://social.msdn.microsoft.com/Forums/en-US/vssetup/thread/795616f2-a4b6-4dde-b997-73f403e60666
     * http://stackoverflow.com/questions/8616001/side-by-side-configuration-incorrect-due-to-incorrect-manifest
     * 
     */

    class Program
    {
        static void Main(string[] args)
        {
            // abalta deps
            string deps_root = @"C:\Users\josh\projects\Nokia\RoutePhD\NGMB_Source_01_22_2013\AbaltaInternal";
            System.IO.DirectoryInfo rootDir = new System.IO.DirectoryInfo(deps_root);
            WalkDirectoryTree(rootDir);
        }
        static void WalkDirectoryTree(System.IO.DirectoryInfo root)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder 
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater 
            // than the application provides. 
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse. 
                // You may decide to do something different here. For example, you 
                // can try to elevate your privileges and access the file again.
                Console.WriteLine(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    // a try-catch block required here to handle the case 
                    // where the file has been deleted since the call to TraverseTree().
                    if(fi.FullName.Contains(".lib") || fi.FullName.Contains(".obj")) {
                        //Console.WriteLine(fi.FullName);
                        Scan(fi.FullName);
                    }
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo);
                }
            }            
        }
        static void Scan(string filename) 
        {
            // need to setup this env to find link.exe
            Process process = new Process();
            process.OutputDataReceived += new DataReceivedEventHandler
            (
                delegate(object sender, DataReceivedEventArgs e)
                {
                    // regex the data looking for shit
                    if(e.Data != null && e.Data.Contains("DebugCRT"))
                        Console.WriteLine(filename);
                }
            );
            /*
             * READ This!
             * 
             * To run link.exe, you first have to set a bunch of VC vars, fortunately M$
             * provides a scripts for this.
             * 
             * Run: 
             * C:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\bin\vcvars32.bat in the same shell
             * you intend to run link.exe
             * 
             */
            process.StartInfo.FileName = @"link.exe";
            process.StartInfo.Arguments = "/dump /directives " + filename;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();// Waits here for the process to exit.
            process.CancelOutputRead();
        }
    }
}
