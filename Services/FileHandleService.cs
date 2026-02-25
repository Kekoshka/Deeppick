using Deeppick.Interfaces;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using MediaFileProcessor;
using MediaFileProcessor.Models;
using MediaFileProcessor.Models.Common;
using MediaFileProcessor.Models.Enums;
using System.Drawing;
using System.Drawing.Imaging;


namespace Deeppick.Services
{
    public class FileHandleService : IFileHandleService
    {
        public FileHandleService() { }
        public string[] GetDirectoryFilesPaths(string directoryPath, string[] filesExtensions)
        {
            // Проверяем существование директории
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Директория не найдена: {directoryPath}");
            }

            // Если расширения не указаны, возвращаем все файлы
            if (filesExtensions == null || filesExtensions.Length == 0)
            {
                return Directory.GetFiles(directoryPath);
            }

            // Приводим расширения к нижнему регистру для сравнения
            var extensions = filesExtensions.Select(ext =>
                ext.StartsWith(".") ? ext.ToLower() : "." + ext.ToLower()).ToArray();

            // Получаем все файлы и фильтруем по расширениям
            var allFiles = Directory.GetFiles(directoryPath);
            var filteredFiles = allFiles.Where(file =>
            {
                string fileExtension = Path.GetExtension(file).ToLower();
                return extensions.Contains(fileExtension);
            }).ToArray();

            return filteredFiles;
        }

        /// <summary>
        /// Получение файла по указанному пути
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        /// <returns>Байтовый массив содержимого файла</returns>
        public byte[] GetFile(string filePath)
        {
            // Проверяем существование файла
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Файл не найден: {filePath}");
            }

            // Читаем все байты из файла
            return File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// Сохранение файла изображения по указанному пути
        /// </summary>
        /// <param name="image">Байтовый массив изображения</param>
        /// <param name="path">Путь для сохранения</param>
        /// <param name="fileName">Имя файла (без расширения или с расширением)</param>
        public void SaveImage(byte[] image, string path, string fileName)
        {
            // Проверяем директорию
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Формируем полный путь
            string fullPath = Path.Combine(path, fileName);

            // Записываем байты в файл
            File.WriteAllBytes(fullPath, image);
        }

        /// <summary>
        /// Сохранение массива файлов изображений по указанному пути
        /// </summary>
        /// <param name="imagesList">Список байтовых массивов изображений</param>
        /// <param name="path">Путь для сохранения</param>
        /// <param name="baseFileName">Базовое имя файлов (по умолчанию "image")</param>
        /// <param name="format">Формат изображений (по умолчанию jpeg)</param>
        public void SaveImages(List<byte[]> imagesList, string path,
                              string baseFileName = "image",
                              ImageFormat format = null)
        {
            if (imagesList == null || imagesList.Count == 0)
            {
                return;
            }

            // Создаем директорию, если не существует
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Сохраняем каждое изображение
            for (int i = 0; i < imagesList.Count; i++)
            {
                string fileName = $"{baseFileName}_{Guid.NewGuid()}.jpg";
                string fullPath = Path.Combine(path, fileName);
                File.WriteAllBytes(fullPath, imagesList[i]);
            }
        }

        /// <summary>
        /// Сохранение массива файлов изображений с автоматическим определением формата
        /// </summary>
        public void SaveImages(List<byte[]> imagesList, string path)
        {
            SaveImages(imagesList, path, "image", ImageFormat.Jpeg);
        }

        /// <summary>
        /// Получение изображений из видео с заданным временным промежутком
        /// </summary>
        /// <param name="video">Байтовый массив видеофайла</param>
        /// <param name="secondsRate">Интервал между кадрами в миллисекундах</param>
        /// <returns>Список байтовых массивов изображений в формате JPEG</returns>
        public List<byte[]> ExtractFramesFromVideo(byte[] video, int intervalMs)
        {
            var frames = new List<byte[]>();
            string tempVideoPath = Path.GetTempFileName() + ".mp4";
            File.WriteAllBytes(tempVideoPath, video);

            try
            {
                using (var capture = new VideoCapture(tempVideoPath))
                {
                    if (!capture.IsOpened)
                        throw new Exception("Не удалось открыть видеофайл");

                    double fps = capture.Get(CapProp.Fps);
                    if (fps <= 0) fps = 30;

                    // Рассчитываем, сколько кадров нужно пропускать для заданного интервала
                    int frameInterval = (int)(fps * intervalMs / 1000.0);
                    if (frameInterval < 1) frameInterval = 1;

                    Mat frame = new Mat();
                    int currentFrame = 0;

                    while (capture.Read(frame) && !frame.IsEmpty)
                    {
                        if (currentFrame % frameInterval == 0)
                        {
                            // Кодируем кадр в JPEG
                            using (var jpegBytes = new VectorOfByte())
                            {
                                CvInvoke.Imencode(".jpg", frame, jpegBytes,
                                    new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 95));
                                frames.Add(jpegBytes.ToArray());
                            }
                        }
                        currentFrame++;
                    }
                    frame.Dispose();
                }
            }
            finally
            {
                if (File.Exists(tempVideoPath))
                    File.Delete(tempVideoPath);
            }

            return frames;
        }
        public byte[] ResizeImage(int height, int width, byte[] imageByte)
        {
            Mat imageMat = new();
            CvInvoke.Imdecode(imageByte,ImreadModes.ColorRgb,imageMat);
            using (var resizedFace = new Mat())
            {
                CvInvoke.Resize(imageMat, resizedFace,
                    new Size(height, width),
                    interpolation: Inter.Linear);

                using (var jpegBytes = new VectorOfByte())
                {
                    CvInvoke.Imencode(".jpg", resizedFace, jpegBytes,
                        new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 95));
                    return jpegBytes.ToArray();
                }
            }
        }
        public List<byte[]> ResizeImageRange(int height, int width, List<byte[]> imagesBytes)
        {
            List<byte[]> resizedImages = new();
            foreach (var imageByte in imagesBytes)
                resizedImages.Add(ResizeImage(height, width, imageByte));
            return resizedImages;
        }
        public byte[] ResizeImage(int size, byte[] imageByte) => ResizeImage(size, size, imageByte);
        public List<byte[]> ResizeImageRange(int size, List<byte[]> imagesBytes) => ResizeImageRange(size, size, imagesBytes);
    }
}