using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace HGP.Web.Utilities
{
    public class ImageUtilities
    {
        public Size ResizeKeepAspect(Size CurrentDimensions, int maxWidth, int maxHeight)
        {
            int newHeight = CurrentDimensions.Height;
            int newWidth = CurrentDimensions.Width;
            if (maxWidth > 0 && newWidth > maxWidth) //WidthResize
            {
                Decimal divider = Math.Abs((Decimal) newWidth/(Decimal) maxWidth);
                newWidth = maxWidth;
                newHeight = (int) Math.Round((Decimal) (newHeight/divider));
            }
            if (maxHeight > 0 && newHeight > maxHeight) //HeightResize
            {
                Decimal divider = Math.Abs((Decimal) newHeight/(Decimal) maxHeight);
                newHeight = maxHeight;
                newWidth = (int) Math.Round((Decimal) (newWidth/divider));
            }
            return new Size(newWidth, newHeight);
        }


        public MemoryStream GenerateThumbNail(Stream imageData, ImageFormat format, int height, int width)
        {

            //using (Image img = Image.FromStream(imageData))
            //{
            //    Image thumbNail = img.GetThumbnailImage(width, height, new Image.GetThumbnailImageAbort(GenerateThumbNailAbort), IntPtr.Zero);

            //    // make a memory stream to work with the image bytes
            //    var imageStream = new MemoryStream();

            //    // put the image into the memory stream
            //    thumbNail.Save(imageStream, ImageFormat.Jpeg);

            //    return imageStream;

            //}

            var image = Image.FromStream(imageData);
            var newSize = this.ResizeKeepAspect(image.Size, width, height);

            
            var imageStream = new MemoryStream();
            Bitmap newImage = new Bitmap(newSize.Width, newSize.Height);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(image, new Rectangle(0, 0, newSize.Width, newSize.Height));

                newImage.Save(imageStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                return imageStream;
            }


        }

        public bool GenerateThumbNailAbort()
        {
            return true;
        }
    }
}



namespace MyPhotos.Common
{
    public class ThumbCreator
    {

        public enum VerticalAlign
        {
            Top,
            Middle,
            Bottom
        }

        public enum HorizontalAlign
        {
            Left,
            Middle,
            Right
        }

        

        public void Convert(string sourceFile, string targetFile, ImageFormat targetFormat, int height, int width, VerticalAlign valign, HorizontalAlign halign)
        {
            using (Image img = Image.FromFile(sourceFile))
            {
                using (Image targetImg = Convert(img, height, width, valign, halign))
                {
                    string directory = Path.GetDirectoryName(targetFile);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    if (targetFormat == ImageFormat.Jpeg)
                    {
                        SaveJpeg(targetFile, targetImg, 100);
                    }
                    else
                    {
                        targetImg.Save(targetFile, targetFormat);
                    }
                }
            }
        }

        /// <summary> 
        /// Saves an image as a jpeg image, with the given quality 
        /// </summary> 
        /// <param name="path">Path to which the image would be saved.</param> 
        // <param name="quality">An integer from 0 to 100, with 100 being the 
        /// highest quality</param> 
        public static void SaveJpeg(string path, Image img, int quality)
        {
            if (quality < 0 || quality > 100)
                throw new ArgumentOutOfRangeException("quality must be between 0 and 100.");


            // Encoder parameter for image quality 
            EncoderParameter qualityParam =
                new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            // Jpeg image codec 
            ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = qualityParam;

            img.Save(path, jpegCodec, encoderParams);
        }

        /// <summary> 
        /// Returns the image codec with the given mime type 
        /// </summary> 
        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            // Get image codecs for all image formats 
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            // Find the correct image codec 
            for (int i = 0; i < codecs.Length; i++)
                if (codecs[i].MimeType == mimeType)
                    return codecs[i];
            return null;
        }

        public Image Convert(Image img, int height, int width, VerticalAlign valign, HorizontalAlign halign)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                float ratio = (float)height / (float)img.Height;
                int temp = (int)((float)img.Width * ratio);
                if (temp == width)
                {
                    //no corrections are needed!
                    g.DrawImage(img, 0, 0, width, height);
                    return result;
                }
                else if (temp > width)
                {
                    //den e för bred!
                    int overFlow = (temp - width);
                    if (halign == HorizontalAlign.Middle)
                    {
                        g.DrawImage(img, 0 - overFlow / 2, 0, temp, height);
                    }
                    else if (halign == HorizontalAlign.Left)
                    {
                        g.DrawImage(img, 0, 0, temp, height);
                    }
                    else if (halign == HorizontalAlign.Right)
                    {
                        g.DrawImage(img, -overFlow, 0, temp, height);
                    }
                }
                else
                {
                    //den e för hög!
                    ratio = (float)width / (float)img.Width;
                    temp = (int)((float)img.Height * ratio);
                    int overFlow = (temp - height);
                    if (valign == VerticalAlign.Top)
                    {
                        g.DrawImage(img, 0, 0, width, temp);
                    }
                    else if (valign == VerticalAlign.Middle)
                    {
                        g.DrawImage(img, 0, -overFlow / 2, width, temp);
                    }
                    else if (valign == VerticalAlign.Bottom)
                    {
                        g.DrawImage(img, 0, -overFlow, width, temp);
                    }
                }
            }
            return result;
        }
    }
}