namespace FuzzPhyte.Network
{
    using FuzzPhyte.Utility;
    using UnityEngine;

    public enum DevicePlayerType
    {
        None,
        iPad,
        MetaQuest
    }
    public enum NetworkPlayerType
    {
        None,
        Server,
        Client,
        Host
    }
    public class FPNetworkData:FP_Data
    {
        public DevicePlayerType TheDevicePlayerType;
        public NetworkPlayerType TheNetworkPlayerType;
    }
}
