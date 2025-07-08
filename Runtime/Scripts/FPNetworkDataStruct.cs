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
        public string OptionalNetworkMessage;
        public int OptionalSceneIndex; //build scene index?
        public string OptionalDataMessage; //secondary data message
        public DevicePlayerType TheDevicePlayerType;
        public NetworkPlayerType TheNetworkPlayerType;
        public string ClientIPAddress;
        public string ClientColor;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref TheNetworkMessageType);
            serializer.SerializeValue(ref TheNetworkMessage);
            serializer.SerializeValue(ref OptionalNetworkMessage);
            serializer.SerializeValue(ref TheDevicePlayerType);
            serializer.SerializeValue(ref TheNetworkPlayerType); 
            serializer.SerializeValue(ref TheClientID);
            serializer.SerializeValue(ref ClientIPAddress);
            serializer.SerializeValue(ref ClientColor);
            serializer.SerializeValue(ref OptionalSceneIndex);
            serializer.SerializeValue(ref OptionalDataMessage);
        }
    }
    /// <summary>
    /// Struct to hold initial connection data
    /// </summary>
    [Serializable]
    public struct FPInitialConnectionData : INetworkSerializable
    {
        public string PlayerColor;
        public string SceneToLoad;
        public string PlayerName;
        public DevicePlayerType PlayerType;
        public ulong NetworkIDPayloadA;
        public ulong NetworkIDPayloadB;

        // INetworkSerializable requirement
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerColor);
            serializer.SerializeValue(ref SceneToLoad);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref PlayerType);
            serializer.SerializeValue(ref NetworkIDPayloadA);
            serializer.SerializeValue(ref NetworkIDPayloadB);
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