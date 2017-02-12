using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using MetadataExtractor.Formats.Jpeg;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ExifReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected Dictionary<string, string> allowedType;
        protected List<Objects.ExifData> data;
        protected bool isSingleFile;
        protected Exception lastException;

        public MainWindow()
        {
            InitializeComponent();

            InitAllowedImageType();

            txtBottomStatus.Text = "Supported formats: JPEG, JPG, TIFF, WEBP, PSD, PNG, BMP, GIF, ICO, PCX, NEF, CR2, ORF, ARW, RW2, RWL, SRW";
        }

        private void InitAllowedImageType()
        {
            //JPEG;*.JPG;*.TIFF;*.WEBP;*.PSD;*.PNG;*.BMP;*.GIF;*.ICO;*.PCX;*.NEF;*.CR2;*.ORF;*.ARW;*.RW2;*.RWL;*.SRW
            allowedType = new Dictionary<string, string>();
            allowedType.Add(".JEPG", ".jpeg");
            allowedType.Add(".JPG", ".jpg");
            allowedType.Add(".TIFF", ".tiff");
            allowedType.Add(".WebP", ".webp");
            allowedType.Add(".PSD", ".psd");
            allowedType.Add(".PNG", ".png");
            allowedType.Add(".GIF", ".gif");
            allowedType.Add(".ICO", ".ico");
            allowedType.Add(".PCX", ".pcx");
            allowedType.Add(".NEF", ".nef");
            allowedType.Add(".CR2", ".cr2");
            allowedType.Add(".ORF", ".orf");
            allowedType.Add(".ARW", ".arw");
            allowedType.Add(".RW2", ".rw2");
            allowedType.Add(".RWL", ".rwl");
            allowedType.Add(".SRW", ".srw");
        }

        private void ShowExifData(string filename)
        {
            if (IsImageFile(filename))
            {
                try
                {
                    txtExifFileSource.Text = System.IO.Path.GetFileName(filename);

                    data = new List<Objects.ExifData>();

                    IEnumerable<MetadataExtractor.Directory> metaData = ImageMetadataReader.ReadMetadata(filename);

                    if (metaData.Count() == 0)
                    {
                        txtFileStatus.Text = GetStatus(filename, metaData);
                        menuExport.IsEnabled = false;
                    }
                    else
                    {
                        foreach (var directory in metaData)
                        {
                            foreach (var tag in directory.Tags)
                            {
                                bool highlight = false;
                                switch (tag.Type)
                                {
                                    case ExifDirectoryBase.TagCameraOwnerName:
                                    case ExifDirectoryBase.TagMake:
                                    case ExifDirectoryBase.TagModel:
                                    case ExifDirectoryBase.TagCopyright:
                                    case ExifDirectoryBase.TagExifImageWidth:
                                    case ExifDirectoryBase.TagExifImageHeight:
                                    case ExifDirectoryBase.TagSoftware:
                                    case ExifDirectoryBase.TagDateTimeOriginal:
                                    case ExifDirectoryBase.TagDateTimeDigitized:
                                    case ExifDirectoryBase.TagDateTime:

                                    case MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight:
                                    case MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth:
                                        highlight = true;
                                        break;
                                    default:
                                        highlight = false;
                                        break;
                                }

                                string bgColor = highlight == true ? "#FF434A54" : "transparent";
                                string fgColor = highlight == true ? "#FFF6BB42" : "#FF8CC152";

                                var item = new Objects.ExifData()
                                {
                                    category = directory.Name,
                                    tag = tag.Name,
                                    value = tag.Description,
                                    backgroundColor = bgColor,
                                    foregroundColor = fgColor
                                };

                                data.Add(item);
                            }
                        }

                        txtFileStatus.Text = GetStatus(filename, metaData);
                        menuExport.IsEnabled = true;
                    }

                    lvExifData.ItemsSource = data;

                    CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvExifData.ItemsSource);
                    PropertyGroupDescription groupDescription = new PropertyGroupDescription("category");
                    view.GroupDescriptions.Add(groupDescription);
                }
                catch
                {
                    throw;
                }
            }
        }

        private bool IsImageFile(string filename)
        {
            string ext = System.IO.Path.GetExtension(filename).ToUpper();
            return allowedType.ContainsKey(ext);
        }

        private void PreviewImage(string filename, bool isSingleImage)
        {
            if (txtNoPreview.Visibility == Visibility.Visible)
                txtNoPreview.Visibility = Visibility.Collapsed;

            if (txtNoPreview2.Visibility == Visibility.Visible)
                txtNoPreview2.Visibility = Visibility.Collapsed;

            isSingleFile = isSingleImage;

            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(filename);
                bmp.EndInit();

                if (isSingleImage)
                    imagePreview2.Source = bmp;
                else
                    imagePreview.Source = bmp;

                // update file path
                tbSelectedImageFile.Text = filename;
            }
            catch
            {
                if (isSingleImage)
                {
                    imagePreview2.Visibility = Visibility.Collapsed;
                    txtNoPreview2.Text = "No image preview available";
                    txtNoPreview2.Visibility = Visibility.Visible;
                }
                else
                {
                    imagePreview.Visibility = Visibility.Collapsed;
                    txtNoPreview.Text = "No image preview available";
                    txtNoPreview.Visibility = Visibility.Visible;
                }
            }

            Mouse.OverrideCursor = null;
        }

        private string GetStatus(string filename, IEnumerable<MetadataExtractor.Directory> directories)
        {
            string status = string.Empty;
            txtFileStatus.Foreground = new SolidColorBrush(Colors.Black);
            try
            {
                if (directories.Count() == 0)
                {
                    status = "No EXIF data found.";
                    return status;
                }

                var subIfdDirectory = directories.OfType<MetadataExtractor.Formats.Exif.ExifSubIfdDirectory>().FirstOrDefault();

                //  check size
                var originalWidth = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageWidth);
                var originalHeight = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagExifImageHeight);

                var jpegDirectory = directories.OfType<JpegDirectory>().FirstOrDefault();
                var jpegWidth = jpegDirectory?.GetDescription(JpegDirectory.TagImageWidth);
                var jpegHeight = jpegDirectory?.GetDescription(JpegDirectory.TagImageHeight);

                if (originalWidth != null && jpegWidth != null)
                    if (originalWidth != jpegWidth)
                    {
                        status = "This image has been resized or cropped.";
                        return status;
                    }

                if (originalHeight != null && jpegHeight != null)
                    if (originalHeight != jpegHeight)
                    {
                        status = "This image has been resized or cropped.";
                        return status;
                    }

                //  check date time
                var dateTimeOriginalString = subIfdDirectory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);

                var fileDirectory = directories.OfType<FileMetadataDirectory>().FirstOrDefault();
                var dateTimeModifiedString = fileDirectory?.GetDescription(FileMetadataDirectory.TagFileModifiedDate);

                if (dateTimeOriginalString != null && dateTimeModifiedString != null)
                {
                    CultureInfo provider = CultureInfo.CurrentCulture;
                    var origDateTime = DateTime.ParseExact(dateTimeOriginalString, "yyyy:MM:dd HH:mm:ss", provider);
                    var modifiedTime = DateTime.ParseExact(dateTimeModifiedString, "ddd MMM dd HH:mm:ss K yyyy", provider);

                    if (modifiedTime - origDateTime > new TimeSpan(0, 0, 0, 1))
                    {
                        status = string.Format("This image might has been modified on {0}.", modifiedTime);
                        return status;
                    }
                }

                //  camera data not found but software
                var subIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                var cameraMake = subIfd0Directory?.GetDescription(ExifDirectoryBase.TagMake);
                var cameraModel = subIfd0Directory?.GetDescription(ExifDirectoryBase.TagModel);
                var software = subIfd0Directory?.GetDescription(ExifDirectoryBase.TagSoftware);

                if (cameraMake == null && cameraModel == null)
                {
                    if (software == null)
                    {
                        status = "No camera data found.";
                        return status;
                    }
                    else
                    {
                        status = string.Format("No camera data found. This image might be created by {0}.", software);
                        return status;
                    }
                }

                return "No data found that this image has been modified but be aware that editor can modify EXIF as well.";
            }
            catch (Exception ex)
            {
                txtFileStatus.Foreground = new SolidColorBrush(Colors.Red);
                status = ex.Message;
            }

            return status;
        }

        private void menuOpenSingleImageFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Supported Image Files|*.JPEG;*.JPG;*.TIFF;*.WEBP;*.PSD;*.PNG;*.BMP;*.GIF;*.ICO;*.PCX;*.NEF;*.CR2;*.ORF;*.ARW;*.RW2;*.RWL;*.SRW|" +
                "Supported Camera Raw Files|*.NEF;*.CR2;*.ORF;*.ARW;*.RW2;*.RWL;*.SRW|" +
                "JPEG Files (*.JPG;*.JPEG)|*.JPG;*.JPEG|" +
                "TIFF Files (*.TIFF)|*.TIFF|" +
                "WebP Files (*.WEBP)|*.WEBP|" +
                "PSD Files (*.PSD)|*.PSD|" +
                "PNG Files (*.PNG)|*.PNG|" +
                "BMP Files (*.BMP)|*.BMP|" +
                "GIF Files (*.GIF)|*.GIF|" +
                "ICO Files (*.ICO)|*.ICO|" +
                "PCX Files (*.PCX)|*.PCX|" +
                "Nikon Files (*.NEF)|*.NEF|" +
                "Canon Files (*.CR2)|*.CR2|" +
                "Olympus Files (*.ORF)|*.ORF|" +
                "Sony Files (*.ARW)|*.ARW|" +
                "Panasonic Files (*.RW2)|*.RW2|" +
                "Leica Files (*.RWL)|*.RWL|" +
                "Samsung Files (*.SRW)|*.SRW";
            bool? result = dlg.ShowDialog();

            if (result == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (gbFileList.Visibility == Visibility.Visible)
                    gbFileList.Visibility = Visibility.Collapsed;

                if (gbSingleImage.Visibility == Visibility.Collapsed)
                    gbSingleImage.Visibility = Visibility.Visible;

                if (overlay.Visibility == Visibility.Visible)
                    overlay.Visibility = Visibility.Collapsed;

                try
                {
                    // get filename
                    string filename = dlg.FileName;

                    // preview image
                    PreviewImage(filename, true);

                    // show exif data
                    ShowExifData(filename);

                    txtBottomStatus.Text = string.Format("Image file: {0}", filename);
                }
                catch (Exception ex)
                {
                    Mouse.OverrideCursor = null;

                    txtBottomStatus.Text = "Failed to open image file!";
                    lastException = ex;
                    btnShowException.Visibility = Visibility.Visible;
                }
            }
        }

        private void menuOpenImagesDir_Click(object sender, RoutedEventArgs e)
        {
            VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();
            bool? result = fbd.ShowDialog();

            if (result == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (gbSingleImage.Visibility == Visibility.Visible)
                    gbSingleImage.Visibility = Visibility.Collapsed;

                if (gbFileList.Visibility == Visibility.Collapsed)
                    gbFileList.Visibility = Visibility.Visible;

                if (overlay.Visibility == Visibility.Visible)
                    overlay.Visibility = Visibility.Collapsed;

                try
                {
                    // get dir
                    string dir = fbd.SelectedPath;

                    // populate file list
                    if (GetImageFiles(dir) == false)
                        Mouse.OverrideCursor = null;

                    txtBottomStatus.Text = string.Format("Images directory: {0}", dir);
                }
                catch (Exception ex)
                {
                    Mouse.OverrideCursor = null;

                    txtBottomStatus.Text = "Failed to open images directory!";
                    lastException = ex;
                    btnShowException.Visibility = Visibility.Visible;
                }
            }
        }

        private bool GetImageFiles(string dir)
        {
            // clear existing file list
            if (lbImageFileList.Items.Count > 0)
                lbImageFileList.Items.Clear();

            // get files
            var files = System.IO.Directory.GetFiles(dir);

            // show files count
            txtFilelistCount.Text = files.Length > 1 ? string.Format("Total files found: {0} image files", files.Length) : string.Format("Total files found: {0} image file", files.Length);

            // empty -> return
            if (files.Length == 0)
                return false;

            // loop each file
            foreach (var file in files)
            {
                var extension = System.IO.Path.GetExtension(file).ToUpper();
                if (allowedType.ContainsKey(extension))
                {
                    var filename = System.IO.Path.GetFileName(file);
                    var filesize = Helpers.BytesToString(new FileInfo(file).Length);
                    var lbItem = new ListBoxItem()
                    {
                        Content = filename + " • " + filesize,
                        Tag = file
                    };
                    lbImageFileList.Items.Add(lbItem);
                }
            }

            // select first file available
            if (lbImageFileList.Items.Count > 0)
                lbImageFileList.SelectedIndex = 0;

            return true;
        }

        private void lbImageFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as ListBox;
            var selectedItems = listbox.SelectedItems;

            if (selectedItems.Count == 1)
            {
                if (selectedItems[0] != null)
                {
                    string filename = ((ListBoxItem)selectedItems[0]).Tag.ToString();

                    if (File.Exists(filename))
                    {
                        // preview image
                        PreviewImage(filename, false);

                        // show exif data
                        ShowExifData(filename);

                        // focus the selection
                        ((ListBoxItem)selectedItems[0]).Focus();
                    }
                }
            }
        }

        private void menuExportTxt_Click(object sender, RoutedEventArgs e)
        {
            if (data != null && data.Count > 0 && tbSelectedImageFile.Text.Length > 0)
            {
                try
                {
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.Filter = "Text Files (*.TXT)|*.txt";
                    dlg.FileName = System.IO.Path.GetFileNameWithoutExtension(tbSelectedImageFile.Text) + "_EXIFDATA";
                    bool? result = dlg.ShowDialog();
                    if (result == true)
                    {
                        using (var tw = new StreamWriter(dlg.FileName))
                        {
                            foreach (var item in data)
                            {
                                tw.WriteLine(string.Format("{0} -- {1} -- {2}", item.category, item.tag, item.value));
                            }
                        }
                    }
                    txtBottomStatus.Text = string.Format("File exported: {0}", dlg.FileName);
                }
                catch (Exception ex)
                {
                    txtBottomStatus.Text = "Failed to export EXIF data to a TXT file!";
                    lastException = ex;
                    btnShowException.Visibility = Visibility.Visible;
                }
            }
        }

        private void menuExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (data != null && data.Count > 0 && tbSelectedImageFile.Text.Length > 0)
            {
                try
                {
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.Filter = "CSV Files (*.CSV)|*.CSV";
                    dlg.FileName = System.IO.Path.GetFileNameWithoutExtension(tbSelectedImageFile.Text) + "_EXIFDATA";
                    bool? result = dlg.ShowDialog();
                    if (result == true)
                    {
                        using (var tw = new StreamWriter(dlg.FileName))
                        {
                            foreach (var item in data)
                            {
                                tw.WriteLine(string.Format("\"{0}\",\"{1}\",\"{2}\"", item.category, item.tag, item.value));
                            }
                        }
                    }
                    txtBottomStatus.Text = string.Format("File exported: {0}", dlg.FileName);
                }
                catch (Exception ex)
                {
                    txtBottomStatus.Text = "Failed to export EXIF data to a CSV file!";
                    lastException = ex;
                    btnShowException.Visibility = Visibility.Visible;
                }
            }
        }

        private void ImagePreviewClicked(object sender, RoutedEventArgs e)
        {
            ImagePreview preview = new ImagePreview(this, tbSelectedImageFile.Text);
            preview.Show();
        }

        private void btnShowException_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, lastException.ToString(), lastException.Message, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void menuAbout_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, string.Format("Another EXIF Reader by Heiswayi Nrird, ver. {0}\n\nLoad image file, extract file metadata or EXIF data, preview the actual image, provide simple image analysis and export EXIF data into TXT/CSV file.\n\nThis program is freeware and open source.\nSource code: http://github.com/heiswayi/another-exif-reader", Assembly.GetExecutingAssembly().GetName().Version.ToString(2)), "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}