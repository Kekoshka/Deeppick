using Deeppick.Interfaces;
using Deeppick.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IFaceExtractService,FaceExtractService>();
builder.Services.AddScoped<IDataAnalysisService, DataAnalysisService>();
builder.Services.AddScoped<IDataHandleService, DataHandleService>();
builder.Services.AddScoped<IFileHandleService, FileHandleService>();
builder.Services.AddScoped<INoiseExtractService, NoiseExtractService>();
builder.Services.AddSingleton<IMLService,MLService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Настройка логирования
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.MapControllers();

var dataDir = Path.Combine(app.Environment.ContentRootPath, "data");
var modelsDir = Path.Combine(app.Environment.ContentRootPath, "Models");

if (!Directory.Exists(dataDir))
{
    Directory.CreateDirectory(Path.Combine(dataDir, "train", "real"));
    Directory.CreateDirectory(Path.Combine(dataDir, "train", "fake"));
    Directory.CreateDirectory(Path.Combine(dataDir, "test", "real"));
    Directory.CreateDirectory(Path.Combine(dataDir, "test", "fake"));
}

if (!Directory.Exists(modelsDir))
    Directory.CreateDirectory(modelsDir);

Console.WriteLine($"Data directory: {dataDir}");
Console.WriteLine($"Models directory: {modelsDir}");


app.Run();


