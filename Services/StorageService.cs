using System.Text.Json;
using System.Text.Json.Serialization;
using SmartStudy.Models;

namespace SmartStudy.Services;

public sealed class StorageService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string AppDirectory { get; }
    public string DataFilePath { get; }

    public StorageService()
    {
        AppDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SmartStudy");
        DataFilePath = Path.Combine(AppDirectory, "study-data.json");
    }

    public AppState Load()
    {
        Directory.CreateDirectory(AppDirectory);

        if (!File.Exists(DataFilePath))
        {
            return CreateStarterState();
        }

        try
        {
            var json = File.ReadAllText(DataFilePath);
            var state = JsonSerializer.Deserialize<AppState>(json, _jsonOptions) ?? new AppState();
            state.Tasks ??= [];
            state.Sessions ??= [];
            state.Settings ??= new AppSettings();
            return state;
        }
        catch
        {
            PreserveCorruptFile();
            return CreateStarterState();
        }
    }

    public void Save(AppState state)
    {
        Directory.CreateDirectory(AppDirectory);
        state.LastSavedAt = DateTime.Now;

        var json = JsonSerializer.Serialize(state, _jsonOptions);
        var temporaryPath = DataFilePath + ".tmp";
        var backupPath = DataFilePath + ".bak";

        File.WriteAllText(temporaryPath, json);

        if (File.Exists(DataFilePath))
        {
            File.Copy(DataFilePath, backupPath, overwrite: true);
        }

        File.Move(temporaryPath, DataFilePath, overwrite: true);
    }

    public void Export(AppState state, string destinationPath)
    {
        var json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(destinationPath, json);
    }

    public AppState Import(string sourcePath)
    {
        var json = File.ReadAllText(sourcePath);
        var state = JsonSerializer.Deserialize<AppState>(json, _jsonOptions)
                    ?? throw new InvalidDataException("ملف النسخة الاحتياطية غير صالح.");

        state.Tasks ??= [];
        state.Sessions ??= [];
        state.Settings ??= new AppSettings();
        return state;
    }

    private void PreserveCorruptFile()
    {
        try
        {
            if (!File.Exists(DataFilePath))
            {
                return;
            }

            var corruptPath = Path.Combine(
                AppDirectory,
                $"study-data.corrupt.{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(DataFilePath, corruptPath, overwrite: true);
        }
        catch
        {
            // Recovery must never block the application from opening.
        }
    }

    private static AppState CreateStarterState()
    {
        return new AppState
        {
            Tasks =
            [
                new StudyTask
                {
                    Title = "مراجعة المحاضرة وتلخيص أهم الأفكار",
                    Subject = "هندسة البرمجيات",
                    EstimatedMinutes = 45,
                    DueDate = DateTime.Today,
                    Priority = StudyPriority.High
                },
                new StudyTask
                {
                    Title = "حل 3 مسائل تدريبية",
                    Subject = "هياكل البيانات",
                    EstimatedMinutes = 60,
                    DueDate = DateTime.Today.AddDays(1),
                    Priority = StudyPriority.Medium
                }
            ]
        };
    }
}
