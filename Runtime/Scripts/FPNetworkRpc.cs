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
        [ClientRpc]
        public virtual void ApplyColorToClientRpc(string colorString, ClientRpcParams clientRpcParams = default)
        {
            // Convert the string to a Unity Color
            //check if # is present in the string
            if (!colorString.Contains("#"))
            {
                colorString = "#" + colorString;
            }
            if (ColorUtility.TryParseHtmlString(colorString, out Color color))
            {
                // Assuming you have a reference to the material (DebugMat in this case)
                if (FPNetworkPlayer != null)
                {
                    FPNetworkPlayer.ClientDebugSetup(colorString, color);
                }
                else
                {
                    Debug.LogError($"No Client/FPNetworkPlayer Found");
                }
            }
            else
            {
                Debug.LogError($"Invalid color string: {colorString}");
            }
        }
        #endregion
        [ClientRpc]
        public virtual void BroadcastVisualUpdateClientRpc(string colorString, ulong targetClientId)
        {
            if (ColorUtility.TryParseHtmlString(colorString, out Color color))
            {
                // Assuming you have a method to handle visual updates for all clients
                UpdateAllClientVisuals(targetClientId, color, colorString);
            }
            else
            {
                Debug.LogError($"Invalid color string in broadcast: {colorString}");
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
