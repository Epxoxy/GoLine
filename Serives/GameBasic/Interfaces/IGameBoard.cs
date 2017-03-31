namespace GameServices.GameBasic
{
    public interface IGameBoard<T1,T2>
    {
        bool Hand(T1 t1, T2 t2);
        bool UndoHand(T1 t1, T2 t2);
        void Clear();
    }
}
