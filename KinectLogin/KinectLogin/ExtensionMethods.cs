using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Text.RegularExpressions;


namespace KinectLogin
{
    public static class ExtensionMethods
    {
        // Deep clone
        public static T DeepClone<T>(this T a)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                //XmlSerializer formatter = new XmlSerializer(typeof(Gesture));
                formatter.Serialize(stream, a);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }

        public static void timer(int seconds)
        {
            Thread.Sleep(seconds * 1000);
            //DateTime startTime = DateTime.Now;
            //DateTime currentTime = DateTime.Now;
            //while (currentTime.Second - startTime.Second < seconds)
            //{
            //    //if (currentTime.Second - startTime.Second % 1 == 0)
            //    //    System.Console.WriteLine(currentTime.Second - startTime.Second);
            //    currentTime = DateTime.Now;
            //}
        }

       public static void countdown()
        {
            for (int i = 5; i > 0; i--)
            {
                System.Console.Write(i);
                System.Threading.Thread.Sleep(250);
                System.Console.Write(".");
                System.Threading.Thread.Sleep(250);
                System.Console.Write(".");
                System.Threading.Thread.Sleep(250);
                System.Console.Write(".");
                System.Threading.Thread.Sleep(250);
            }
            System.Console.WriteLine("GO!");
        }

       public static Bitmap ToBitmap(this byte[] pixels, int width, int height, PixelFormat format)
       {
           if (pixels == null)
               return null;

           var bitmap = new Bitmap(width, height, format);

           var data = bitmap.LockBits(
               new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
               ImageLockMode.ReadWrite,
               bitmap.PixelFormat);

           Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);

           bitmap.UnlockBits(data);

           return bitmap;
       }

       public static Image<Gray, float> CalculatePCAofImage(Image<Gray, byte> TestIm, int WindowSize)
       {
           int sX = TestIm.Width;
           int sY = TestIm.Height;
           int i, j;
           Image<Gray, float> ImageFlo = TestIm.Convert<Gray, float>();
           Matrix<float> Window = new Matrix<float>(WindowSize, WindowSize);
           Matrix<float> Avg = new Matrix<float>(1, WindowSize);
           Matrix<float> EigVals = new Matrix<float>(1, WindowSize);
           Matrix<float> EigVects = new Matrix<float>(WindowSize, WindowSize);
           Matrix<float> PCAFeatures = new Matrix<float>(WindowSize, WindowSize);
           Image<Gray, float> TempIm = ImageFlo.CopyBlank();

           for (i = 0; i < (sX - WindowSize); i++)
           {
               for (j = 0; j < (sY - WindowSize); j++)
               {
                   CvInvoke.cvSetImageROI(ImageFlo, new Rectangle(new Point(i, j), new Size(WindowSize, WindowSize)));
                   CvInvoke.cvSetImageROI(TempIm, new Rectangle(new Point(i, j), new Size(WindowSize, WindowSize)));
                   CvInvoke.cvConvert(ImageFlo, Window);
                   CvInvoke.cvCalcPCA(Window, Avg, EigVals, EigVects, Emgu.CV.CvEnum.PCA_TYPE.CV_PCA_DATA_AS_ROW);
                   try
                   {
                       CvInvoke.cvProjectPCA(Window, Avg, EigVects, PCAFeatures);
                   }
                   catch (Exception e)
                   {
                       throw (e);
                   }
                   CvInvoke.cvConvert(PCAFeatures, TempIm);

                   CvInvoke.cvResetImageROI(ImageFlo);
                   CvInvoke.cvResetImageROI(TempIm);
               }
           }

           return (TempIm);
       }
       
       public static string getNameFromFile(string file)
       {
           string filenameWithoutPath = Path.GetFileName(file);
           string[] fileNameTokens = null;

           if (filenameWithoutPath.Contains('_'))
           {
               // Get the name from the file (the token before the first underscore)
               // We are expecting file names with the following format: <name>_<timestamp>.<extension>
               fileNameTokens = filenameWithoutPath.Split('_');
           }
           else
           {
               // Get the name from the file (the token before the timestamp)
               // We are expecting file names with the following format: <name><timestamp>.<extension>
               string pattern = @"\d+";
               Regex rgx = new Regex(pattern);

               fileNameTokens = rgx.Split(filenameWithoutPath);
           }

           if (fileNameTokens != null && fileNameTokens.Length > 0)
           {
               // The name is the first token in the filename
               return fileNameTokens[0];
           }
           else
           {
               // Fallback onto the filename
               return filenameWithoutPath;
           }
       }
    }
}
