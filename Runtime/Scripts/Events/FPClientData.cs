namespace FuzzPhyte.Network
{
    using FuzzPhyte.SystemEvent;
    using UnityEditor.PackageManager;

    public class FPClientData: IFPNetworkEvent
    {
        public ulong ClientId { get; private set; }
        public ConnectionStatus Status { get; private set; }
        public FPNetworkClientEvent ClientEventType;
        public string ClientAction;
        //public int Priority { get; private set; }
        //add other needs here for misc data and/or string based header data

        public FPClientData(ulong clientId, ConnectionStatus status,FPNetworkClientEvent clientEventType,string clientAction)
        {
            ClientId = clientId;
            Status = status;
            ClientEventType = clientEventType;
            ClientAction =clientAction;
        }
        public void Execute(object data = null)
        {
            // Define what happens when this event is executed
            UnityEngine.Debug.Log($"Client {ClientId} is {Status}");
            //add in FPevent object execute here if needed
        }
        public void SetupEvent()
        {

        }
        public void DebugEvent()
        {
            
        }
    }
}