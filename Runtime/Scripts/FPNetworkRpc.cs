namespace FuzzPhyte.Network
{
    using Unity.Netcode;
    using UnityEngine;

    /// <summary>
    /// Manage Server Rpcs and manage client receiving Rpcs
    /// </summary>
    public class FPNetworkRpc : MonoBehaviour
    {
        public FPNetworkSystem FPNetworkSystem;
        [Tooltip("This is set via the client when they spawn, don't set this in the inspector")]
        public FPNetworkPlayer FPNetworkPlayer;
        #region Server Rpcs
        [ServerRpc(RequireOwnership = false)]
        public virtual void SendColorToClientServerRpc(string colorString, ulong clientId)
        {
            // Send the color string to the specific client
            // Create a ClientRpcParams and set the TargetClientIds to the specific clientId
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            // Send the RPC to the specific client
            ApplyColorToClientRpc(colorString, clientRpcParams);
        }
        
        
        [ServerRpc(RequireOwnership = false)]
        public virtual void SendLoadCommandToClientServerRpc(string sceneName)
        {
            LoadSceneSingleModeClientRpc(sceneName);
        }
        /// <summary>
        /// A Client sends an RPC to the server about some sort of interaction
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="payload"></param>
        [ServerRpc(RequireOwnership = false)]
        public virtual void SendLocalInteractionEvent(ulong clientId, FPNetworkDataStruct payload)
        {
            FPNetworkSystem.ClientEventDataCameIn(clientId, payload);
        }
        [ClientRpc]
        public virtual void LoadSceneSingleModeClientRpc(string sceneName, ClientRpcParams clientRpcParams = default)
        {
            // Load the scene on the client
            Debug.Log($"Loading a single scene {sceneName} on client");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        [ClientRpc]
        public virtual void LoadSceneAdditiveModeClientRpc(string sceneName, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"Loading an additive scene {sceneName} on client");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
        [ClientRpc]
        public virtual void ApplySceneDataToClientRpc(string sceneNameToLoad, ClientRpcParams clientRpcParams = default)
        {
            if (FPNetworkPlayer != null)
            {
                Debug.LogWarning($"ClientRPC: Apply Scene Data to Client {sceneNameToLoad}");
                FPNetworkPlayer.ClientServerSceneSetup(sceneNameToLoad);
            }
        }
        [ClientRpc]
        public virtual void ApplyColorToClientRpc(string colorString, ClientRpcParams clientRpcParams = default)
        {
            // Convert the string to a Unity Color
            //check if # is present in the string
            Debug.LogWarning($"Apply Color(AC): Color coming in:{colorString}");
            if (!colorString.Contains("#"))
            {
                colorString = "#" + colorString;
                Debug.LogWarning($"AC:Changing Color to include hash: {colorString}");
            }
            Color color;
            if (ColorUtility.TryParseHtmlString(colorString, out color))
            {
                // Assuming you have a reference to the material (DebugMat in this case)
                if (FPNetworkPlayer != null)
                {
                    FPNetworkPlayer.ClientDebugSetup(colorString, color);
                }
                else
                {
                    Debug.LogError($"AC:No Client/FPNetworkPlayer Found");
                }
            }
            else
            {
                Debug.LogError($"AC:Invalid color string: {colorString}");
            }
        }
        #endregion
        /*
        [ClientRpc]
        public void RegisterObjectsOnClientRpc(ulong leftHandNetworkObjectId, ulong rightHandNetworkObjectId, ClientRpcParams clientRpcParams = default)
        {
            // This will be called on *all* clients, so we filter
            //Debug.LogWarning($"Register Objects on Client RPC called on client#{clientId}");
            //if (FPNetworkSystem.NetworkManager.LocalClientId != clientId)
            //    return;
            Debug.LogError($"Client Log? Sending information for registering with my client");
            if (FPNetworkPlayer != null)
            {
               
                var leftHand = FPNetworkSystem.NetworkManager.SpawnManager.SpawnedObjects[leftHandNetworkObjectId].GetComponent<NetworkObject>();
                var rightHand = FPNetworkSystem.NetworkManager.SpawnManager.SpawnedObjects[rightHandNetworkObjectId].GetComponent<NetworkObject>();
                FPNetworkPlayer.RegisterOtherObjects(leftHand, rightHand);
            }
            //var player = FPNetworkSystem.NetworkManager.SpawnManager.GetLocalPlayerObject().GetComponent<FPNetworkPlayer>();
            //player.RegisterOtherObjects(leftHand, rightHand);
        }
        */
        [ClientRpc]
        public virtual void BroadcastVisualUpdateClientRpc(string colorString, ulong targetClientId)
        {
            Debug.LogWarning($"Broadcast Visual(BV): Color coming in:{colorString}");
            if (!colorString.Contains("#"))
            {
                colorString = "#" + colorString;
                Debug.LogWarning($"BV: Changing Color to include hash: {colorString}");
            }
            Color color;
            if (ColorUtility.TryParseHtmlString(colorString, out color))
            {
                // Assuming you have a method to handle visual updates for all clients
                UpdateAllClientVisuals(targetClientId, color, colorString);
            }
            else
            { 
                Debug.LogError($"BV:Invalid color string in broadcast: {colorString}");
            }
        }

        // This method would be used to apply the visual update logic on all clients
        protected virtual void UpdateAllClientVisuals(ulong clientId, Color color, string colorString)
        {
            // Update visuals specific to the clientId on all clients
            Debug.Log($"Broadcasting visual update for client {clientId} with color {color}");
            if (FPNetworkPlayer != null)
            {
                FPNetworkPlayer.ClientDebugSetup(colorString, color);
            }
            else
            {
                Debug.LogError($"No Client/FPNetworkPlayer Found");
            }
        }
    }
}
