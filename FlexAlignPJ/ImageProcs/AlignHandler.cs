using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FlexAlignPJ.ImageProcs
{
    internal class AlignHandler : IDisposable
    {
        private CourseAlign _CourseAlign;
        private double _UsePointsRatioCourse;

        /// <summary>マッチングした点対の数</summary>
        public int TotalMatchPointsCountCourse => _CourseAlign.TotalMatchPointsCount;

        /// <summary>粗アライメントに使うマッチング点の数の割合（0~1で指定）</summary>
        public double UsePointsRatioCourse
        {
            get => _UsePointsRatioCourse;
            set
            {
                if (value < 0) value = 0;
                if (1 < value) value = 1;
                _UsePointsRatioCourse = value;
            }
        }

        /// <summary>粗アライメントに使うマッチング点の数</summary>
        public int UsePointsCountCourse => (int)(TotalMatchPointsCountCourse * UsePointsRatioCourse);

        /// <summary>距離（マッチングスコア）のヒストグラムを示す画像</summary>
        public WriteableBitmap MatchScoreHistogramBmpCourse => _CourseAlign?.MatchScoreHistogramBmp;

        /// <summary>マッチング結果の可視化画像</summary>
        public WriteableBitmap MatchResultBmp => _CourseAlign?.MatchingResultBmp;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AlignHandler()
        {
        }

        public void Dispose()
        {
            _CourseAlign?.Dispose();
        }

        /// <summary>
        /// 2枚の画像の粗マッチングを実行する。
        /// </summary>
        /// <param name="source">元画像</param>
        /// <param name="target">合わせ込み先画像</param>
        public void CourseMatchImages(WriteableBitmap source, WriteableBitmap target)
        {
            CourseMatchImages(source.ToMat(), target.ToMat());
        }

        /// <summary>
        /// 2枚の画像の粗マッチングを実行する。
        /// </summary>
        /// <param name="source">元画像</param>
        /// <param name="target">合わせ込み先画像</param>
        /// <param name="sourceRoi">元画像のROI</param>
        /// <param name="sourceRoi">合わせ込み先画像のROI</param>
        public void CourseMatchImages(WriteableBitmap source, WriteableBitmap target, OpenCvSharp.Rect sourceRoi, OpenCvSharp.Rect targetRoi)
        {
            using (var tempSource = source.ToMat())
            using (var tempTarget = target.ToMat())
            {
                CourseMatchImages(new Mat(tempSource, sourceRoi), new Mat(tempTarget, targetRoi));
            }
        }

        /// <summary>
        /// 2枚の画像の粗マッチングを実行する。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        private void CourseMatchImages(Mat source, Mat target)
        {
            _CourseAlign?.Dispose();
            _CourseAlign = new CourseAlign();
            _CourseAlign.UsePointsRatio = UsePointsRatioCourse;
            _CourseAlign.Match(source, target);
        }

        /// <summary>
        /// 粗マッチングの対応画像を更新する。
        /// </summary>
        public void UpdateMatchingResultImageCourse()
        {
            if (_CourseAlign is not null)
            {
                _CourseAlign.UsePointsRatio = UsePointsRatioCourse;
                _CourseAlign.UpdateMatchingImage();
            }
        }
    }
}
