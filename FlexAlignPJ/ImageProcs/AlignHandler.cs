using OpenCvSharp;
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
        private readonly System.Windows.Media.Color _HistogramColor
            = System.Windows.Media.Colors.SlateBlue;
        private readonly int _HistogramWidth = 640;
        private readonly int _HistogramHeight = 320;

        private Mat _Source;
        private Mat _Target;
        private KeyPoint[] _KeyPointsSource;
        private KeyPoint[] _KeyPointsTarget;
        private DMatch[] _DMatches;
        private double _UsePointsRatio;

        /// <summary>各マッチング点の情報のリスト</summary>
        public ReadOnlySpan<DMatch> DMatches => _DMatches;

        /// <summary>マッチングした点対の数</summary>
        public int TotalMatchPointsCount => _DMatches.Length;

        /// <summary>アライメントに使うマッチング点の数の割合（0~1で指定）</summary>
        public double UsePointsRatio
        {
            get => _UsePointsRatio;
            set
            {
                if (value < 0) value = 0;
                if (1 < value) value = 1;
                _UsePointsRatio = value;
            }
        }

        /// <summary>アライメントに使うマッチング点の数。</summary>
        public int UsePointsCount => (int)(TotalMatchPointsCount * UsePointsRatio);

        /// <summary>距離（マッチングスコア）のヒストグラムを示す画像</summary>
        public WriteableBitmap MatchScoreHistogramBmp { get; set; }

        /// <summary>マッチング結果の可視化画像</summary>
        public WriteableBitmap MatchResultBmp { get; set; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public AlignHandler()
        {
            Init();
        }

        /// <summary>
        /// 各種オブジェクトをDisposeする。
        /// </summary>
        public void Dispose()
        {
            Init();
        }

        /// <summary>
        /// プロパティ・フィールドを初期化する。
        /// </summary>
        private void Init()
        {
            _Source?.Dispose();
            _Target?.Dispose();
            _KeyPointsSource = Array.Empty<KeyPoint>();
            _KeyPointsTarget = Array.Empty<KeyPoint>();
            _DMatches = Array.Empty<DMatch>();
            UsePointsRatio = 0.2;
            MatchScoreHistogramBmp = null;
            MatchResultBmp = null;
        }

        /// <summary>
        /// Source画像とTarget画像を更新する。
        /// </summary>
        /// <param name="source">元画像</param>
        /// <param name="target">合わせ込み先の画像</param>
        public void MatchImages(WriteableBitmap source, WriteableBitmap target)
        {
            _Source = source.ToMat();
            _Target = target.ToMat();
            Match(_Source, _Target);
            UpdateMatchingResultImage();
        }

        /// <summary>
        /// マッチング箇所可視化画像を更新する。
        /// </summary>
        public void UpdateMatchingResultImage()
        {
            using (var matchResult = new Mat())
            {
                var debugMatches = _DMatches
                    .OrderBy(s => s.Distance)
                    .Take(UsePointsCount)
                    .ToArray();
                Cv2.DrawMatches(_Source, _KeyPointsSource, _Target, _KeyPointsTarget, debugMatches, matchResult);
                MatchResultBmp = matchResult.ToWriteableBitmap();
            }
        }

        #region マッチング処理

        /// <summary>
        /// 2枚の画像の特徴点を検出し、マッチングまで実行する。
        /// </summary>
        /// <param name="source">元画像</param>
        /// <param name="target">合わせ込み先の画像</param>
        public void Match(Mat source, Mat target)
        {
            using (var descriptor1 = new Mat())
            using (var descriptor2 = new Mat())
            using (var result = new Mat())
            using (var akaze = AKAZE.Create())
            using (var matcher = DescriptorMatcher.Create("BruteForce"))
            {
                // 特徴点検出
                akaze.DetectAndCompute(source, null, out _KeyPointsSource, descriptor1);
                akaze.DetectAndCompute(target, null, out _KeyPointsTarget, descriptor2);
                _DMatches = matcher.Match(descriptor1, descriptor2)
                    .OrderBy(s => s.Distance)
                    .ToArray();
                MatchScoreHistogramBmp = GenDistanceHistogramBmp(_DMatches);
            }
        }

        /// <summary>
        /// 距離(マッチングスコア)のヒストグラム関係のプロパティを初期化する。
        /// </summary>
        /// <param name="matches"></param>
        private WriteableBitmap GenDistanceHistogramBmp(DMatch[] matches)
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

        #endregion
    }
}
