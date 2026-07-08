using Microsoft.Extensions.Logging;

namespace J4JSoftware.FileUtilities;

public class CsvTableReader( ILoggerFactory? loggerFactory = null )
    : CsvTableReaderBase<DataRecord>( loggerFactory ), ITableReader
{
    public Type ImportedType => typeof( DataRecord );

    public HashSet<int> GetReplacementIds() => ReplacementAdjuster?.GetReplacementIds() ?? [];

    public IEnumerable<DataRecord> GetData( ImportContext context )
    {
        if( !BeginGetData( context ) )
            yield break;

        var headerRead = false;
        List<string>? headers = null;

        while( CsvReader!.Read() )
        {
            if( !headerRead && context.HasHeaders )
            {
                var headerResult = TryGetHeaders();

                if( !headerResult.succeeded )
                    yield break;

                headers = headerResult.headers;

                headerRead = true;
                continue;
            }

            headers ??= CreateDefaultHeaders();

            switch( ProcessRecord( headers, out var curRecord ) )
            {
                case ProcessRecordResult.Okay:
                    yield return curRecord;

                    break;

                case ProcessRecordResult.Failed:
                    yield break;

                case ProcessRecordResult.FilteredOut:
                    // no op; just continue to next record
                    break;
            }
        }

        OnReadingEnded();
    }

    // The CancellationToken arg is needed to conform to the interface, but CsvReader does not 
    // support using it in ReadAsync()...which is weird
#pragma warning disable CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
    public async IAsyncEnumerable<DataRecord> GetDataAsync( ImportContext context, CancellationToken ctx )
#pragma warning restore CS8425 // Async-iterator member has one or more parameters of type 'CancellationToken' but none of them is decorated with the 'EnumeratorCancellation' attribute, so the cancellation token parameter from the generated 'IAsyncEnumerable<>.GetAsyncEnumerator' will be unconsumed
    {
        if( !BeginGetData( context ) )
            yield break;

        var headerRead = false;
        var headers = new List<string>();

        while( await CsvReader!.ReadAsync() )
        {
            if( !headerRead && context.HasHeaders )
            {
                var headerResult = TryGetHeaders();

                if( !headerResult.succeeded )
                    yield break;

                headers = headerResult.headers;

                headerRead = true;
                continue;
            }

            headers ??= CreateDefaultHeaders();

            switch( ProcessRecord( headers, out var curRecord ) )
            {
                case ProcessRecordResult.Okay:
                    yield return curRecord;

                    break;

                case ProcessRecordResult.Failed:
                    yield break;

                case ProcessRecordResult.FilteredOut:
                    // no op; just continue to next record
                    break;
            }
        }

        OnReadingEnded();
    }

    // only called if CsvReader is defined and ImportContext specifies that the stream has headers
    private (bool succeeded, List<string>? headers) TryGetHeaders()
    {
        if( CsvReader!.ReadHeader() )
            return ( true, CsvReader.HeaderRecord!.ToList() );

        Logger?.StreamHeaderUnreadable();
        return ( false, null );
    }

    // only called if CsvReader is defined
    private List<string> CreateDefaultHeaders()
    {
        var retVal = new List<string>();

        for( var idx = 0; idx < CsvReader!.ColumnCount; idx++ )
        {
            retVal.Add( $"Field{idx + 1}" );
        }

        return retVal;
    }

    private ProcessRecordResult ProcessRecord( List<string> headers, out DataRecord curRecord )
    {
        CurrentRecord++;

        curRecord = CreateDataRecord( headers );

        if( !AlgorithmicAdjuster?.AdjustEntity( curRecord ) ?? false )
            return ProcessRecordResult.Failed;

        if( Filter != null && !Filter.Include( curRecord ) )
            return ProcessRecordResult.FilteredOut;

        return ProcessRecordResult.Okay;
    }

    // CsvReader will always be non-null when this is called
    private DataRecord CreateDataRecord( List<string> headers )
    {
        var retVal = new DataRecord( CurrentRecord, headers );

        for( var colIdx = 0; colIdx < CsvReader!.ColumnCount; colIdx++ )
        {
            if( retVal.AddValue( colIdx, CsvReader[ colIdx ]! ) )
                continue;

            Logger?.DuplicateColumnReadFromStream( colIdx, CurrentRecord );
        }

        return retVal;
    }

    bool ITableReader.TryGetData(
        ImportContext context,
        out IEnumerable<object> data
    )
    {
        data = GetData( context );
        return true;
    }

    IAsyncEnumerable<object> ITableReader.GetObjectDataAsync( ImportContext context, CancellationToken ctx ) =>
        GetDataAsync( context, ctx );

    bool ITableReader.SetAlgorithmicAdjuster( IAlgorithmicAdjuster? adjuster )
    {
        if( adjuster == null )
        {
            AlgorithmicAdjuster = null;
            return true;
        }

        if( adjuster is not IAlgorithmicAdjuster<DataRecord> castAdjuster )
        {
            Logger?.InvalidTypeAssignment( adjuster.GetType(),
                                           typeof( IAlgorithmicAdjuster<DataRecord> ) );
            return false;
        }

        AlgorithmicAdjuster = castAdjuster;
        return true;
    }

    bool ITableReader.SetReplacementAdjuster( IReplacementAdjuster? adjuster )
    {
        if( adjuster == null )
        {
            ReplacementAdjuster = null;
            return true;
        }

        if( adjuster is not IReplacementAdjuster<DataRecord> castAdjuster )
        {
            Logger?.InvalidTypeAssignment( adjuster.GetType(),
                                           typeof( IAlgorithmicAdjuster<DataRecord> ) );
            return false;
        }

        ReplacementAdjuster = castAdjuster;

        return true;
    }

    bool ITableReader.SetFilter( IRecordFilter? filter )
    {
        if( filter == null )
        {
            Filter = null;
            return true;
        }

        if( filter is not IRecordFilter<DataRecord> castFilter )
        {
            Logger?.InvalidTypeAssignment( filter.GetType(), typeof( IRecordFilter<DataRecord> ) );
            return false;
        }

        Filter = castFilter;
        return true;
    }
}
