namespace FuzzPhyte.Network
{
    using System;

    [Serializable]
    public class FPClientData: IFPNetworkEvent
    {
        public ulong ClientId { get; private set; }
        public string ClientPlayerName;
        public ConnectionStatus Status { get; private set; }
        public FPNetworkClientEvent ClientEventType;
        public string ClientAction;

        public FPClientData(ulong clientId, ConnectionStatus status,FPNetworkClientEvent clientEventType,string clientAction,string playerName="Player")
        {
            ClientId = clientId;
            Status = status;
            ClientEventType = clientEventType;
            ClientAction =clientAction;
            ClientPlayerName = playerName;
        }
        public virtual void Execute(object data = null)
        {
            // Define what happens when this event is executed
            UnityEngine.Debug.Log($"Client {ClientId} is {Status}");
            //add in FPevent object execute here if needed
        }
        public virtual void SetupEvent()
        {

        }
        public virtual void DebugEvent()
        {
            
        }
    }
}
