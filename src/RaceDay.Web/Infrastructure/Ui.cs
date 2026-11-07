using System.Globalization;

namespace RaceDay.Web.Infrastructure;

/// <summary>Display helpers shared by all views, so badges, dates and times look the same on every page.</summary>
public static class Ui
{
    private static readonly CultureInfo SouthAfrica = CultureInfo.GetCultureInfo("en-ZA");

    /// <summary>CSS class for an enrolment status badge: green Confirmed, amber Pending, red Cancelled.</summary>
    public static string StatusBadgeClass(string status) => status switch
    {
        "Confirmed" => "rd-status rd-status-confirmed",
        "Pending" => "rd-status rd-status-pending",
        "Cancelled" => "rd-status rd-status-cancelled",
        _ => "rd-status",
    };

    /// <summary>Bootstrap Icons name for each status, so colour is never the only signal.</summary>
    public static string StatusIcon(string status) => status switch
    {
        "Confirmed" => "bi-check-circle-fill",
        "Pending" => "bi-hourglass-split",
        "Cancelled" => "bi-x-circle-fill",
        _ => "bi-circle",
    };

    public static string EventTypeClass(string eventType) => $"rd-type rd-type-{eventType.ToLowerInvariant()}";

    public static string EventTypeIcon(string eventType) => eventType switch
    {
        "Run" => "bi-lightning-charge-fill",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag-fill",
    };

    public static string Date(DateTime value) => value.ToString("ddd d MMM yyyy", SouthAfrica);

    public static string DateTime(DateTime value) => value.ToString("ddd d MMM yyyy, HH:mm", SouthAfrica);

    public static string Distance(decimal km) => $"{km.ToString("0.##", CultureInfo.InvariantCulture)} km";

    /// <summary>Race time as h:mm:ss, for example 3:42:07.</summary>
    public static string FinishTime(TimeSpan time) => $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";

    /// <summary>1st, 2nd, 3rd, 4th ... 11th, 12th, 13th ... 21st.</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        var suffix = lastTwo is >= 11 and <= 13
            ? "th"
            : (number % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
        return $"{number}{suffix}";
    }

    /// <summary>"Today", "Tomorrow", "In 5 days" or "Finished" relative to now.</summary>
    public static string Countdown(DateTime eventDate)
    {
        var days = (eventDate.Date - System.DateTime.Today).Days;
        return days switch
        {
            < 0 => "Finished",
            0 => "Today",
            1 => "Tomorrow",
            _ => $"In {days} days",
        };
    }

    public static bool IsUpcoming(DateTime eventDate) => eventDate >= System.DateTime.Today;
}
