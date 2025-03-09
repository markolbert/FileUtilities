using Microsoft.Extensions.Logging;

namespace J4JSoftware.FileUtilities;

public abstract class Adjuster<TEntity> : IAdjuster
    where TEntity : class
{
    protected Adjuster(
        ILoggerFactory? loggerFactory
    )
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory?.CreateLogger( GetType() );
    }

    protected ILoggerFactory? LoggerFactory { get; }
    protected ILogger? Logger { get; }

    public Type EntityType => typeof(TEntity);
    public bool IsValid { get; protected set; }
    public IUpdateRecorder? UpdateRecorder { get; set; }

    public abstract bool Initialize( ImportContext context );

    public abstract bool AdjustEntity( TEntity entity );

    public void RecordAdjustment(
        TEntity entity,
        string field,
        ChangeSource source,
        string? originalValue,
        string? adjValue,
        string? reason = null
    )
    {
        UpdateRecorder?.PropertyValueChanged(entity, field, source, originalValue, adjValue, reason);
    }

    public virtual void SaveAdjustmentRecords()
    {
        UpdateRecorder?.SaveChanges();
    }

    bool IAdjuster.AdjustEntity(object entity)
    {
        if (entity is TEntity castEntity)
            return AdjustEntity(castEntity);

        Logger?.InvalidTypeAssignment(entity.GetType(), EntityType);

        return false;
    }

}
