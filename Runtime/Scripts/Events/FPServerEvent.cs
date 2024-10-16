namespace FuzzPhyte.Network
{
    using FuzzPhyte.SystemEvent;
    public class FPServerEvent:FPEvent,IFPNetworkEvent
    {
        public string ServerAction { get; private set; }
        // come up with a few standards here for payload byte array data and how much of this can be setup as a base event and/or what not

        public FPServerEvent(string serverAction, int priority = 0)
        {
            ServerAction = serverAction;
            Priority = priority;
        }

        public override void Execute(object data = null)
        {
            UnityEngine.Debug.Log($"Server action executed: {ServerAction}");
        }
        public void SetupEvent()
        {

        }
        public void DebugEvent()
        {
            
        }
    }
}
