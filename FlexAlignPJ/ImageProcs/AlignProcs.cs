using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FlexAlignPJ.ImageProcs
{
    internal static class AlignProcs
    {
        private static readonly System.Windows.Media.Color _HistogramColor
            = System.Windows.Media.Colors.SlateBlue;
        private static readonly int _HistogramWidth = 640;
        private static readonly int _HistogramHeight = 320;

        /// <summary>
        /// 2枚の画像の特徴点を検出し、変換用の配列を取得する。
        /// </summary>
        /// <param name="source">元画像</param>
        /// <param name="target">合わせ込み先の画像</param>
        /// <returns>対応点(source), 対応点(target), 変換情報</returns>
        public static (KeyPoint[] kpsSource, KeyPoint[] kpsTarget, DMatch[] dMatches) Match(Mat source, Mat target)
        {
            using (var descriptor1 = new Mat())
            using (var descriptor2 = new Mat())
            using (var result = new Mat())
            using (var akaze = AKAZE.Create())
            using (var matcher = DescriptorMatcher.Create("BruteForce"))
            {
                // 特徴点検出
                akaze.DetectAndCompute(source, null, out var kpsSource, descriptor1);
                akaze.DetectAndCompute(target, null, out var kpsTarget, descriptor2);
                var dMatches = matcher.Match(descriptor1, descriptor2)
                    .OrderBy(s => s.Distance)
                    .ToArray();
                return (kpsSource, kpsTarget, dMatches);
            }
        }

        /// <summary>
        /// 距離(マッチングスコア)のヒストグラム画像を生成する。
        /// </summary>
        /// <param name="matches"></param>
        public static WriteableBitmap GenDistanceHistogramBmp(DMatch[] matches)
        {
            var distances = matches
                .Select(s => s.Distance)
                .OrderBy(s => s)
                .ToArray();

            // 計算用の係数
            float cx = (distances.Length / (float)_HistogramWidth);
            float cy = _HistogramHeight / distances.Max();

            // 距離のヒストグラム画像作成
            var bmp = new WriteableBitmap(_HistogramWidth, _HistogramHeight, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            for (int x = 0; x < _HistogramWidth; x++)
            {
                int i = (int)(cx * x);
                int y = (int)(cy * distances[i]);
                bmp.DrawLine(x, _HistogramHeight, x, _HistogramHeight - y - 3, _HistogramColor);
            }
            return bmp;
        }

        /// <summary>
        /// マッチング箇所可視化画像を更新する。
        /// </summary>
        public static WriteableBitmap GenMatchingResultImage(Mat source, KeyPoint[] kpsSource, Mat target, KeyPoint[] kpsTarget, DMatch[] dMatches, int usePointsCount)
        {
            using (var matchResult = new Mat())
            {
                var debugMatches = dMatches
                    .OrderBy(s => s.Distance)
                    .Take(usePointsCount)
                    .ToArray();
                Cv2.DrawMatches(source, kpsSource, target, kpsTarget, debugMatches, matchResult);
                return matchResult.ToWriteableBitmap();
            }
        }

        public static void AlignSourceToTarget(Mat source, KeyPoint[] kpsSource, Mat target, KeyPoint[] kpsTarget, DMatch[] matches, int usePointsCount)
        {
            var useMatches = matches
                .OrderBy(s => s.Distance)
                .Take(usePointsCount).Select(s => (kpsSource[s.QueryIdx], kpsTarget[s.TrainIdx]))
                .ToArray();

            using (var sourcePoints = InputArray.Create(matches.Select(s => kpsSource[s.QueryIdx].Pt)))
            using (var targetPoints = InputArray.Create(matches.Select(s => kpsTarget[s.TrainIdx].Pt)))
            using (var homoMat = Cv2.FindHomography(sourcePoints, targetPoints, HomographyMethods.Ransac))
            {
                var warpedSource = new Mat();
                Cv2.WarpPerspective(source, warpedSource, homoMat, target.Size());
                // debug
                //warpedSource.ImWrite("_warped.png");
                //_Source.ImWrite("_source.png");
                //_Target.ImWrite("_target.png");

            }
        }
    }
}
