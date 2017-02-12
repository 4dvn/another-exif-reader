using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ExifReader
{
    /// <summary>
    /// Interaction logic for ImagePreview.xaml
    /// </summary>
    public partial class ImagePreview : Window
    {
        public ImagePreview(Window owner, string filename)
        {
            InitializeComponent();
            this.Owner = owner;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(filename);
            bmp.EndInit();

            var imageWidth = bmp.PixelWidth;
            var imageHeight = bmp.PixelHeight;
            this.Title = string.Format("{0} ({1}x{2} pixels)", System.IO.Path.GetFileName(filename), imageWidth, imageHeight);

            image.MaxWidth = imageWidth;
            image.MaxHeight = imageHeight;
            image.Source = bmp;
        }
    }
}