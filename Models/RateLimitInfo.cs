using System;

namespace Pam.Models;

public class UsageInfo
{
    public UsageWindow FiveHour { get; init; } = new();
    public UsageWindow SevenDay { get; init; } = new();
}

public class UsageWindow
{
    public double Utilization { get; init; }
    public DateTimeOffset ResetsAt { get; init; }
}
