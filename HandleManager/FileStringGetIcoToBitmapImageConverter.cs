using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SiretT.Converters {
    public class FileStringGetIcoToBitmapImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value != null) {
                string path = value.ToString();
                if (File.Exists(path))
                    return FromStream(LargeIcon.ExtractAssociatedIcon(path as string, LargeIcon.IconSize.Size32x32).ToBitmap());
                string ext = System.IO.Path.GetExtension(path);
                return FromStream(LargeIcon.ExtractAssociatedIcon(ext, LargeIcon.IconSize.Size32x32).ToBitmap());
            }
            return FromStream(LargeIcon.ExtractAssociatedIcon(".exe", LargeIcon.IconSize.Size32x32).ToBitmap());
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value;
        }
        public static BitmapImage FromStream(System.Drawing.Image Image) {
            if (Image != null) {
                BitmapImage bmp = new BitmapImage();
                var image = new System.Drawing.Bitmap(Image);
                bmp.BeginInit();
                MemoryStream memoryStream = new MemoryStream();
                image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                bmp.StreamSource = memoryStream;
                bmp.EndInit();
                return bmp;
            }
            return null;
        }
        public static Icon BitmapToIcon(System.Drawing.Bitmap bitmap) {
            if (bitmap != null) {
                Icon ico = null;
                MemoryStream memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Icon);
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                ico = new Icon(memoryStream);
                return ico;
            }
            return null;
        }
    }
}