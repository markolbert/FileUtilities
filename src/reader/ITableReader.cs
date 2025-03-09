namespace J4JSoftware.FileUtilities;

public interface ITableReader : IDisposable
{
    Type ImportedType { get; }

    bool SetFilter( IRecordFilter? filter );
    bool SetAlgorithmicAdjuster( IAlgorithmicAdjuster? adjuster );
    bool SetReplacementAdjuster( IReplacementAdjuster? adjuster );

    HashSet<int> GetReplacementIds();

    bool TryGetData( ImportContext context, out IEnumerable<object>? data );
    IAsyncEnumerable<object> GetObjectDataAsync( ImportContext context, CancellationToken ctx );
}

public interface ITableReader<TEntity, in TContext> : ITableReader
    where TEntity : class
    where TContext : ImportContext
{
    IRecordFilter<TEntity>? Filter { get; set; }
    
    IAlgorithmicAdjuster<TEntity>? AlgorithmicAdjuster { get; set; }
    IReplacementAdjuster<TEntity>? ReplacementAdjuster { get; set; }

    IEnumerable<TEntity> GetData( TContext context );
    IAsyncEnumerable<TEntity> GetDataAsync( TContext context, CancellationToken ctx );
}
