namespace PklCSharp.Generator.Parser;

public class ExactTokenAttribute: Attribute
{
    public readonly string TokenValue;
    
    public ExactTokenAttribute(string tokenValue)
    {
        TokenValue = tokenValue;
    }
}
