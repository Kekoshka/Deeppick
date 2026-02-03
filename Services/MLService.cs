using Microsoft.ML;
using Microsoft.ML.Data;
using Deeppick.Models;
using Microsoft.AspNetCore.Hosting;
using Deeppick.Interfaces;
using Microsoft.ML.Vision;

namespace Deeppick.Services
{
    public class MLService : IMLService
    {
        private readonly IWebHostEnvironment _environment;
        private MLContext _mlContext;
        private TrainingStatus _trainingStatus;
        private ITransformer _loadedModel;
        private PredictionEngine<ModelInput, ModelOutput> _predictionEngine;

        // Классы для ML.NET
        private class ModelInput
        {
            [LoadColumn(0)]
            public string ImagePath { get; set; }

            [LoadColumn(1)]
            public string Label { get; set; }
        }

        private class ModelOutput
        {
            [ColumnName("PredictedLabel")]
            public string Prediction { get; set; }

            public float[] Score { get; set; }
        }

        public MLService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _mlContext = new MLContext(seed: 0);
            _trainingStatus = new TrainingStatus
            {
                Status = "Idle",
                Progress = 0,
                IsComplete = true
            };
        }

        public async Task<TrainingResponse> TrainModelAsync(TrainingRequest request)
        {
            try
            {
                _trainingStatus = new TrainingStatus
                {
                    Status = "Initializing",
                    Progress = 0,
                    CurrentOperation = "Preparing data",
                    IsComplete = false,
                    LastUpdated = DateTime.UtcNow
                };

                // 1. Подготовка путей
                var fullTrainingPath = Path.Combine(_environment.ContentRootPath, request.TrainingDataPath);
                var fullTestPath = Path.Combine(_environment.ContentRootPath, request.TestDataPath);
                var fullModelPath = Path.Combine(_environment.ContentRootPath, request.ModelSavePath);

                // Создание директории для модели
                var modelDir = Path.GetDirectoryName(fullModelPath);
                if (!Directory.Exists(modelDir))
                    Directory.CreateDirectory(modelDir);

                _trainingStatus.Progress = 10;
                _trainingStatus.CurrentOperation = "Loading training data";
                _trainingStatus.LastUpdated = DateTime.UtcNow;

                // 2. Загрузка данных
                var trainingData = LoadImageData(fullTrainingPath);
                var testData = LoadImageData(fullTestPath);

                if (!trainingData.Any())
                    throw new Exception("No training data found");

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
                var shuffledData = _mlContext.Data.ShuffleRows(dataView);

                _trainingStatus.Progress = 30;
                _trainingStatus.CurrentOperation = "Building pipeline";
                _trainingStatus.LastUpdated = DateTime.UtcNow;

                var options = new ImageClassificationTrainer.Options()
                {
                    // Используйте правильное имя свойства
                    WorkspacePath = Path.Combine(Path.GetTempPath(), "ML_ImageClassification"),

                    // Архитектура модели
                    Arch = ImageClassificationTrainer.Architecture.ResnetV2101, // Поле Arch существует и корректно[citation:1][citation:10]

                    // Гиперпараметры
                    Epoch = request.Epochs,
                    BatchSize = request.BatchSize,
                    LearningRate = 0.001f,

                    // Настройка ранней остановки
                    EarlyStoppingCriteria = new ImageClassificationTrainer.EarlyStopping()
                    {
                        MinDelta = 0.001f,
                        Patience = 5,
                        // Укажите, улучшение какой метрики ожидаем:
                        // true для Accuracy (по умолчанию), false для Loss
                        CheckIncreasing = true
                    },

                    // Столбцы данных
                    LabelColumnName = "LabelKey",
                    FeatureColumnName = "Image",

                    ReuseTrainSetBottleneckCachedValues = true,
                    ReuseValidationSetBottleneckCachedValues = true
                };

                // 4. Создание конвейера
                var pipeline = _mlContext.Transforms
    .LoadRawImageBytes(
        outputColumnName: "Image",
        imageFolder: null,
        inputColumnName: nameof(ModelInput.ImagePath))
    .Append(_mlContext.Transforms.Conversion.MapValueToKey(
        outputColumnName: "LabelKey",
        inputColumnName: nameof(ModelInput.Label)))
    // Передайте объект options в метод ImageClassification
    .Append(_mlContext.MulticlassClassification.Trainers.ImageClassification(options))
    .Append(_mlContext.Transforms.Conversion.MapKeyToValue(
        outputColumnName: nameof(ModelOutput.Prediction),
        inputColumnName: "PredictedLabel"));

                // 4. Обучение модели
                var model = pipeline.Fit(shuffledData);

                _trainingStatus.Progress = 80;
                _trainingStatus.CurrentOperation = "Evaluating model";
                _trainingStatus.LastUpdated = DateTime.UtcNow;

                // 5. Оценка модели
                var testDataView = _mlContext.Data.LoadFromEnumerable(testData);
                var predictions = model.Transform(testDataView);
                var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

                _trainingStatus.Progress = 90;
                _trainingStatus.CurrentOperation = "Saving model";
                _trainingStatus.LastUpdated = DateTime.UtcNow;

                // 6. Сохранение модели
                _mlContext.Model.Save(model, shuffledData.Schema, fullModelPath);
                _loadedModel = model;

                _trainingStatus.Progress = 100;
                _trainingStatus.Status = "Completed";
                _trainingStatus.CurrentOperation = "Training completed";
                _trainingStatus.IsComplete = true;
                _trainingStatus.LastUpdated = DateTime.UtcNow;

                return new TrainingResponse
                {
                    Status = "Success",
                    Message = "Model trained successfully",
                    Accuracy = metrics.MacroAccuracy,
                    LogLoss = metrics.LogLoss,
                    ModelPath = request.ModelSavePath,
                    TrainingCompleted = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _trainingStatus.Status = "Failed";
                _trainingStatus.CurrentOperation = $"Error: {ex.Message}";
                _trainingStatus.IsComplete = true;
                _trainingStatus.LastUpdated = DateTime.UtcNow;

                return new TrainingResponse
                {
                    Status = "Error",
                    Message = $"Training failed: {ex.Message}",
                    Accuracy = 0,
                    LogLoss = 0
                };
            }
        }

        public async Task<PredictionResponse> PredictAsync(PredictionRequest request, Stream imageStream)
        {
            try
            {
                // Загрузка модели если она еще не загружена
                if (_loadedModel == null || !_loadedModel.GetType().Name.Contains("Transformer"))
                {
                    var modelPath = Path.Combine(_environment.ContentRootPath, request.ModelPath);

                    if (!File.Exists(modelPath))
                        throw new FileNotFoundException($"Model file not found: {modelPath}");

                    DataViewSchema modelSchema;
                    _loadedModel = _mlContext.Model.Load(modelPath, out modelSchema);
                }

                // Сохранение временного файла для предсказания
                var tempFilePath = Path.GetTempFileName() + ".jpg";
                using (var fileStream = File.Create(tempFilePath))
                {
                    await imageStream.CopyToAsync(fileStream);
                }

                // Создание prediction engine
                if (_predictionEngine == null)
                {
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_loadedModel);
                }

                // Создание input для модели
                var input = new ModelInput
                {
                    ImagePath = tempFilePath,
                    Label = "" // Метка не требуется для предсказания
                };

                // Предсказание
                var prediction = _predictionEngine.Predict(input);

                // Очистка временного файла
                File.Delete(tempFilePath);

                return new PredictionResponse
                {
                    Filename = Path.GetFileName(tempFilePath),
                    Prediction = prediction.Prediction,
                    RealConfidence = prediction.Score.Length > 0 ? prediction.Score[0] : 0,
                    FakeConfidence = prediction.Score.Length > 1 ? prediction.Score[1] : 0,
                    ProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Prediction failed: {ex.Message}", ex);
            }
        }

        public async Task<float> PredictImageAsync(byte[] imageBytes, string modelPath = "Models/deepfake_model.zip")
        {
            if(_loadedModel is null)
            {
                var fullModelPath = Path.Combine(_environment.ContentRootPath, modelPath);
                if (!File.Exists(fullModelPath))
                    throw new FileNotFoundException($"Model file not found: {fullModelPath}");
                _loadedModel = _mlContext.Model.Load(fullModelPath, out var modelSchema);
            }
            if(_predictionEngine is null)
            {
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_loadedModel);
            }

            var tempFilePath = Path.GetTempFileName();
            var tempImagePath = Path.ChangeExtension(tempFilePath, ".jpg");

            // Перемещаем временный файл с правильным расширением
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);

