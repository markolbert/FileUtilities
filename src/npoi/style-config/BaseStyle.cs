using System.Text.Json.Serialization;

namespace J4JSoftware.FileUtilities;

[ JsonPolymorphic( IgnoreUnrecognizedTypeDiscriminators = true,
                   UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor ) ]
[ JsonDerivedType( typeof( SimpleNumberStyle ), "number" ) ]
[ JsonDerivedType( typeof( IntegerStyle ), "integer" ) ]
[ JsonDerivedType( typeof( FormatCodeStyle ), "format-code" ) ]
[ JsonDerivedType( typeof( DateTimeStyle ), "datetime" ) ]
[ JsonDerivedType( typeof( DoubleStyle ), "double" ) ]
public class BaseStyle
{
    public const string DefaultFontName = "Segoe UI";
    public const int DefaultFontHeightInPoints = 12;

    protected BaseStyle()
    {
    }

    public string StyleName { get; set; } = string.Empty;

    public string FontName
    {
        get;

        set
        {
            value = value.Trim();

            field = string.IsNullOrEmpty( value ) ? DefaultFontName : value;
        }
    } = DefaultFontName;

    public int FontHeightInPoints
    {
        get;
        set => field = value <= 0 ? DefaultFontHeightInPoints : value;
    } = DefaultFontHeightInPoints;
}
