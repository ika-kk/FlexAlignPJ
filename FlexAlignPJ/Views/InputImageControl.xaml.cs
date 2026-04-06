using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FlexAlignPJ.Views
{
    /// <summary>
    /// InputImageControl.xaml の相互作用ロジック
    /// </summary>
    public partial class InputImageControl : UserControl
    {
        #region 依存プロパティ
        /// <summary>表示画像</summary>
        public WriteableBitmap Image
        {
            get { return (WriteableBitmap)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Image.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                nameof(Image),
                typeof(WriteableBitmap),
                typeof(InputImageControl),
                new PropertyMetadata(null, OnImageChanged));

        /// <summary>
        /// 画像が更新されたときに呼び出されるイベント
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // d はこのプロパティを持っているインスタンスそのもの（InputImageControl）
            var control = (InputImageControl)d;
            control.InitMask();
        }

        /// <summary>ROIデータ。双方向バインドで親方向へ伝播する。</summary>
        public OpenCvSharp.Rect Roi
        {
            get { return (OpenCvSharp.Rect)GetValue(RoiProperty); }
            set { SetValue(RoiProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Roi.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RoiProperty =
            DependencyProperty.Register(
                nameof(Roi),
                typeof(OpenCvSharp.Rect),
                typeof(InputImageControl),
                new FrameworkPropertyMetadata(default(OpenCvSharp.Rect), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        #endregion

        #region マスク関連のプロパティ
        private Mat _MaskMat;

        /// <summary>マスク画像</summary>
        public Mat MaskMat
        {
            get => _MaskMat;
            set
            {
                _MaskMat = value;
                MyMask.Source = value.ToWriteableBitmap();
            }
        }

        #endregion

        #region ROI描画イベント
        /// <summary>マウスが押されているかどうか</summary>
        private bool _IsMouseDown = false;

        /// <summary>マウスが直近押下された座標</summary>
        private (double x, double y) _ClickedPos;

        /// <summary>
        /// ROI描画イベント（マウス押下）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyMask_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(CanvasParent);
            _IsMouseDown = true;
            _ClickedPos = (pos.X, pos.Y);
            MyRect.Visibility = Visibility.Visible;

            //var posOnImage = GetImagePixelPosition(MyMask, pos.X, pos.Y);
        }

        /// <summary>
        /// ROI描画イベント（マウスリリース）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyMask_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _IsMouseDown = false;

            // Rectangleの情報取得
            var roiLeftTop = MyRect.TranslatePoint(new System.Windows.Point(0, 0), MyMask);

            var left = roiLeftTop.X;
            var top = roiLeftTop.Y;
            var width = MyRect.ActualWidth;
            var height = MyRect.ActualHeight;

            if (width > 1 && height > 1)
            {
                UpdateMask(left, top, width, height);
            }

            // Rectangle初期化
            MyRect.Visibility = Visibility.Hidden;
            Canvas.SetLeft(MyRect, 0);
            Canvas.SetTop(MyRect, 0);
            MyRect.Width = 0;
            MyRect.Height = 0;
        }

        /// <summary>
        /// ROI描画イベント（ドラッグ操作）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyMask_MouseMove(object sender, MouseEventArgs e)
        {
            if (_IsMouseDown)
            {
                var pos = e.GetPosition(CanvasParent);
                var currentPos = (pos.X, pos.Y);
                double left = currentPos.X < _ClickedPos.x ? currentPos.X : _ClickedPos.x;
                double top = currentPos.Y < _ClickedPos.y ? currentPos.Y : _ClickedPos.y;
                double width = Math.Abs(_ClickedPos.x - currentPos.X);
                double height = Math.Abs(_ClickedPos.y - currentPos.Y);
                Canvas.SetLeft(MyRect, left);
                Canvas.SetTop(MyRect, top);
                MyRect.Width = width;
                MyRect.Height = height;
            }
        }

        /// <summary>
        /// マスククリアボタン押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            InitMask();
        }
        #endregion

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public InputImageControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ROI初期化
        /// </summary>
        private void InitMask()
        {
            MaskMat?.Dispose();
            if (Image is not null)
            {
                Roi = new OpenCvSharp.Rect(0, 0, Image.PixelWidth, Image.PixelHeight);
                MaskMat = GenMaskMat((Image.PixelWidth, Image.PixelHeight), Roi);
            }
        }

        /// <summary>
        /// ROI描画時にマスクを更新する。
        /// </summary>
        /// <param name="renderRoiLeft">画面上のROI左上X座標</param>
        /// <param name="renderRoiTop">画面上のROI左上Y座標</param>
        /// <param name="renderRoiWidth">画面上のROI幅</param>
        /// <param name="renderRoiHeight">画面上のROI高さ</param>
        private void UpdateMask(double renderRoiLeft, double renderRoiTop, double renderRoiWidth, double renderRoiHeight)
        {
            // 表示上のサイズ
            double renderImageWidth = MyImage.ActualWidth;
            double renderImageHeight = MyImage.ActualHeight;

            // 元画像のピクセルサイズ
            double pixelWidth = Image.PixelWidth;
            double pixelHeight = Image.PixelHeight;

            // 画像座標に合わせたROI座標
            double left = (renderRoiLeft / renderImageWidth) * pixelWidth;
            double top = (renderRoiTop / renderImageHeight) * pixelHeight;
            double width = (renderRoiWidth / renderImageWidth) * pixelWidth;
            double height = (renderRoiHeight / renderImageHeight) * pixelHeight;
            Roi = new OpenCvSharp.Rect((int)left, (int)top, (int)width, (int)height);

            // マスク画像更新
            MaskMat?.Dispose();
            MaskMat = GenMaskMat(((int)pixelWidth, (int)pixelHeight), Roi);
        }

        /// <summary>
        /// マスク画像を生成する。
        /// </summary>
        /// <param name="imageSize">画像サイズ</param>
        /// <param name="roi">ROI</param>
        /// <returns>マスク画像</returns>
        private Mat GenMaskMat((int width, int height) imageSize, OpenCvSharp.Rect roi)
        {
            var alphaMat = new Mat(imageSize.height, imageSize.width, MatType.CV_8UC1);
            var rgbMat = new Mat(imageSize.height, imageSize.width, MatType.CV_8UC1);
            Cv2.BitwiseNot(alphaMat, alphaMat);
            Cv2.Rectangle(alphaMat, roi, new Scalar(0), -1);
            var mats = new Mat[]
            {
                rgbMat,rgbMat,rgbMat,alphaMat
            };
            var maskMat = new Mat();
            Cv2.Merge(mats, maskMat);
            int lineWidth = (int)(imageSize.width * 0.01);
            maskMat.Rectangle(roi, new Scalar(255, 255, 0, 255), lineWidth);
            alphaMat.Dispose();
            rgbMat.Dispose();
            return maskMat;
        }
    }
}
