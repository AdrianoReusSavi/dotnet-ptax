namespace Ptax.Worker;

public class PtaxSyncOptions
{
    public string[] Currencies { get; set; } = [];
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
    public int WindowDays { get; set; } = 7;
}