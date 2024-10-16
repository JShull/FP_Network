namespace FuzzPhyte.Network
{
    using FuzzPhyte.SystemEvent;
    public class FPClientEvent: FPEvent,IFPNetworkEvent
    {
        public ulong ClientId { get; private set; }
        public ConnectionStatus Status { get; private set; }
        //add other needs here for misc data and/or string based header data

        public FPClientEvent(ulong clientId, ConnectionStatus status, int priority = 0)
        {
            ClientId = clientId;
            Status = status;
            Priority = priority;
        }

        public override void Execute(object data = null)
        {
            // Define what happens when this event is executed
            UnityEngine.Debug.Log($"Client {ClientId} is {Status}");
        }
        public void SetupEvent()
        {

        }
        public void DebugEvent()
        {
            
        }
    }
}
