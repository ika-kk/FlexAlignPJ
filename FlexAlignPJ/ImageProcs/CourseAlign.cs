using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FlexAlignPJ.ImageProcs
{
    internal class CourseAlign : IDisposable
    {
        private DMatch[] _DMatches;
        private KeyPoint[] _KpsSource;
        private KeyPoint[] _KpsTarget;
        private Mat _Source;
        private Mat _Target;
        public WriteableBitmap MatchScoreHistogramBmp { get; set; }
        public WriteableBitmap MatchingResultBmp { get; set; }

        public int TotalMatchPointsCount { get; private set; }

        public double UsePointsRatio { get; set; } = 0.20;

        public void Dispose()
        {
            _DMatches = Array.Empty<DMatch>();
            _KpsSource = Array.Empty<KeyPoint>();
            _KpsTarget = Array.Empty<KeyPoint>();
            _Source?.Dispose();
            _Target?.Dispose();
            MatchScoreHistogramBmp = null;
            MatchingResultBmp = null;
        }

        /// 2枚の画像の粗マッチングを実行する。
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public void Match(Mat source, Mat target)
        {
            _Source?.Dispose();
            _Target?.Dispose();

            _Source = source;
            _Target = target;
            var infos = AlignProcs.Match(source, target);
            _DMatches = infos.dMatches;
            _KpsSource = infos.kpsSource;
            _KpsTarget = infos.kpsTarget;
            TotalMatchPointsCount = _DMatches.Length;

            MatchScoreHistogramBmp = AlignProcs.GenDistanceHistogramBmp(_DMatches);
            UpdateMatchingImage();
        }

        public void UpdateMatchingImage()
        {
            MatchingResultBmp = AlignProcs.GenMatchingResultImage(
                _Source,
                _KpsSource,
                _Target,
                _KpsTarget,
                _DMatches,
                (int)(_DMatches.Length * UsePointsRatio));
        }
    }
}
