namespace FuzzPhyte.Network
{

    using UnityEngine;
    using System;
    using Unity.Netcode;
    using System.Collections.Generic;
    #region Network Related Enums
    [Serializable]
    public enum NetworkSequenceStatus
    {
        None = 0,
        Startup = 1,
        WaitingForClients = 2,
        ConfirmScene = 3,
        Active = 4,
        Finishing = 5,
        QA = 10,
        Done = 86,
        DEBUG = 99
    }
    [Serializable]
    public enum ConnectionStatus
    {
        Connected,
        Disconnected,
        Connecting,
        Disconnecting,
    }
    [Serializable]
    public enum DevicePlayerType
    {
        None,
        iPad,
        MetaQuest
    }
    [Serializable]
    public enum NetworkPlayerType
    {
        None,
        Server,
        Client,
        Host
    }

    [Serializable]
    public enum NetworkMessageType
    {
        None,
        ServerConfirmation,
        ClientConfirmed,
        ClientChoice,
        ClientInteraction,
        ClientLocationUpdate,
        ClientImage,
        ClientMessage,
        ClientDisconnectRequest
    }
    #endregion
    /// <summary>
    /// Used to help manage a similar structure between my derived FPEvent classes associated with the network system
    /// </summary>
    public interface IFPNetworkEvent
    {
        void SetupEvent();
        void DebugEvent();
    }
    public interface IFPNetworkPlayerSetup
    {
        void SetupSystem(FPNetworkPlayer player);
        void RegisterOtherObjects(NetworkObject networkObject, FPNetworkPlayer player);
        List<IFPNetworkPlayerSetup> ReturnOtherIFPNetworkObjects();
    }
    public interface IFPNetworkOtherObjectSetup
    {
        void SetupSystem(FPNetworkOtherObject otherObject);
    }
    public interface IFPNetworkProxySetup
    {
        void OnClientSpawned();
        void OnServerSpawned();
        void OnNetworkSpawn();
        /// <summary>
        /// Should result in calling a ServerRPC
        /// This is a wrapper function to take information in from some other script and by using a 
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        void UpdatePositionAndRotation(Vector3 position, Quaternion rotation);
    }
    
    public interface IFPNetworkUISetup
    {
        void OnUISetup(FPNetworkPlayer player);
    }
    public class FPNetworkUtility
    {
    
    }
}
