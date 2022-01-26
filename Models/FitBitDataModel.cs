#nullable enable
using System;
using Newtonsoft.Json;

namespace BullyBot.Models;
public class FitBitDataModel
{
    /// <summary>
    /// Refresh Token Model
    /// https://dev.fitbit.com/build/reference/web-api/authorization/refresh-token
    /// </summary>
    public class RefreshFlowResult
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string? RefreshToken { get; set; }
    }
    
    /// <summary>
    /// Activity Data
    /// https://dev.fitbit.com/build/reference/web-api/activity/get-daily-activity-summary/
    /// </summary>
    public class ActivityData
    {
        [JsonProperty("activities")]
        public Activity[] Activities { get; set; } = Array.Empty<Activity>();
        
        [JsonProperty("summary")]
        public SummaryData? Summary { get; set; }
    }
    
    /// <summary>
    /// Individual Recorded Activity Data
    /// https://dev.fitbit.com/build/reference/web-api/activity/get-daily-activity-summary/
    /// </summary>
    public class Activity
    {
        [JsonProperty("duration")]
        public long DurationMilliseconds { get; set; }
    }
    
    /// <summary>
    /// Summary Activity Data
    /// https://dev.fitbit.com/build/reference/web-api/activity/get-daily-activity-summary/
    /// </summary>
    public class SummaryData
    {
        [JsonProperty("veryActiveMinutes")]
        public long ActiveMinutes { get; set; }
        
        [JsonProperty("steps")]
        public long Steps { get; set; }
        
        [JsonProperty("restingHeartRate")]
        public long HeartRate { get; set; }
    }
}