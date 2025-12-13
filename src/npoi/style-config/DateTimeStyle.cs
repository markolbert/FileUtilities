namespace J4JSoftware.FileUtilities;

public class DateTimeStyle : BaseStyle
{
    public const string DefaultDateSeparator = "/";

    public MonthDayTimeFormat DateFormat { get; set; } = MonthDayTimeFormat.Numbers;
    public DateSequence DateSequence { get; set; } = DateSequence.MonthDayYear;
    public bool DateLeadingZero { get; set; }
    public bool FourDigitYear { get; set; } = true;
    public string DateSeparator { get; set; } = DefaultDateSeparator;

    public bool IncludeTime { get; set; }
    public MonthDayTimeFormat TimeFormat { get; set; } = MonthDayTimeFormat.Numbers;
    public bool TimeLeadingZero { get; set; }
    public bool TwentyFourHourTime { get; set; }
    public bool IncludeSeconds { get; set; }

    public int SecondsDecimalPlaces
    {
        get;
        set => field = value < 0 ? 0 : value;
    }
}