            // Сохраняем массив байт в файл
            await File.WriteAllBytesAsync(tempImagePath, imageBytes);

            try
            {
                // 3. Проверка, что файл создан и не пустой
                var fileInfo = new FileInfo(tempImagePath);
                if (!fileInfo.Exists || fileInfo.Length == 0)
                    throw new InvalidDataException("Failed to create temporary image file");

                // 4. Создание входных данных
                var input = new ModelInput
                {
                    ImagePath = tempImagePath,
                    Label = string.Empty // Метка не нужна для предсказания
                };

                // 5. Выполнение предсказания
                var prediction = _predictionEngine.Predict(input);

                if (prediction == null || prediction.Score == null || prediction.Score.Length < 2)
                    throw new InvalidOperationException("Invalid prediction result");

                // 6. Получаем вероятность "real" (не дипфейк)
                // Предполагаем, что:
                // - Score[0] = вероятность класса "real" (настоящее)
                // - Score[1] = вероятность класса "fake" (дипфейк)
                var realProbability = prediction.Score[0];

                // 7. Дополнительная проверка на валидность вероятности
                if (realProbability < 0 || realProbability > 1)
                {
                    // Нормализуем, если значения выходят за пределы [0, 1]
                    var sum = prediction.Score[0] + prediction.Score[1];
                    if (sum > 0)
                        realProbability = prediction.Score[0] / sum;
                    else
                        realProbability = 0.5f; // Неопределенность
                }

                return realProbability;
            }
            finally
            {
                if (File.Exists(tempImagePath))
                    File.Delete(tempImagePath);
            }
        }

        public TrainingStatus GetTrainingStatus()
        {
            return _trainingStatus;
        }

        public List<string> GetAvailableModels()
        {
            var modelsPath = Path.Combine(_environment.ContentRootPath, "Models");
            if (!Directory.Exists(modelsPath))
                return new List<string>();

            return Directory.GetFiles(modelsPath, "*.zip")
                .Select(Path.GetFileName)
                .ToList();
        }

        public bool ModelExists(string modelPath)
        {
            var fullPath = Path.Combine(_environment.ContentRootPath, modelPath);
            return File.Exists(fullPath);
        }

        private List<ModelInput> LoadImageData(string folderPath)
        {
            var data = new List<ModelInput>();

            if (!Directory.Exists(folderPath))
                return data;

            // Загрузка реальных лиц
            var realPath = Path.Combine(folderPath, "real");
            if (Directory.Exists(realPath))
            {
                var realImages = Directory.GetFiles(realPath, "*.jpg")
                    .Concat(Directory.GetFiles(realPath, "*.png"))
                    .Concat(Directory.GetFiles(realPath, "*.jpeg"))
                    .Select(path => new ModelInput
                    {
                        ImagePath = path,
                        Label = "real"
                    });
                data.AddRange(realImages);
            }

            // Загрузка дипфейков
            var fakePath = Path.Combine(folderPath, "fake");
            if (Directory.Exists(fakePath))
            {
                var fakeImages = Directory.GetFiles(fakePath, "*.jpg")
                    .Concat(Directory.GetFiles(fakePath, "*.png"))
                    .Concat(Directory.GetFiles(fakePath, "*.jpeg"))
                    .Select(path => new ModelInput
                    {
                        ImagePath = path,
                        Label = "fake"
                    });
                data.AddRange(fakeImages);
            }

            return data;
        }
    }
}