namespace FuzzPhyte.Network
{
    using System;
    using FuzzPhyte.Utility;
    
    [Serializable]
    public class FPNetworkData:FP_Data
    {
        public DevicePlayerType TheDevicePlayerType;
        public NetworkPlayerType TheNetworkPlayerType;
    }
}
