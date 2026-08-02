using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class StorageService
    {
        private readonly string _filePath;
        private readonly JavaScriptSerializer _serializer;

        public StorageService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "SmartStudyPlanner");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            _filePath = Path.Combine(folder, "tasks.json");
            _serializer = new JavaScriptSerializer();
        }

        public string LastError { get; private set; }

        public bool SaveTasks(List<StudyTask> tasks)
        {
            LastError = null;

            try
            {
                var records = new List<StudyTaskRecord>();
                foreach (var task in tasks)
                {
                    records.Add(new StudyTaskRecord
                    {
                        Title = task.Title,
                        Subject = task.Subject,
                        DueDate = task.DueDate.ToString("o", CultureInfo.InvariantCulture),
                        EstimateText = task.EstimateText,
                        StudyMode = task.StudyMode,
                        ConfidencePercent = task.ConfidencePercent,
                        PriorityWeight = task.PriorityWeight,
                        PriorityLabel = task.PriorityLabel,
                        IsDone = task.IsDone
                    });
                }

                string json = _serializer.Serialize(records);
                File.WriteAllText(_filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public List<StudyTask> LoadTasks()
        {
            LastError = null;

            if (!File.Exists(_filePath)) return new List<StudyTask>();

            try
            {
                string json = File.ReadAllText(_filePath, Encoding.UTF8);
                var data = _serializer.Deserialize<List<Dictionary<string, object>>>(json);
                var tasks = new List<StudyTask>();

                foreach (var row in data)
                {
                    tasks.Add(new StudyTask
                    {
                        Title = ReadString(row, "Title", string.Empty),
                        Subject = ReadString(row, "Subject", string.Empty),
                        DueDate = ReadDate(row, "DueDate", DateTime.Today),
                        EstimateText = ReadString(row, "EstimateText", "30 دقيقة"),
                        StudyMode = ReadString(row, "StudyMode", string.Empty),
                        ConfidencePercent = ReadInt(row, "ConfidencePercent", 70),
                        PriorityWeight = ReadInt(row, "PriorityWeight", 1),
                        PriorityLabel = ReadString(row, "PriorityLabel", "عادي"),
                        IsDone = ReadBool(row, "IsDone", false)
                    });
                }

                return tasks;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                BackupCorruptedFile();
                return new List<StudyTask>();
            }
        }

        private void BackupCorruptedFile()
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                string suffix = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                File.Copy(_filePath, _filePath + ".broken-" + suffix, true);
            }
            catch
            {
            }
        }

        private static string ReadString(Dictionary<string, object> row, string key, string fallback)
        {
            if (!row.ContainsKey(key) || row[key] == null) return fallback;
            string value = row[key].ToString();
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static int ReadInt(Dictionary<string, object> row, string key, int fallback)
        {
            if (!row.ContainsKey(key) || row[key] == null) return fallback;

            try
            {
                return Convert.ToInt32(row[key], CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool ReadBool(Dictionary<string, object> row, string key, bool fallback)
        {
            if (!row.ContainsKey(key) || row[key] == null) return fallback;

            try
            {
                return Convert.ToBoolean(row[key], CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static DateTime ReadDate(Dictionary<string, object> row, string key, DateTime fallback)
        {
            if (!row.ContainsKey(key) || row[key] == null) return fallback;

            object rawValue = row[key];
            if (rawValue is DateTime)
            {
                return ((DateTime)rawValue).Date;
            }

            string value = rawValue.ToString();
            DateTime parsed;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                return parsed.Date;
            }

            if (DateTime.TryParse(value, out parsed))
            {
                return parsed.Date;
            }

            Match legacyJsonDate = Regex.Match(value, @"\\?/Date\((-?\d+)");
            long milliseconds;
            if (legacyJsonDate.Success && long.TryParse(legacyJsonDate.Groups[1].Value, out milliseconds))
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return epoch.AddMilliseconds(milliseconds).ToLocalTime().Date;
            }

            return fallback;
        }

        private class StudyTaskRecord
        {
            public string Title { get; set; }
            public string Subject { get; set; }
            public string DueDate { get; set; }
            public string EstimateText { get; set; }
            public string StudyMode { get; set; }
            public int ConfidencePercent { get; set; }
            public int PriorityWeight { get; set; }
            public string PriorityLabel { get; set; }
            public bool IsDone { get; set; }
        }
    }
}
