using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace J4JSoftware.FileUtilities;

public class CsvTableReaderBase<TEntity>
    where TEntity : class
{
    protected enum ProcessRecordResult
    {
        Okay,
        Failed,
        FilteredOut
    }

    private StreamReader? _textReader;

    protected CsvTableReaderBase(
        ILoggerFactory? loggerFactory
    )
    {
        LoggerFactory = loggerFactory;
        Logger = loggerFactory?.CreateLogger( GetType() );
    }

    protected ILoggerFactory? LoggerFactory { get; }
    protected ILogger? Logger { get; }

    protected CsvReader? CsvReader { get; private set; }
    protected int CurrentRecord { get; set; }

    public IRecordFilter<TEntity>? Filter { get; set; }
    
    public IAlgorithmicAdjuster<TEntity>? AlgorithmicAdjuster { get; set; }
    public IReplacementAdjuster<TEntity>? ReplacementAdjuster { get; set; }

    protected virtual bool Initialize() => true;

    protected virtual bool BeginGetData( ImportContext context )
    {
        if( context.ImportStream == null )
        {
            Logger?.UndefinedStream();
            return false;
        }

        // initialize the filter, if one exists
        if( !Filter?.Initialize() ?? false )
            return false;

        if( !AlgorithmicAdjuster?.Initialize( context ) ?? false )
            return false;

        // finally, complete whatever custom reader initialization
        // may be defined
        if( !Initialize() )
            return false;

        CurrentRecord = 0;

        try
        {
            _textReader = new StreamReader( context.ImportStream );
            CsvReader = new CsvReader( _textReader, CultureInfo.InvariantCulture );
        }
        catch( Exception ex )
        {
            Logger?.StreamParsingError( ex.Message );
            Dispose();

            return false;
        }

        return true;
    }

    protected virtual void OnReadingEnded()
    {
        // save whatever changes/updates were recorded
        AlgorithmicAdjuster?.SaveAdjustmentRecords();
    }

    public void Dispose()
    {
        _textReader?.Dispose();
        CsvReader?.Dispose();
    }
}
