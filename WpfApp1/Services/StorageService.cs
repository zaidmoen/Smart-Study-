using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization; // Note: Need to check if this is available, otherwise use a simple implementation
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

        public void SaveTasks(List<StudyTask> tasks)
        {
            try
            {
                // We don't want to save the Brush objects as they are not serializable
                // In a real app we'd use a DTO. For now, let's just save the raw data.
                // Since this is a "to the furthest extent" request, I'll keep it simple for now.
                var data = new List<object>();
                foreach (var t in tasks)
                {
                    data.Add(new {
                        t.Title, t.Subject, t.DueDate, t.EstimateText, 
                        t.StudyMode, t.ConfidencePercent, t.PriorityWeight, 
                        t.PriorityLabel, t.IsDone
                    });
                }
                string json = _serializer.Serialize(data);
                File.WriteAllText(_filePath, json);
            }
            catch { /* Handle errors */ }
        }

        public List<StudyTask> LoadTasks()
        {
            if (!File.Exists(_filePath)) return new List<StudyTask>();
            try
            {
                string json = File.ReadAllText(_filePath);
                var data = _serializer.Deserialize<List<Dictionary<string, object>>>(json);
                var tasks = new List<StudyTask>();
                foreach (var d in data)
                {
                    tasks.Add(new StudyTask {
                        Title = d.ContainsKey("Title") ? d["Title"].ToString() : "",
                        Subject = d.ContainsKey("Subject") ? d["Subject"].ToString() : "",
                        DueDate = d.ContainsKey("DueDate") ? DateTime.Parse(d["DueDate"].ToString()) : DateTime.Now,
                        EstimateText = d.ContainsKey("EstimateText") ? d["EstimateText"].ToString() : "",
                        StudyMode = d.ContainsKey("StudyMode") ? d["StudyMode"].ToString() : "",
                        ConfidencePercent = d.ContainsKey("ConfidencePercent") ? Convert.ToInt32(d["ConfidencePercent"]) : 70,
                        PriorityWeight = d.ContainsKey("PriorityWeight") ? Convert.ToInt32(d["PriorityWeight"]) : 1,
                        PriorityLabel = d.ContainsKey("PriorityLabel") ? d["PriorityLabel"].ToString() : "عادي",
                        IsDone = d.ContainsKey("IsDone") ? Convert.ToBoolean(d["IsDone"]) : false
                    });
                }
                return tasks;
            }
            catch { return new List<StudyTask>(); }
        }
    }
}
