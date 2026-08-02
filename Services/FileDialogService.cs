using Microsoft.Win32;

namespace SmartStudy.Services;

public sealed class FileDialogService
{
    public string? ChooseExportPath()
    {
        var dialog = new SaveFileDialog
        {
            Title = "تصدير نسخة احتياطية",
            Filter = "Smart Study backup (*.json)|*.json",
            FileName = $"smart-study-backup-{DateTime.Now:yyyy-MM-dd}.json",
            AddExtension = true,
            DefaultExt = ".json"
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? ChooseImportPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "استيراد نسخة احتياطية",
            Filter = "Smart Study backup (*.json)|*.json",
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
