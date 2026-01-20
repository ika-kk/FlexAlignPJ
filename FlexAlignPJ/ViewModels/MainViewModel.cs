using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FlexAlignPJ.ViewModels
{
    internal partial class MainViewModel : BindableBase
    {

        #region バインドプロパティ

        private string _SourceImageFileName = string.Empty;
        private string _TargetImageFileName = string.Empty;
        private WriteableBitmap _SourceBmp;
        private WriteableBitmap _TargetBmp;

        /// <summary>Source画像のファイル名</summary>
        public string SourceImageFileName
        {
            get => _SourceImageFileName; set => SetProperty(ref _SourceImageFileName, value);
        }

        /// <summary>Target画像のファイル名</summary>
        public string TargetImageFileName
        {
            get => _TargetImageFileName; set => SetProperty(ref _TargetImageFileName, value);
        }

        /// <summary>Source画像</summary>
        public WriteableBitmap SourceBmp
        {
            get => _SourceBmp; set => SetProperty(ref _SourceBmp, value);
        }

        /// <summary>Target画像</summary>
        public WriteableBitmap TargetBmp
        {
            get => _TargetBmp; set => SetProperty(ref _TargetBmp, value);
        }

        #endregion

        [RelayCommand]
        private void Reference(string sender)
        {
            if (sender == "source")
            {
                var ofd = new OpenFileDialog();
                ofd.Filter =
                    "bitmap(*.bmp)|*.bmp|" +
                    "png(*.png)|*.png|" +
                    "jpeg(*.jpg;*.jpeg)|*.jpg;*.jpeg";
                if (ofd.ShowDialog() == true)
                {
                    try
                    {
                        SourceBmp = ReadWriteableBitmap(ofd.FileName);
                        SourceImageFileName = Path.GetFileName(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (sender == "target")
            {
                var ofd = new OpenFileDialog();
                ofd.Filter =
                    "bitmap(*.bmp)|*.bmp|" +
                    "png(*.png)|*.png|" +
                    "jpeg(*.jpg;*.jpeg)|*.jpg;*.jpeg";
                if (ofd.ShowDialog() == true)
                {
                    try
                    {
                        TargetBmp = ReadWriteableBitmap(ofd.FileName);
                        TargetImageFileName = Path.GetFileName(ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 画像ファイルを読み込む。
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static WriteableBitmap ReadWriteableBitmap(string filePath)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(filePath, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            // フォーマット統一
            var convertedBmp = new FormatConvertedBitmap(bmp, System.Windows.Media.PixelFormats.Bgra32, null, 0);
            return new WriteableBitmap(convertedBmp);
        }
    }
}
