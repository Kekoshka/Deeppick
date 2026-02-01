using Deeppick.Interfaces;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Deeppick.Services
{
    public class FaceExtractService :IFaceExtractService
    {

        private List<byte[]> ExtractFacesFromVideo(int rate, int frameResolution, byte[] video)
        {
            var faces = new List<byte[]>();
            string tempVideoPath = Path.GetTempFileName() + ".mp4";
            File.WriteAllBytes(tempVideoPath, video);

            try
            {
                using (var capture = new VideoCapture(tempVideoPath))
                {
                    // Проверка успешности открытия видео
                    if (!capture.IsOpened)
                    {
                        throw new Exception("Не удалось открыть видеофайл");
                    }

                    double fps = capture.Get(CapProp.Fps);
                    if (fps <= 0) fps = 30;
                    int frameInterval = (int)(fps * rate / 1000);
                    if (frameInterval < 1) frameInterval = 1;

                    // Проверка наличия файла модели YuNet
                    string modelPath = "C:\\Users\\Kekoshka\\Source\\Repos\\Deeppick\\face_detection_yunet_2023mar.onnx";
                    if (!File.Exists(modelPath))
                    {
                        throw new FileNotFoundException($"Файл модели YuNet не найден: {modelPath}");
                    }

                    Mat frame = new Mat();
                    int currentFrame = 0;
                    FaceDetectorYN detector = null;

                    while (capture.Read(frame) && !frame.IsEmpty)
                    {
                        if (currentFrame % frameInterval == 0)
                        {
                            // Инициализация или обновление детектора YuNet (если изменился размер кадра)
                            if (detector == null || detector.InputSize.Width != frame.Width || detector.InputSize.Height != frame.Height)
                            {
                                detector?.Dispose();
                                detector = new FaceDetectorYN(
                                    model: modelPath,
                                    config: string.Empty,
                                    inputSize: new Size(frame.Width, frame.Height),
                                    scoreThreshold: 0.90f, // Понижен порог для лучшего обнаружения
                                    nmsThreshold: 0.5f,
                                    topK: 5000,
                                    backendId: Emgu.CV.Dnn.Backend.Default,
                                    targetId: Target.Cuda);
                            }

                            // Детекция лиц с помощью YuNet
                            using (var facesMat = new Mat())
                            {
                                detector.Detect(frame, facesMat);

                                if (facesMat.Rows > 0)
                                {
                                    // Извлекаем данные о лицах
                                    var facesData = ExtractFacesData(facesMat);

                                    Console.WriteLine($"Кадр {currentFrame}: найдено {facesData.Count} лиц");

                                    // Обработка каждого обнаруженного лица
                                    foreach (var faceInfo in facesData)
                                    {
                                        var faceRect = faceInfo.Rectangle;

                                        // Проверяем, что прямоугольник лица валидный
                                        if (faceRect.Width > 10 && faceRect.Height > 10 &&
                                            faceRect.X >= 0 && faceRect.Y >= 0 &&
                                            faceRect.X + faceRect.Width <= frame.Width &&
                                            faceRect.Y + faceRect.Height <= frame.Height)
                                        {
                                            // Вырезание и изменение размера области лица
                                            using (var faceMat = new Mat(frame, faceRect))
                                            using (var resizedFace = new Mat())
                                            {
                                                CvInvoke.Resize(faceMat, resizedFace,
                                                    new Size(frameResolution, frameResolution),
                                                    interpolation: Inter.Linear);

                                                // Преобразование в JPEG-байты
                                                using (var jpegBytes = new VectorOfByte())
                                                {
                                                    CvInvoke.Imencode(".jpg", resizedFace, jpegBytes,
                                                        new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 95));
                                                    faces.Add(jpegBytes.ToArray());
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        currentFrame++;
                    }

                    frame.Dispose();
                    detector?.Dispose();
                }
            }
            finally
            {
                if (File.Exists(tempVideoPath))
                    File.Delete(tempVideoPath);
            }

            return faces;
        }

        // Вспомогательный метод для извлечения данных о лицах из матрицы YuNet
        private List<FaceDetectionResult> ExtractFacesData(Mat facesMat)
        {
            var results = new List<FaceDetectionResult>();

            // Проверяем формат данных матрицы
            if (facesMat.Dims != 2 || facesMat.Cols < 15)
            {
                return results;
            }

            // Получаем данные из матрицы
            float[,] facesData = (float[,])facesMat.GetData(jagged: true);

            for (int i = 0; i < facesMat.Rows; i++)
            {
                try
                {
                    int x = (int)facesData[i, 0];
                    int y = (int)facesData[i, 1];
                    int width = (int)facesData[i, 2];
                    int height = (int)facesData[i, 3];
                    float confidence = facesData[i, 14];

                    // Отсеиваем низкокачественные детекции
                    if (confidence > 0.5f) // Пониженный порог уверенности для видео
                    {
                        results.Add(new FaceDetectionResult
                        {
                            Rectangle = new Rectangle(x, y, width, height),
                            Confidence = confidence
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке лица: {ex.Message}");
                }
            }

            return results;
        }

        // Класс для хранения результатов детекции
        public class FaceDetectionResult
        {
            public Rectangle Rectangle { get; set; }
            public float Confidence { get; set; }
        }

        public void GetImagesFromVideo(string inputPath, string outputPath, int rate, int resolution)
        {
            // Проверка входных параметров
            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentException("Input path cannot be null or empty", nameof(inputPath));

            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"Input video file not found: {inputPath}");

            if (rate <= 0)
                throw new ArgumentException("Rate must be greater than 0", nameof(rate));

            if (resolution <= 0)
                throw new ArgumentException("Resolution must be greater than 0", nameof(resolution));

            // Чтение видеофайла в массив байтов
            byte[] videoBytes;
            try
            {
                videoBytes = File.ReadAllBytes(inputPath);
                Console.WriteLine($"Video file loaded: {videoBytes.Length} bytes");
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to read video file: {ex.Message}", ex);
            }

            // Получение изображений лиц из видео
            List<byte[]> faceImages;
            try
            {
                faceImages = ExtractFacesFromVideo(rate, resolution, videoBytes);
                Console.WriteLine($"Extracted {faceImages.Count} face images");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract faces from video: {ex.Message}", ex);
            }

            // Сохранение изображений в выходной файл
            try
            {
                SaveFaceImages(faceImages, outputPath);
                Console.WriteLine($"Face images saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save face images: {ex.Message}", ex);
            }
        }

        private void SaveFaceImages(List<byte[]> faceImages, string outputPath)
        {
            if (faceImages == null || faceImages.Count == 0)
            {
                Console.WriteLine("No face images to save");
                return;
            }

            // Определяем формат выходного файла по расширению
            string extension = Path.GetExtension(outputPath).ToLower();

            if (extension == ".zip")
            {
                SaveAsZip(faceImages, outputPath);
            }
            else
            {
                // По умолчанию сохраняем как серию изображений в папке
                SaveAsImageSeries(faceImages, outputPath);
            }
        }

        private void SaveAsImageSeries(List<byte[]> faceImages, string outputPath)
        {
            // Если путь заканчивается расширением файла, создаем папку с таким же именем
            if (Path.HasExtension(outputPath))
            {
                string directory = Path.Combine(
                    Path.GetDirectoryName(outputPath),
                    Path.GetFileNameWithoutExtension(outputPath)
                );

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                outputPath = directory;
            }

            // Создаем папку если не существует
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            // Сохраняем каждое изображение как отдельный файл
            for (int i = 0; i < faceImages.Count; i++)
            {
                string imagePath = Path.Combine(outputPath, $"face_{Guid.NewGuid()}.jpg");
                File.WriteAllBytes(imagePath, faceImages[i]);
            }

            Console.WriteLine($"Saved {faceImages.Count} images to directory: {outputPath}");
        }

        private void SaveAsZip(List<byte[]> faceImages, string outputPath)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream,
                       System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    for (int i = 0; i < faceImages.Count; i++)
                    {
                        var entry = archive.CreateEntry($"face_{i:000}.jpg",
                            System.IO.Compression.CompressionLevel.Optimal);

                        using (var entryStream = entry.Open())
                        using (var imageStream = new MemoryStream(faceImages[i]))
                        {
                            imageStream.CopyTo(entryStream);
                        }
                    }
                }

                File.WriteAllBytes(outputPath, memoryStream.ToArray());
            }
        }

        public List<byte[]> ExtractFaceFromImage(byte[] image)
        {
            string modelPath = "C:\\Users\\Kekoshka\\Source\\Repos\\Deeppick\\face_detection_yunet_2023mar.onnx";
            Mat frame = new();
            CvInvoke.Imdecode(image,ImreadModes.ColorRgb, frame);
            var faces = new List<byte[]>();
            FaceDetectorYN detector = null;

            if (detector == null || detector.InputSize.Width != frame.Width || detector.InputSize.Height != frame.Height)
            {
                detector?.Dispose();
                detector = new FaceDetectorYN(
                    model: modelPath,
                    config: string.Empty,
                    inputSize: new Size(frame.Width, frame.Height),
                    scoreThreshold: 0.90f, // Понижен порог для лучшего обнаружения
                    nmsThreshold: 0.5f,
                    topK: 5000,
                    backendId: Emgu.CV.Dnn.Backend.Default,
                    targetId: Target.Cuda);
            }

            // Детекция лиц с помощью YuNet
            using (var facesMat = new Mat())
            {
                detector.Detect(frame, facesMat);

                if (facesMat.Rows > 0)
                {
                    // Извлекаем данные о лицах
                    var facesData = ExtractFacesData(facesMat);

                    // Обработка каждого обнаруженного лица
                    foreach (var faceInfo in facesData)
                    {
                        var faceRect = faceInfo.Rectangle;

                        // Проверяем, что прямоугольник лица валидный
                        if (faceRect.Width > 10 && faceRect.Height > 10 &&
                            faceRect.X >= 0 && faceRect.Y >= 0 &&
                            faceRect.X + faceRect.Width <= frame.Width &&
                            faceRect.Y + faceRect.Height <= frame.Height)
                        {
                            // Вырезание и изменение размера области лица
                            using (var faceMat = new Mat(frame, faceRect))
                            using (var jpegBytes = new VectorOfByte())
                            {
                                CvInvoke.Imencode(".jpg", faceMat, jpegBytes,
                                    new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 95));
                                faces.Add(jpegBytes.ToArray());
                            }
                        }
                    }
                }
            }
            frame.Dispose();
            detector?.Dispose();
            return faces;
        }
    }
}
