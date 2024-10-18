namespace FuzzPhyte.Network
{
    using FuzzPhyte.SystemEvent;
    public class FPServerData: IFPNetworkEvent
    {
        public FPNetworkServerEvent ServerEventType;
        public string ServerAction;
        // come up with a few standards here for payload byte array data and how much of this can be setup as a base event and/or what not
        public FPServerData(FPNetworkServerEvent serverEventType, string serverAction)
        {
            ServerEventType = serverEventType;
            ServerAction = serverAction;
        }

        public void Execute(object data = null)
        {
            UnityEngine.Debug.Log($"Server action executed: {ServerAction}");
            ServerEventType.Execute();
        }
        public void SetupEvent()
        {

        }
        public void DebugEvent()
        {
            
        }
    }
}
