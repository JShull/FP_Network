namespace FuzzPhyte.Network{

    using System;
    using Unity.Netcode;

    [Serializable]
    public struct FPNetworkDataStruct:INetworkSerializable
    {
        public NetworkMessageType TheNetworkMessageType;
        public string TheNetworkMessage;
        public ulong TheClientID;
        public DevicePlayerType TheDevicePlayerType;
        public NetworkPlayerType TheNetworkPlayerType;
        public string ClientIPAddress;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TheNetworkMessageType);
            serializer.SerializeValue(ref TheNetworkMessage);
            serializer.SerializeValue(ref TheDevicePlayerType);
            serializer.SerializeValue(ref TheNetworkPlayerType);
            serializer.SerializeValue(ref TheClientID);
            serializer.SerializeValue(ref ClientIPAddress);
        }
    }
}