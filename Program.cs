using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Saraff.Twain;

namespace tryTwain
{
    class Program
    {
        static float dpi = 150;
        static bool isSilenced = false;
        static TwPixelType pt = TwPixelType.Gray;

        static void Main(string[] args)
        {
            isSilenced = !(indexOf(args, "-s") >= 0);
            var d = indexOf(args, "-d");
            if (d >= 0 && args.Length > d + 1) float.TryParse(args[d + 1], out dpi);
            var p = indexOf(args, "-p");
            if (p >= 0 && args.Length > p + 1)
            {
                string val = args[p+1].ToUpper();

                if (val == "BW") pt = (TwPixelType)0;
                if (val == "Gray") pt = (TwPixelType)1;
                if (val == "RGB") pt = (TwPixelType)2;
                if (val == "Palette") pt = (TwPixelType)3;
                if (val == "CMY") pt = (TwPixelType)4;
                if (val == "CMYK") pt = (TwPixelType)5;
                if (val == "YUV") pt = (TwPixelType)6;
                if (val == "YUVK") pt = (TwPixelType)7;
                if (val == "CIEXYZ") pt = (TwPixelType)8;
                if (val == "LAB") pt = (TwPixelType)9;
                if (val == "SRGB") pt = (TwPixelType)10;
                if (val == "SCRGB") pt = (TwPixelType)11;
                if (val == "INFRARED") pt = (TwPixelType)16;
            }

            try
            {
                using (Twain32 twain = new Twain32())
                {
                    var _asm = twain.GetType().Assembly;
                    WriteMessage( "{1} {2}{0}{3}{0}", Environment.NewLine,
                        ((AssemblyTitleAttribute)_asm.GetCustomAttributes(typeof(AssemblyTitleAttribute), false)[0]).Title,
                        ((AssemblyFileVersionAttribute)_asm.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false)[0]).Version,
                        ((AssemblyCopyrightAttribute)_asm.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright);

                    twain.ShowUI = false;
                    WriteMessage("ShowUI: {0}", twain.ShowUI ? "true" : "false");

                    WriteMessage("IsTwain2Enable {0}: ", twain.IsTwain2Enable ? "[Y/n]" : "[y/N]");

                    if (isSilenced)
                        for (var _res = Console.ReadLine().Trim().ToUpper(); !string.IsNullOrEmpty(_res);)
                        {
                            twain.IsTwain2Enable = _res == "Y";
                            break;
                        }
                    else
                        twain.IsTwain2Enable = false;
                    WriteMessage("IsTwain2Enable: {0}", twain.IsTwain2Enable ? "true" : "false");

                    twain.OpenDSM();
                    WriteMessage("Select Data Source:");
                    if (isSilenced)
                        for (var i1 = 0; i1 < twain.SourcesCount; i1++)
                        {
                            WriteMessage("{0}: {1}{2}", i1, twain.GetSourceProductName(i1), twain.IsTwain2Supported && twain.GetIsSourceTwain2Compatible(i1) ? " (TWAIN 2.x)" : string.Empty);
                        }
                    WriteMessage("[{0}]: ", twain.SourceIndex);
                    if (isSilenced)
                        for (var _res = Console.ReadLine().Trim(); !string.IsNullOrEmpty(_res);)
                        {
                            twain.SourceIndex = Convert.ToInt32(_res);
                            break;
                        }
                    else
                        twain.SourceIndex = 0;
                    WriteMessage(string.Format("Data Source: {0}", twain.GetSourceProductName(twain.SourceIndex)));

                    twain.OpenDataSource();


                    WriteMessage("Select Resolution:");
                    var _resolutions = twain.Capabilities.XResolution.Get();

                    if (isSilenced)
                        for (var i1 = 0; i1 < _resolutions.Count; i1++)
                        {
                            WriteMessage(true, "{0}: {1} dpi", i1, _resolutions[i1]);
                        }
                    WriteMessage("[{0}]: ", _resolutions.CurrentIndex);
                    if (isSilenced)
                        for (var _res = Console.ReadLine().Trim(); !string.IsNullOrEmpty(_res);)
                        {
                            var _val = (float)_resolutions[Convert.ToInt32(_res)];
                            twain.Capabilities.XResolution.Set(_val);
                            twain.Capabilities.YResolution.Set(_val);
                            break;
                        }
                    else
                    {
                        twain.Capabilities.XResolution.Set(dpi);
                        twain.Capabilities.YResolution.Set(dpi);
                    }
                    WriteMessage("Resolution: {0}", twain.Capabilities.XResolution.GetCurrent());


                    WriteMessage("Select Pixel Type:");
                    var _pixels = twain.Capabilities.PixelType.Get();
                    if (isSilenced)
                        for (var i1 = 0; i1 < _pixels.Count; i1++)
                        {
                            WriteMessage("{0}: {1}", i1, _pixels[i1]);
                        }
                    WriteMessage("[{0}]: ", _pixels.CurrentIndex);
                    if (isSilenced)
                        for (var _res = Console.ReadLine().Trim(); !string.IsNullOrEmpty(_res);)
                        {
                            var _val = (TwPixelType)_pixels[Convert.ToInt32(_res)];
                            twain.Capabilities.PixelType.Set(_val);
                            break;
                        }
                    else
                    {
                        twain.Capabilities.PixelType.Set(pt);
                    }
                    WriteMessage(string.Format("Pixel Type: {0}", twain.Capabilities.PixelType.GetCurrent()));

                    twain.EndXfer += (object sender, Twain32.EndXferEventArgs e) =>
                    {
                        try
                        {
                            var _file = Path.Combine("", Path.ChangeExtension(Path.GetFileName(Path.GetTempFileName()), ".jpg"));
                            e.Image.Save(_file, ImageFormat.Jpeg);
                            WriteMessage(true, "Saved in: {0}", _file);
                            e.Image.Dispose();
                        }
                        catch (Exception ex)
                        {
                            WriteMessage("{0}: {1}{2}{3}{2}", ex.GetType().Name, ex.Message, Environment.NewLine, ex.StackTrace);
                        }
                    };

                    twain.AcquireCompleted += (sender, e) =>
                    {
                        try
                        {
                            WriteMessage("Acquire Completed.");
                        }
                        catch (Exception ex)
                        {
                            Program.WriteException(ex);
                        }
                    };

                    twain.AcquireError += (object sender, Twain32.AcquireErrorEventArgs e) =>
                    {
                        try
                        {
                            WriteMessage("Acquire Error: ReturnCode = {0}; ConditionCode = {1};", e.Exception.ReturnCode, e.Exception.ConditionCode);
                            Program.WriteException(e.Exception);
                        }
                        catch (Exception ex)
                        {
                            Program.WriteException(ex);
                        }
                    };

                    twain.Acquire();
                }
            }
            catch (Exception ex)
            {
                WriteException(ex);
            }

            if (isSilenced)
            {
                Console.WriteLine("{0}{1}", Environment.NewLine, "Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void WriteException(Exception ex)
        {
            for (var _ex = ex; _ex != null; _ex = _ex.InnerException)
            {
                WriteMessage("{0}: {1}{2}{3}{2}", _ex.GetType().Name, _ex.Message, Environment.NewLine, _ex.StackTrace);
            }
        }

        private static void WriteMessage(string message)
        {
            if (isSilenced) Console.WriteLine(message);
        }
        private static void WriteMessage(string message, params Object[] args)
        {
            if (isSilenced) Console.WriteLine(message, args);
        }
        private static void WriteMessage(bool forsed, string message, params Object[] args)
        {
            if (isSilenced || forsed) Console.WriteLine(message, args);
        }
        private static void WriteMessage(bool forsed, string message)
        {
            if (isSilenced || forsed) Console.WriteLine(message);
        }

        private static int indexOf(string[] args, string argKey)
        {
            return args.ToList().IndexOf(argKey);
        }
    }
}
