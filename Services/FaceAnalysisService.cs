using System.Drawing;
using System;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace Deeppick.Services
{
    public class FaceAnalysisService
    {

        private List<byte[]> ExtractFacesFromVideo(int rate, int frameResolution, byte[] video)
    {
        var faces = new List<byte[]>();

        // Создаем временный файл для видео
        string tempVideoPath = Path.GetTempFileName() + ".mp4";
        File.WriteAllBytes(tempVideoPath, video);

        try
        {
            using (var capture = new VideoCapture(tempVideoPath))
            {
                // Получаем FPS видео
                double fps = capture.Get(CapProp.Fps);
                if (fps <= 0) fps = 30; // Значение по умолчанию

                // Рассчитываем интервал кадров на основе rate (в миллисекундах)
                int frameInterval = (int)(fps * rate / 1000);
                if (frameInterval < 1) frameInterval = 1;

                int currentFrame = 0;
                Mat frame = new Mat();

                // Загружаем каскадный классификатор для обнаружения лиц
                using (var faceClassifier = new CascadeClassifier("haarcascade_frontalface_default.xml"))
                {
                    while (capture.Read(frame) && !frame.IsEmpty)
                    {
                        // Пропускаем кадры согласно rate
                        if (currentFrame % frameInterval == 0)
                        {
                            // Обнаруживаем лица на кадре
                            var grayFrame = new Mat();
                            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
                            CvInvoke.EqualizeHist(grayFrame, grayFrame);

                            Rectangle[] detectedFaces = faceClassifier.DetectMultiScale(
                                grayFrame,
                                scaleFactor: 1.1,
                                minNeighbors: 5,
                                minSize: new Size(30, 30)
                            );

                            // Обрабатываем каждое обнаруженное лицо
                            foreach (var faceRect in detectedFaces)
                            {
                                // Вырезаем область лица
                                using (var faceMat = new Mat(frame, faceRect))
                                {
                                    // Изменяем размер до квадрата
                                    using (var resizedFace = new Mat())
                                    {
                                        CvInvoke.Resize(faceMat, resizedFace,
                                            new Size(frameResolution, frameResolution),
                                            interpolation: Inter.Linear);

                                        // Конвертируем в массив байтов
                                        using (var ms = new MemoryStream())
                                        {

                                            var bitmap = resizedFace.ToImage<Bgr, byte>();
                                            faces.Add(bitmap.Bytes);
                                        }
                                    }
                                }
                            }

                            grayFrame.Dispose();
                        }

                        currentFrame++;
                    }
                }

                frame.Dispose();
            }
        }
        finally
        {
            // Удаляем временный файл
            if (File.Exists(tempVideoPath))
                File.Delete(tempVideoPath);
        }

        return faces;
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
                string imagePath = Path.Combine(outputPath, $"face_{i:000}.jpg");
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
    }
}
