namespace NonSucking.Framework.Serialization;

internal interface INoosonConverter<TFrom, TTo>
{
    bool TryConvert(TFrom val, out TTo res);

    bool TryConvert(TTo val, out TFrom res);
}