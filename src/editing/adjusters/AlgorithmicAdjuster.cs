using System.Linq.Expressions;
using J4JSoftware.Utilities;
using Microsoft.Extensions.Logging;

namespace J4JSoftware.FileUtilities;

public abstract class AlgorithmicAdjuster<TEntity>( ILoggerFactory? loggerFactory )
    : Adjuster<TEntity>( loggerFactory ), IAlgorithmicAdjuster<TEntity>
    where TEntity : class
{
    private readonly Correctors<TEntity> _correctors = [];

    public override bool Initialize( ImportContext context )
    {
        IsValid = true;

        return IsValid;
    }

    public override bool AdjustEntity( TEntity entity )
    {
        if( !IsValid )
            return false;

        CorrectProperties( entity );

        return true;
    }

    protected void AddSinglePropertyCorrector<TProp>(
        Expression<Func<TEntity, TProp?>> propExpr,
        params IPropertyAdjuster<TProp?>[] adjusters
    )
    {
        if( adjusters.Length == 0 )
            return;

        var propInfo = propExpr.GetPropertyInfo();

        if( !_correctors.TryGetValue( propInfo.Name, out var corrector ) )
        {
            corrector = new Corrector<TEntity, TProp>( propExpr, UpdateRecorder );
            _correctors.Add( corrector );
        }

        ( (Corrector<TEntity, TProp>) corrector ).Adjusters.AddRange( adjusters );
    }

    protected virtual void CorrectProperties( TEntity entity )
    {
        foreach( var corrector in _correctors )
        {
            corrector.CorrectEntity( entity );
        }
    }
}
