namespace Pkl.PklTypes;

public struct Duration
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
            # if NET7_0_OR_GREATER
            DurationUnit.Nanosecond => TimeSpan.FromMicroseconds(Value / 1_000),
            DurationUnit.Microsecond => TimeSpan.FromMicroseconds(Value),
            # else
            DurationUnit.Nanosecond => TimeSpan.FromMilliseconds(Value / 1_000_000),
            DurationUnit.Microsecond => TimeSpan.FromMilliseconds(Value / 1_000),
            # endif
            DurationUnit.Millisecond => TimeSpan.FromMilliseconds(Value),
            DurationUnit.Second => TimeSpan.FromSeconds(Value),
            DurationUnit.Minute => TimeSpan.FromMinutes(Value),
            DurationUnit.Hour => TimeSpan.FromHours(Value),
            DurationUnit.Day => TimeSpan.FromDays(Value),
            
            _ => throw new NotImplementedException("Unit " + Unit + "is out of range"),
        };
    }
}