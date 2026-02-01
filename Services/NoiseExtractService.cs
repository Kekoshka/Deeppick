using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Diagnostics;
using System.Drawing;

namespace Deeppick.Services
{
    public class NoiseExtractService
    {
        private int _iterations = 1;
        private double _errorScale = 1.0;
        private bool _equalizeHistogram = true;
        private bool _isDisposed = false;

        /// <summary>
        /// Количество итераций медианного фильтра (1-10)
        /// </summary>
        public int Iterations
        {
            get => _iterations;
            set => _iterations = Math.Max(1, Math.Min(10, value));
        }

        /// <summary>
        /// Коэффициент усиления шума (0.1-100)
        /// </summary>
        public double ErrorScale
        {
            get => _errorScale;
            set => _errorScale = Math.Max(0.1, Math.Min(100.0, value));
        }

        /// <summary>
        /// Включить выравнивание гистограммы
        /// </summary>
        public bool EqualizeHistogram
        {
            get => _equalizeHistogram;
            set => _equalizeHistogram = value;
        }

        /// <summary>
        /// Основной метод сервиса для обработки изображения
        /// Принимает массив байтов изображения, обрабатывает и возвращает массив байтов результата
        /// </summary>
        public async Task<byte[]> ProcessImageAsync(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes cannot be null or empty");

            return await Task.Run(() => ProcessImage(imageBytes));
        }

        /// <summary>
        /// Синхронная версия обработки изображения
        /// </summary>
        public byte[] ProcessImage(byte[] imageBytes)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(NoiseExtractService));

            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("Image bytes cannot be null or empty");

            Debug.WriteLine($"Starting noise analysis: iterations={_iterations}, scale={_errorScale}, equalize={_equalizeHistogram}");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Загружаем изображение из массива байтов
                using (var original = LoadImageFromBytes(imageBytes))
                {
                    // Обрабатываем изображение
                    using (var resultMat = ProcessImageInternal(original))
                    {
                        // Конвертируем результат обратно в массив байтов
                        byte[] resultBytes = SaveImageToBytes(resultMat);

                        stopwatch.Stop();
                        Debug.WriteLine($"Noise analysis completed in {stopwatch.ElapsedMilliseconds}ms");

                        return resultBytes;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ProcessImage: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Загрузка изображения из массива байтов
        /// </summary>
        private Mat LoadImageFromBytes(byte[] imageBytes)
        {
            using (var ms = new MemoryStream(imageBytes))
            {
                Mat mat = new();
                CvInvoke.Imdecode(imageBytes,ImreadModes.ColorRgb, mat);
                if (mat.IsEmpty)
                    throw new ArgumentException("Failed to decode image from bytes");

                return mat;
            }
        }

        /// <summary>
        /// Сохранение изображения в массив байтов
        /// </summary>
        private byte[] SaveImageToBytes(Mat image)
        {
            // Используем Imencode для кодирования изображения в формате PNG
            // Можно изменить на другие форматы (JPEG, etc.) при необходимости
            using (var vector = new VectorOfByte())
            {
                CvInvoke.Imencode(".png", image, vector);
                return vector.ToArray();
            }
        }

        /// <summary>
        /// Основной процесс обработки изображения
        /// </summary>
        private Mat ProcessImageInternal(Mat original)
        {
            using (var smoothed = ApplyMedianFilter(original, _iterations))
            using (var noise = ComputeNoise(original, smoothed))
            {
                // Усиление шума
                if (Math.Abs(_errorScale - 1.0) > 0.001)
                {
                    EnhanceNoise(noise, _errorScale);
                }

                // Выравнивание гистограммы
                if (_equalizeHistogram)
                {
                    EqualizeHistogramM(noise);
                }

                return noise.Clone();
            }
        }

        /// <summary>
        /// Применение медианного фильтра
        /// </summary>
        private Mat ApplyMedianFilter(Mat source, int iterations)
        {
            var result = source.Clone();

            for (int i = 0; i < iterations; i++)
            {
                using (var temp = new Mat())
                {
                    CvInvoke.MedianBlur(result, temp, 3);
                    result = temp.Clone();
                }
            }

            return result;
        }

        /// <summary>
        /// Вычисление разницы (шума) между оригиналом и сглаженным изображением
        /// </summary>
        private Mat ComputeNoise(Mat original, Mat smoothed)
        {
            var noise = new Mat();
            CvInvoke.AbsDiff(original, smoothed, noise);
            return noise;
        }

        /// <summary>
        /// Усиление шума с заданным коэффициентом
        /// </summary>
        private void EnhanceNoise(Mat noise, double scale)
        {
            using (var floatMat = new Mat())
            {
                noise.ConvertTo(floatMat, DepthType.Cv32F);
                CvInvoke.Multiply(floatMat, new ScalarArray(scale), floatMat);
                CvInvoke.ConvertScaleAbs(floatMat, noise, 1, 0);
            }
        }

        /// <summary>
        /// Выравнивание гистограммы для каждого канала
        /// </summary>
        private void EqualizeHistogramM(Mat image)
        {
            if (image.NumberOfChannels == 1)
            {
                CvInvoke.EqualizeHist(image, image);
            }
            else if (image.NumberOfChannels == 3)
            {
                using (var channels = new VectorOfMat())
                {
                    CvInvoke.Split(image, channels);

                    for (int i = 0; i < channels.Size; i++)
                    {
                        CvInvoke.EqualizeHist(channels[i], channels[i]);
                    }

                    CvInvoke.Merge(channels, image);
                }
            }
        }

        #region IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        ~NoiseExtractService()
        {
            Dispose(false);
        }
        #endregion
    }
}
