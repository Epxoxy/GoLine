using System.Collections.Generic;

namespace GameServices.GameBasic
{
    public class Player : IPlayer
    {
        public bool IsAI { get; internal set; } = false;
        public string Name { get; set; }
        public int TokenID { get; internal set; }
        public GameScore Score { get; internal set; }
        public IGameService ServicesProvider { get; private set; }
        public Player NextPlayer { get; internal set; }
        public Player FrontPlayer { get; internal set; }
        public bool IsMyTurn => ServicesProvider?.CurrentPlayer == this;
        Stack<Location> MyInput { get; set; } = new Stack<Location>();

        #region Constructor

        public Player() { }
        public Player(string name)
        {
            if(Name != name) Name = name;
        }
        public Player(IGameService gameServices, string name = "") : this(name)
        {
            ServicesProvider = gameServices;
        }

        #endregion

        public void RegisterService(IGameService newServicesProvider)
        {
            OnServicesProviderChanging(ServicesProvider, newServicesProvider);
            ServicesProvider = newServicesProvider;
        }

        internal virtual void OnServicesProviderChanging(IGameService oldService, IGameService newService)
        {

        }

        public bool Input(Location location)
        {
            if (ServicesProvider != null && ServicesProvider.HandInput(this, location, ActionType.New))
            {
                OnInputed(location);
                MyInput.Push(location);
                return true;
            }
            return false;
        }

        public bool Undo()
        {
            if (ServicesProvider != null && MyInput.Count <= 0) return false;
            if (ServicesProvider.HandInput(this, MyInput.Peek(), ActionType.Undo))
            {
                MyInput.Pop();
                return true;
            }
            return false;
        }

        internal virtual void OnInputed(Location location)
        {

        }

        public virtual RequestResult HandRequest(RequestType type)
        {
            return RequestResult.OK;
        }
        
    }
}
