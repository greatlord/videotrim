using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FFmpeg.NET;
using FFmpeg.NET.Enums;
using FFmpeg.NET.Events;
using FFmpeg.NET.Exceptions;
using FFmpeg.NET.Extensions;
using FFmpeg.NET.Models;
using FFmpeg.NET.Services;
using System;



namespace TrimVideo
{
    internal class ClassFfmpegProc
    {

        public static string ffmpegPath = "ffmpeg.exe";
        private static List<string> strSilencedetect = new List<string>();

        static public bool useCuda = false;
        static public async Task runFFmpeg(string inVideoFile, string outVideoFile, string parms )
        {
            Engine ffmpeg;
            
            ffmpeg = new Engine(ffmpegPath);

            ffmpeg.Progress += OnProgress;
            ffmpeg.Data += OnData;
            ffmpeg.Complete += OnComplete;
            ffmpeg.Error += OnError;

            if ( useCuda ) {
                parms = "-hwaccel cuda " + "-i " + inVideoFile + " " + parms + " " + outVideoFile;
            } else {
                parms = "-hwaccel auto -i " + inVideoFile + " " + parms + " " + outVideoFile;
            }
            
            await ffmpeg.ExecuteAsync(parms, default).ConfigureAwait(false);
        }

        private static void OnProgress(object sender, ConversionProgressEventArgs e)
        {
           
            //Console.WriteLine("[{0} => {1}]", e.Input.Name, e.Output?.Name);
            Console.WriteLine("Bitrate: {0}", e.Bitrate);
            Console.WriteLine("Fps: {0}", e.Fps);
            Console.WriteLine("Frame: {0}", e.Frame);
            Console.WriteLine("ProcessedDuration: {0}", e.ProcessedDuration);
            Console.WriteLine("Size: {0} kb", e.SizeKb);
            Console.WriteLine("TotalDuration: {0}\n", e.TotalDuration);
            
            
        }

        private static void OnData(object sender, ConversionDataEventArgs e)
        {
            // Console.WriteLine("[{0} => {1}]: {2}", e.Input.Name, e.Output?.Name, e.Data);
        }


        private static void OnComplete(object sender, ConversionCompleteEventArgs e)
        {
            //Console.WriteLine("Completed conversion from {0} to {1}", e.Input.Name, e.Output?.Name);
        }


        private static void OnError(object sender, ConversionErrorEventArgs e)
        {
            //Console.WriteLine("[{0} => {1}]: Error: {2}\n{3}", e.Input.Name, e.Output?.Name, e.Exception.ExitCode, e.Exception.InnerException);
        }




        static public async Task<string> GetSilenceTrimBeignAndEnd( string inVideoFile, DB db )
        {

            Engine ffmpeg;
            string args;
            StringBuilder s;


            // always clear and init global strSilencedetect 
            strSilencedetect = new List<string>();

            // use string builder it is faster that + 
            s = new StringBuilder();

            s.Append("-hide_banner ");

            // input file
            s.Append( "-i " + inVideoFile  );

            // no video
            s.Append(" -vn ");

            // detect silence at begin and end of video using dB for analog audio and digtial defualt is rms works only for digtal audio
            switch ( db )
            {
                case DB.db30:
                    s.Append("-af \"silencedetect=n=-30dB\" ");
                    break;

                case DB.db35:
                    s.Append("-af \"silencedetect=n=-35dB\" ");
                    break;

                case DB.db40:
                    s.Append("-af \"silencedetect=n=-35dB\" ");
                    break;

                default:
                    s.Append("-af \"silencedetect=n=-50dB\" ");
                    break;
            }

            // We do not need a outfile so we send it to null:
            s.Append(" -f null - ");

            // setup xffmpeg library
            ffmpeg = new Engine(ffmpegPath);
            ffmpeg.Data += OnDataTrimInfo;

            string parms = s.ToString();

            // execute ffmpeg with our own custom parms
            await ffmpeg.ExecuteAsync(parms, default).ConfigureAwait(false);

            // Get the trim parms for begin and of the video. if no silence found at begin or end no parms are set then
            args = "";
            if ( strSilencedetect.Count >= 6 )
            {
                
                string strBeignEnd;
                string strEndStart;
                

                // the string strSilencedetect contain 3 post for each silence it find
                // pos 0 : silence_start
                // pos 1 : silence_end
                // pos 2 : durations of the silence

                // get the frist silence it found and start the cut where it ends
                strBeignEnd = strSilencedetect[1].Replace("end:", "").Trim();

                // get last silence start and end the cut at that point
                strEndStart = strSilencedetect[strSilencedetect.Count - 3].Replace("start:", "").Trim();

                args += "-ss " + strBeignEnd;
                args += " -to " + strEndStart;

            }

            return args;

        }

        private static void OnDataTrimInfo(object sender, ConversionDataEventArgs e)
        {
            // Console.WriteLine("[{0} => {1}]: {2}", e.Input.Name, e.Output?.Name, e.Data);
            // "[silencedetect @ 00000159f14d7ec0] silence_start: 0.0231066"
            if (!string.IsNullOrEmpty(e.Data))
            {
                if (e.Data.StartsWith("[silencedetect"))
                {
                    if (e.Data.Contains("silence_start"))
                    {
                        string s = e.Data.Split("silence_")[1];
                        strSilencedetect.Add(s);
                    }

                    if (e.Data.Contains("silence_end"))
                    {
                        string s = e.Data.Split("silence_")[1].Replace("|", "").Trim();
                        strSilencedetect.Add(s);
                        s = e.Data.Split("silence_")[2];
                        strSilencedetect.Add(s);
                    }



                }
            }

        }




          






        public enum DB
        {
            db50 = 0, // Cuts are almost not recognizable.
            db40 = 1, // Cuts are almost not recognizable.
            db35 = 2, //Cut inhaling breath before speaking; cuts are quite recognizable.
            db30 = 3, // Cut Mouse clicks and mouse movent; cuts are very recognizable."

        }

        private bool isUnix { 
            get
            {
                var os = Environment.OSVersion;
                if (os.Platform == PlatformID.Unix)
                {
                    return true;
                }
                return false;
            }
        }

        private bool isWindows {

            get
            {
                var os = Environment.OSVersion;
                if (os.Platform == PlatformID.Win32NT)
                {
                    return true;
                }
                return false;
            }
        }


    }
}
