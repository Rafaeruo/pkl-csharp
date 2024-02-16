namespace Pkl.PklTypes;

public class DataSize
{
    public DataSize(DataSizeUnit unit, double value)
    {
        Unit = unit;
        Value = value;
    }

    public DataSizeUnit Unit { get; set; }
    
    public double Value { get; set; }
}