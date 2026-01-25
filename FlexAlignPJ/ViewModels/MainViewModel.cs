using CommunityToolkit.Mvvm.Input;
using FlexAlignPJ.ImageProcs;
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
        private AlignHandler _AlignHandler = new();

        #region バインドプロパティ

        private string _SourceImageFileName = string.Empty;
        private string _TargetImageFileName = string.Empty;
        private WriteableBitmap _SourceBmp;
        private WriteableBitmap _TargetBmp;
        private WriteableBitmap _MatchScoreHistogramBmp;
        private WriteableBitmap _MatchResultBmp;
        private int _TotalMatchPointsCount = 0;
        private double _UsePointsRatio = 0.2;
        private int _UsePointsCount = 0;
        private double _UsePointsPercent = 20;

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

        /// <summary>マッチングスコアのヒストグラム画像</summary>
        public WriteableBitmap MatchScoreHistogramBmp
        {
            get => _MatchScoreHistogramBmp; set => SetProperty(ref _MatchScoreHistogramBmp, value);
        }

        /// <summary>マッチング結果画像</summary>
        public WriteableBitmap MatchResultBmp
        {
            get => _MatchResultBmp; set => SetProperty(ref _MatchResultBmp, value);
        }

        /// <summary>マッチング点の総数</summary>
        public int TotalMatchPointsCount
        {
            get => _TotalMatchPointsCount; set => SetProperty(ref _TotalMatchPointsCount, value);
        }

        /// <summary>アライメントに使用するマッチング点の割合(0~1)</summary>
        public double UsePointsRatio
        {
            get => _UsePointsRatio;
            set
            {
                if (value < 0) value = 0;
                if (1 < value) value = 1;
                SetProperty(ref _UsePointsRatio, value);

                // 表示用の関連プロパティを更新
                UsePointsCount = (int)(TotalMatchPointsCount * value);
                UsePointsPercent = value * 100;
            }
        }

        /// <summary>アライメントに使用するマッチング点の数</summary>
        public int UsePointsCount
        {
            get => _UsePointsCount; set => SetProperty(ref _UsePointsCount, value);
        }

        /// <summary>アライメントに使用するマッチング点の割合[%]</summary>
        public double UsePointsPercent
        {
            get => _UsePointsPercent; set => SetProperty(ref _UsePointsPercent, value);
        }

        #endregion

        #region コマンド
        /// <summary>
        /// 画像ファイル参照コマンド
        /// </summary>
        /// <param name="sender">source or target</param>
        /// <exception cref="NotImplementedException"></exception>
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
                ofd.FilterIndex = 3;
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
                ofd.FilterIndex = 3;
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
        /// マッチングを実行する。
        /// </summary>
        [RelayCommand]
        private void ExecuteMatching()
        {
            if (SourceBmp is not null &&  TargetBmp is not null)
            {
                // マッチング設定・実行
                _AlignHandler.UsePointsRatio = UsePointsRatio;
                _AlignHandler.MatchImages(SourceBmp, TargetBmp);

                // 結果取得
                MatchScoreHistogramBmp = _AlignHandler.MatchScoreHistogramBmp;
                MatchResultBmp = _AlignHandler.MatchResultBmp;
                TotalMatchPointsCount = _AlignHandler.TotalMatchPointsCount;
                UsePointsCount = _AlignHandler.UsePointsCount;
            }
            else
            {
                MessageBox.Show("Invalid input images.", "Error");
            }
        }

        /// <summary>
        /// 現在の使用点数を反映してマッチング結果画像を更新する。
        /// </summary>
        [RelayCommand]
        private void UpdateMatchingImage()
        {
            try
            {
                _AlignHandler.UsePointsRatio = UsePointsRatio;
                _AlignHandler.UpdateMatchingResultImage();
                MatchResultBmp = _AlignHandler.MatchResultBmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        #endregion


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
