namespace Pkl.PklTypes;

public struct DataSize
{
    public DataSize(DataSizeUnit unit, double value)
    {
        Unit = unit;
        Value = value;
    }

    public DataSizeUnit Unit { get; set; }
    
    public double Value { get; set; }
}