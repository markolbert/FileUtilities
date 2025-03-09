namespace J4JSoftware.FileUtilities;

// marker interface
public interface IAlgorithmicAdjuster : IAdjuster
{
}

public interface IAlgorithmicAdjuster<in TEntity> : IAlgorithmicAdjuster
{
    bool AdjustEntity( TEntity entity );

    void RecordAdjustment(
        TEntity entity,
        string field,
        ChangeSource source,
        string? priorValue,
        string? adjValue,
        string? reason = null
    );
}
