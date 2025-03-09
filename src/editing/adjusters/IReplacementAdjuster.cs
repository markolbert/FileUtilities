namespace J4JSoftware.FileUtilities;

public interface IReplacementAdjuster : IAdjuster
{
    HashSet<int> GetReplacementIds();
}

public interface IReplacementAdjuster<in TEntity> : IReplacementAdjuster
{
    bool AdjustEntity(TEntity entity);

    void RecordAdjustment(
        TEntity entity,
        string field,
        ChangeSource source,
        string? priorValue,
        string? adjValue,
        string? reason = null
    );
}
