namespace FuzzPhyte.Network{

    using System;
    using Unity.Netcode;
    using FuzzPhyte.Utility;
    [Serializable]
    public struct FPNetworkDataStruct:INetworkSerializable
    {
        public NetworkMessageType TheNetworkMessageType;
        public string TheNetworkMessage;
        public ulong TheClientID;

        public DevicePlayerType TheDevicePlayerType;
        public NetworkPlayerType TheNetworkPlayerType;
        public string ClientIPAddress;
        public string ClientColor;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TheNetworkMessageType);
            serializer.SerializeValue(ref TheNetworkMessage);
            serializer.SerializeValue(ref TheDevicePlayerType);
            serializer.SerializeValue(ref TheNetworkPlayerType);
            
            serializer.SerializeValue(ref TheClientID);
            serializer.SerializeValue(ref ClientIPAddress);
            serializer.SerializeValue(ref ClientColor);
        }
    }
    [Serializable]
    public class FPSerializedNetworkData<FPNetworkDataStruct> : FPSerializableList<FPNetworkDataStruct>
    {
        public ulong ClientID;
        public string IPAddress;
        public FPSerializedNetworkData(System.Collections.Generic.List<FPNetworkDataStruct> list, ulong clientID, string iPAddress) : base(list)
        {
            ClientID = clientID;
            IPAddress = iPAddress;
        }
    }
}