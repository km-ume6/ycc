using OpenCvSharp;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace FirstClassLibrary
{
    public static class ImageTools
    {
        public static bool AddWafer { get; set; } = true;
        public static bool AddHistogram { get; set; } = false;

        /// <summary>
        /// 画像をOpenCVでクロップする
        /// </summary>
        public static Mat? CropImageByOpenCV(string imagePath)
        {
            Mat? circle = AddWafer ? CropImageToCircle(imagePath) : null;
            Mat? rect = AddHistogram ? CropImageToRect(imagePath) : null;

            if (circle != null && rect != null)
                return CombineImages(circle, rect);

            return circle ?? rect;
        }

        /// <summary>
        /// 画像から最初に検出された円をクロップして、結果の画像を返します。
        /// </summary>
        /// <remarks>
        /// このメソッドは指定されたパスから画像を読み込み、画像内の円を検出し、最初に検出された円で画像をクロップします。
        /// 円が検出されない場合やエラーが発生した場合は、<see langword="null"/> を返します。
        /// </remarks>
        /// <param name="imagePath">処理する画像ファイルのパス。null または空文字列は不可。</param>
        /// <returns>
        /// 円が検出された場合はクロップされた円形画像の <see cref="Mat"/> オブジェクト、
        /// それ以外の場合は <see langword="null"/>。
        /// </returns>
        public static Mat? CropImageToCircle(string imagePath)
        {
            try
            {
                using var cvImage = Cv2.ImRead(imagePath);
                if (cvImage.Empty())
                    return LogAndReturnNull("画像の読み込みに失敗しました。");

                using var gray = ConvertToGrayScale(cvImage);
                var circles = DetectCircles(gray);

                if (circles.Length > 0)
                    return CropAndResizeImage(cvImage, circles[0]);
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        /// <summary>
        /// 画像から矩形領域（主にヒストグラム領域）を検出し、クロップして返します。
        /// </summary>
        /// <param name="imagePath">処理する画像ファイルのパス。null または空文字列は不可。</param>
        /// <returns>
        /// 矩形領域が検出された場合はクロップ・リサイズされた画像の <see cref="Mat"/> オブジェクト、
        /// それ以外の場合は <see langword="null"/>。
        /// </returns>
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static Mat? CropImageToRect(string imagePath)
        {
            try
            {
                using var cvImage = Cv2.ImRead(imagePath);
                if (cvImage.Empty())
                    return LogAndReturnNull("画像の読み込みに失敗しました。");

                using var gray = ConvertToGrayScale(cvImage);
                using var blurred = new Mat();
                Cv2.GaussianBlur(gray, blurred, new Size(5, 5), 0);
                using var edged = new Mat();
                Cv2.Canny(blurred, edged, 75, 200);

                Cv2.FindContours(edged, out Point[][] contours, out _, RetrievalModes.List, ContourApproximationModes.ApproxSimple);

                foreach (var contour in contours)
                {
                    var approx = Cv2.ApproxPolyDP(contour, 0.02 * Cv2.ArcLength(contour, true), true);
                    if (approx.Length == 4)
                    {
                        var rect = Cv2.BoundingRect(approx);
                        if (rect.Width >= 940 && rect.Height >= 260)
                        {
                            using var cropped = new Mat(cvImage, rect);
                            int targetWidth = 800;
                            int targetHeight = (int)(cropped.Height * (targetWidth / (double)cropped.Width));
                            return ResizeImage(cropped, targetWidth, targetHeight);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return null;
        }

        /// <summary>
        /// 2つの画像(Mat)を縦方向に結合し、1枚の画像として返します。
        /// 画像の幅は2つのうち大きい方に合わせ、高さは合計となります。
        /// </summary>
        /// <param name="image1">上部に配置する画像。</param>
        /// <param name="image2">下部に配置する画像。</param>
        /// <returns>結合された画像(Mat)。</returns>
        public static Mat CombineImages(Mat image1, Mat image2)
        {
            int width = Math.Max(image1.Width, image2.Width);
            int height = image1.Height + image2.Height;
            var combined = new Mat(new Size(width, height), image1.Type());

            image1.CopyTo(new Mat(combined, new Rect(0, 0, image1.Width, image1.Height)));
            image2.CopyTo(new Mat(combined, new Rect(0, image1.Height, image2.Width, image2.Height)));

            return combined;
        }

        /// <summary>
        /// カラー画像をグレースケール画像に変換します。
        /// </summary>
        /// <param name="cvImage">BGRカラーフォーマットの入力画像。null不可。</param>
        /// <returns>入力画像のグレースケール版を表す新しい <see cref="Mat"/> オブジェクト。</returns>
        private static Mat ConvertToGrayScale(Mat cvImage)
        {
            var gray = new Mat();
            Cv2.CvtColor(cvImage, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }

        /// <summary>
        /// 円を検出するためのHough変換を使用します。
        /// </summary>
        /// <param name="grayImage"></param>
        /// <returns></returns>
        private static CircleSegment[] DetectCircles(Mat grayImage)
        {
            return Cv2.HoughCircles(
                grayImage,
                HoughModes.Gradient,
                dp: 1,
                minDist: 100,
                param1: 100,
                param2: 30,
                minRadius: 190,
                maxRadius: 210
            );
        }

        /// <summary>
        /// 指定された円で画像をクロップし、400x400にリサイズします。
        /// </summary>
        /// <param name="cvImage"></param>
        /// <param name="circle"></param>
        /// <returns></returns>
        private static Mat CropAndResizeImage(Mat cvImage, CircleSegment circle)
        {
            using var cropped = CropImage(cvImage, circle);
            return ResizeImage(cropped, 400, 400);
        }

        /// <summary>
        /// 指定された円の中心と半径を使用して画像をクロップします。
        /// </summary>
        /// <param name="cvImage"></param>
        /// <param name="circle"></param>
        /// <returns></returns>
        private static Mat CropImage(Mat cvImage, CircleSegment circle)
        {
            int x = (int)circle.Center.X;
            int y = (int)circle.Center.Y;
            int r = (int)circle.Radius;

            using var mask = new Mat(cvImage.Size(), MatType.CV_8UC3, Scalar.All(0));
            Cv2.Circle(mask, new Point(x, y), r, Scalar.All(255), thickness: -1);

            using var masked = new Mat();
            Cv2.BitwiseAnd(cvImage, mask, masked);

            int xMin = Math.Max(x - r, 0);
            int yMin = Math.Max(y - r, 0);
            int width = Math.Min(2 * r, cvImage.Width - xMin);
            int height = Math.Min(2 * r, cvImage.Height - yMin);

            return new Mat(masked, new Rect(xMin, yMin, width, height));
        }

        /// <summary>
        /// 画像を指定された幅と高さにリサイズします。
        /// </summary>
        /// <param name="image"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private static Mat ResizeImage(Mat image, int width, int height)
        {
            int newWidth, newHeight;
            if (image.Width > image.Height)
            {
                newWidth = width;
                newHeight = (int)(image.Height * (width / (double)image.Width));
            }
            else
            {
                newHeight = height;
                newWidth = (int)(image.Width * (height / (double)image.Height));
            }

            var resized = new Mat();
            Cv2.Resize(image, resized, new Size(newWidth, newHeight));
            return resized;
        }

        /// <summary>
        /// 指定された画像ファイルをクロップし、結果を新しいファイルに保存します。
        /// </summary>
        /// <param name="srcPath"></param>
        /// <param name="dstPath"></param>
        /// <returns></returns>
        public static bool CropToFile(string srcPath, string dstPath)
        {
            using var mat = CropImageByOpenCV(srcPath);
            if (mat == null)
            {
                Console.WriteLine("画像のクロップに失敗しました。");
                return false;
            }
            Cv2.ImWrite(dstPath, mat);
            return true;
        }

        /// <summary>
        /// ログメッセージを出力し、nullを返します。 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static Mat? LogAndReturnNull(string message)
        {
            Console.WriteLine(message);
            return null;
        }

        /// <summary>
        /// エラーをログに出力します。
        /// </summary>
        /// <param name="ex"></param>
        private static void LogError(Exception ex)
        {
            Console.WriteLine($"画像の処理中にエラーが発生しました！: {ex.Message}");
        }
    }
}
