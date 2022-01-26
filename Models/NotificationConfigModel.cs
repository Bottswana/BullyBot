using System.Collections.Generic;

namespace BullyBot.Models;

public class NotificationConfigModel
{
    public string Module { get; set; }
    public string User { get; set; }
    public List<int> TriggerHours { get; set; } = new();
}