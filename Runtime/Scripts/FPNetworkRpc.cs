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
        [ServerRpc(RequireOwnership = false)]
        public void SendColorToClientServerRpc(string colorString, ulong clientId)
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
        public void ApplyColorToClientRpc(string colorString, ClientRpcParams clientRpcParams = default)
        {
            // Convert the string to a Unity Color
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
    }
}