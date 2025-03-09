using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using J4JSoftware.Utilities;
using Microsoft.Extensions.Logging;

namespace J4JSoftware.FileUtilities;

public abstract class ReplacementAdjuster<TEntity> : Adjuster<TEntity>, IReplacementAdjuster<TEntity>
    where TEntity : class
{
    private readonly Dictionary<int, HashSet<string>> _propsChanged = [];
    private readonly Func<TEntity, int> _keyGetter;
    private readonly string _keyName;
    private readonly Dictionary<string, Func<TEntity, object?>> _getters = [];
    private readonly Dictionary<string, Action<TEntity, object?>> _setters = [];
    private readonly List<PropertyInfo> _entityProps;
    private readonly HashSet<JsonConverter> _jsonConverters = [];

    private Dictionary<int, TEntity>? _replEntities;

    protected ReplacementAdjuster(
        Expression<Func<TEntity, int>> keyExpr,
        ILoggerFactory? loggerFactory
    )
    :base(loggerFactory)
    {
        _keyGetter = keyExpr.Compile();
        _keyName = keyExpr.GetPropertyInfo().Name;
        _entityProps = EntityType.GetProperties().ToList();
    }

    public override bool Initialize( ImportContext context )
    {
        IsValid = false;

        if( string.IsNullOrEmpty( context.ReplacementsPath ) )
            return false;

        if (!File.Exists(context.ReplacementsPath))
        {
            Logger?.FileNotFound(context.ReplacementsPath);
            return false;
        }

        try
        {
            if (!LoadPropsChanged(context.ReplacementsPath))
                return false;

            if (!TryLoadEntities(context.ReplacementsPath))
                return false;

            var masterPropsChanged = new HashSet<string>();
            masterPropsChanged.UnionWith(_propsChanged.SelectMany(kvp => kvp.Value));

            // we never want to change the key field
            masterPropsChanged.Remove(_keyName);

            foreach (var propInfo in _entityProps.Where(x => masterPropsChanged.Contains(x.Name)))
            {
                _getters.Add(propInfo.Name, e => propInfo.GetValue(e));
                _setters.Add(propInfo.Name, (e, value) => propInfo.SetValue(e, value));
            }
        }
        catch (Exception ex)
        {
            Logger?.Error($"Could not parse '{context.ReplacementsPath}', message was '{ex.Message}'");

            return false;
        }

        IsValid = true;

        return IsValid;
    }

    public HashSet<int> GetReplacementIds() => _replEntities?.Select( re => re.Key ).ToHashSet() ?? [];

    public override bool AdjustEntity( TEntity entity ) => !IsValid || ApplyReplacements( entity );

    protected void AddJsonConverter( params JsonConverter[] converters )
    {
        foreach( var converter in converters )
        {
            _jsonConverters.Add( converter );
        }
    }

    private bool LoadPropsChanged( string filePath )
    {
        var expandoReader = new MultiRecordJsonFileReader<ExpandoObject>( LoggerFactory );
        expandoReader.LoadFile( filePath );

        var rawExpando = expandoReader.Contents!.ToList();

        foreach( dynamic expando in rawExpando )
        {
            var dict = (IDictionary<string, object?>) expando;

            if( !dict.ContainsKey( _keyName ) )
            {
                Logger?.ExpandoKeyFieldNotFound( EntityType, _keyName );
                return false;
            }

            var key = GetExpandoKey( expando );

            if( _propsChanged.ContainsKey( key ) )
                continue;

            // don't add the key field to the props changed set, because
            // we never want to change it
            _propsChanged.Add( key,
                               ( (IDictionary<string, object?>) expando ).Keys
                                                                         .Where( k => k != _keyName )
                                                                         .ToHashSet() );
        }

        return true;
    }

    private bool TryLoadEntities( string filePath )
    {
        var reader = new MultiRecordJsonFileReader<TEntity>( LoggerFactory );

        foreach( var converter in _jsonConverters )
        {
            reader.SerializerOptions.Converters.Add( converter );
        }

        if( !reader.LoadFile( filePath ) )
            return false;

        _replEntities = reader.Contents!.ToDictionary( _keyGetter, x => x );

        return true;
    }

    protected abstract int GetExpandoKey( dynamic expando );

    protected bool ApplyReplacements( TEntity dbEntity )
    {
        var id = _keyGetter( dbEntity );

        if( !_replEntities!.TryGetValue( id, out var replEntity ) )
            return true;

        if( !_propsChanged.TryGetValue( id, out var propsChanged ) )
            return true;

        var allOkay = true;

        foreach( var propName in propsChanged )
        {
            if( !_getters.TryGetValue( propName, out var getter ) )
            {
                Logger?.UndefinedGetter( EntityType, propName );
                allOkay = false;

                continue;
            }

            var existingValue = getter( dbEntity );
            var replValue = getter( replEntity );

            if( ( existingValue == null && replValue == null ) || ( existingValue?.Equals( replValue ) ?? false ) )
                continue;

            if( !_setters.TryGetValue( propName, out var setter ) )
            {
                Logger?.UndefinedSetter( EntityType, propName );
                allOkay = false;

                continue;
            }

            RecordAdjustment( dbEntity,
                              propName,
                              ChangeSource.Replacement,
                              existingValue?.ToString(),
                              replValue?.ToString() );

            setter( dbEntity, replValue );
        }

        return allOkay;
    }
}
