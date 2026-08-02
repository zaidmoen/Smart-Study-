using SmartStudy.Models;

namespace SmartStudy.Services;

public sealed class RecommendationService
{
    public StudyTask? GetRecommendedTask(IEnumerable<StudyTask> tasks)
    {
        return tasks
            .Where(task => !task.IsCompleted)
            .OrderByDescending(CalculateScore)
            .ThenBy(task => task.EstimatedMinutes)
            .FirstOrDefault();
    }

    public string Explain(StudyTask? task)
    {
        if (task is null)
        {
            return "أضف مهمة جديدة وسيختار Smart Study أفضل نقطة بداية لك.";
        }

        if (task.IsOverdue)
        {
            return "هذه المهمة متأخرة، وإنهاؤها الآن سيخفف ضغط الخطة فورًا.";
        }

        if (task.IsDueToday)
        {
            return task.Priority is StudyPriority.High or StudyPriority.Critical
                ? "موعدها اليوم وأولويتها مرتفعة، لذلك هي أفضل استثمار لجلستك القادمة."
                : "موعدها اليوم ويمكن إغلاقها قبل أن تتحول إلى مهمة متأخرة.";
        }

        if (task.Priority == StudyPriority.Critical)
        {
            return "أولوية حرجة؛ البدء بها الآن يمنع تراكم المخاطر في خطتك.";
        }

        return task.EstimatedMinutes <= 30
            ? "مهمة قصيرة نسبيًا؛ إنجازها سيعطيك زخمًا سريعًا لبقية اليوم."
            : "توازن جيد بين الأهمية والموعد والوقت المطلوب.";
    }

    private static int CalculateScore(StudyTask task)
    {
        var score = task.Priority switch
        {
            StudyPriority.Low => 10,
            StudyPriority.Medium => 25,
            StudyPriority.High => 45,
            StudyPriority.Critical => 70,
            _ => 20
        };

        var days = (task.DueDate.Date - DateTime.Today).Days;
        score += days switch
        {
            < 0 => 100 + Math.Min(Math.Abs(days) * 5, 30),
            0 => 80,
            1 => 55,
            <= 3 => 30,
            <= 7 => 15,
            _ => 0
        };

        if (task.EstimatedMinutes <= 30)
        {
            score += 8;
        }

        return score;
    }
}
