namespace J4JSoftware.FileUtilities;

public class DoubleStyle : NumericStyle
{
    public int DecimalPlaces
    {
        get;
        set => field = value < 0 ? 0 : value;
    }
}
