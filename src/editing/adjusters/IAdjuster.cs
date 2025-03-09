namespace J4JSoftware.FileUtilities;

public interface IAdjuster
{
    Type EntityType { get; }
    bool IsValid { get; }
    IUpdateRecorder? UpdateRecorder { get; set; }

    bool Initialize( ImportContext context );
    bool AdjustEntity( object entity );

    void SaveAdjustmentRecords();
}
