namespace Pkl.PklTypes;

public class Duration
{
    public Duration(DurationUnit unit, double value)
    {
        Unit = unit;
        Value = value;
    }

    public DurationUnit Unit { get; set; }
    
    public double Value { get; set; }

    public TimeSpan ToTimeSpan()
    {
        return Unit switch
        {
            DurationUnit.Nanosecond => TimeSpan.FromMicroseconds(Value / 1000),
            DurationUnit.Microsecond => TimeSpan.FromMicroseconds(Value),
            DurationUnit.Millisecond => TimeSpan.FromMilliseconds(Value),
            DurationUnit.Second => TimeSpan.FromSeconds(Value),
            DurationUnit.Minute => TimeSpan.FromMinutes(Value),
            DurationUnit.Hour => TimeSpan.FromHours(Value),
            DurationUnit.Day => TimeSpan.FromDays(Value),
            
            _ => throw new NotImplementedException("Unit " + Unit + "is out of range"),
        };
    }
}