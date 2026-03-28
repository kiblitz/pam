using System;
using System.IO;
using System.Text.Json;
using Pam.Models;

namespace Pam.Services;

public static class UsageCache
{
    private static readonly string CachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Pam", "usage-cache.json");

    public static void Save(UsageInfo info, DateTimeOffset fetchedAt)
    {
        var dir = Path.GetDirectoryName(CachePath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var data = new CacheData
        {
            FetchedAt = fetchedAt,
            FiveHourUtilization = info.FiveHour.Utilization,
            FiveHourResetsAt = info.FiveHour.ResetsAt,
            SevenDayUtilization = info.SevenDay.Utilization,
            SevenDayResetsAt = info.SevenDay.ResetsAt,
        };

        File.WriteAllText(CachePath, JsonSerializer.Serialize(data));
    }

    public static (UsageInfo Info, DateTimeOffset FetchedAt)? Load()
    {
        if (!File.Exists(CachePath))
            return null;

        try
        {
            var json = File.ReadAllText(CachePath);
            var data = JsonSerializer.Deserialize<CacheData>(json);
            if (data == null) return null;

            var info = new UsageInfo
            {
                FiveHour = new UsageWindow
                {
                    Utilization = data.FiveHourUtilization,
                    ResetsAt = data.FiveHourResetsAt,
                },
                SevenDay = new UsageWindow
                {
                    Utilization = data.SevenDayUtilization,
                    ResetsAt = data.SevenDayResetsAt,
                },
            };

            return (info, data.FetchedAt);
        }
        catch
        {
            return null;
        }
    }

    private class CacheData
    {
        public DateTimeOffset FetchedAt { get; set; }
        public double FiveHourUtilization { get; set; }
        public DateTimeOffset FiveHourResetsAt { get; set; }
        public double SevenDayUtilization { get; set; }
        public DateTimeOffset SevenDayResetsAt { get; set; }
    }
}
