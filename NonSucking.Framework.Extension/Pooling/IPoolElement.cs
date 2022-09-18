namespace NonSucking.Framework.Extension.Pooling
{
    public interface IPoolElement
    {
        void Init(IPool pool);
        void Release();
    }
}