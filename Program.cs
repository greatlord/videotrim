using System;
using System.IO;

namespace TrimVideo // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {

            string inFile = "";
            string outFile = "";
            string strParms;
            string strFFmpegParms = "";

            if ( args.Length < 4 )
            {
                help();
                System.Environment.Exit(-3);
            }

            if (File.Exists("ffmpeg.txt"))
            {
                using (StreamReader reader = new StreamReader("ffmpeg.txt"))
                {
                    strFFmpegParms = reader.ReadLine() ?? "";
                    strFFmpegParms = strFFmpegParms.Trim();
                }
            }

            TrimFileVildate(args,strFFmpegParms);
            TrimFolderVildate(args, strFFmpegParms);

            help();
        }

        static void TrimFolderVildate(string[] parms, string strFFmpegParms)
        {
            
            string inFolder;
            string outFolder;
            string[] files;

            inFolder = "";
            outFolder = "";

            for (int i = 0; i < parms.Length; i += 2)
            {

                string parm = parms[i];

                if (parm == "-id") {
                    inFolder = parms[i + 1];
                } else if (parm == "-od") {
                    outFolder = parms[i + 1];
                } else if (parm == "-cuda") {
                    if (parms[i + 1] == "on") {
                        ClassFfmpegProc.useCuda = true;
                    }
                }

            }

            if ((string.IsNullOrEmpty(inFolder)) || (string.IsNullOrEmpty(outFolder))) {
                return ;
            }

            if ( (!Directory.Exists(inFolder)) || (!Directory.Exists(outFolder)) )
            {
                System.Console.WriteLine("Folders does not exists, duble check in and out folders exists");
                System.Environment.Exit(-7);
            }

            files = Directory.GetFiles(outFolder);
            if (files.Length != 0)
            {
                Console.WriteLine("error: files in outfilder : " + outFolder);
                System.Environment.Exit(-8);
            }

            files = Directory.GetFiles(inFolder);
            if (files.Length == 0 )
            {
                Console.WriteLine("error: No files in infolder : " + inFolder);
                System.Environment.Exit(-8);
            }


            const string quote = "\"";

            foreach ( string inFile in files)
            {
                string outFile;
                FileInfo f = new FileInfo(inFile);

                outFile = outFolder + Path.DirectorySeparatorChar + f.Name;

                doTrimAndVideoEnc(quote + inFile + quote, quote + outFile + quote, strFFmpegParms);
            }


            System.Environment.Exit(2);

        }

        static void TrimFileVildate( string[] parms, string strFFmpegParms)
        {
            string inFile;
            string outFile;

            inFile = "";
            outFile = "";

            for (int i = 0; i < parms.Length; i += 2)
            {
                string parm = parms[i];

                if (parm == "-i") {
                    inFile = parms[i + 1];
                } else if (parm == "-o") {
                    outFile = parms[i + 1];
                } else if (parm == "-cuda") {
                    if ( parms[i + 1] == "on" ) {
                        ClassFfmpegProc.useCuda = true;
                    } 
                }

            }

            if ( ( string.IsNullOrEmpty(inFile)) || (string.IsNullOrEmpty(outFile)) )
            {
                return;
            }

            if (File.Exists(outFile))
            {
                Console.WriteLine("please delete output file before encoding");
                // System.Environment.Exit(-4);
                File.Delete(outFile);
            }

            if (!File.Exists(inFile))
            {
                Console.WriteLine("No file was given for input");
                System.Environment.Exit(-5);
            }

            doTrimAndVideoEnc(inFile, outFile, strFFmpegParms);
            System.Environment.Exit(1);
        }

        static void doTrimAndVideoEnc( string inFile, string outFile, string strCustomParms)
        {
            string strParms;
         
            // Get ffmpeg trim parms for the video
            var procTrimVideo = ClassFfmpegProc.GetSilenceTrimBeignAndEnd(inFile, ClassFfmpegProc.DB.db50);
            procTrimVideo.Wait();
            strParms = procTrimVideo.Result;
            if ( string.IsNullOrEmpty(strParms))
            {
                System.Environment.Exit(-6);
            }

            strParms += " " + strCustomParms;
            strParms = strParms.Trim();

            // start doing encding;
            var procEncdoing = ClassFfmpegProc.runFFmpeg(inFile, outFile, strParms);
            procEncdoing.Wait();

        }

        static void help()
        {
            Console.WriteLine("Simple program that auto trim video begin and end for silence audio");
            Console.WriteLine("ffmpeg must be in same folder other wise it will fail");
            Console.WriteLine("");
            Console.WriteLine("Create a file call ffmpeg.txt in same folder as ffmpeg it should only contain the args/parms you want to use");
            Console.WriteLine("-i infile : the video file you want trim audio at begin and end of the video");
            Console.WriteLine("-o outfile : the out video after it got cut");
            Console.WriteLine("-cuda on : turn on cuda for video filter it adding -hwaccel cuda before -i when it is on other wise it add  -hwaccel auto");

            Console.WriteLine("");
            Console.WriteLine("-id infolder : a folder with video files see -i for more info");
            Console.WriteLine("-od outfolder : a folder to put output files in see -o for more info");


        }
    }
}