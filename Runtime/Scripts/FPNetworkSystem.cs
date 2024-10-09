namespace FuzzPhyte.Network
{
    using FuzzPhyte.Utility.FPSystem;
    using UnityEngine;
    using System.Net;
    using System.Net.Sockets;

    public class FPNetworkSystem : FPSystemBase<FPNetworkData>
    {
        public FPNetworkData TestData;
        
        public override void Initialize(bool runAfterLateUpdateLoop, FPNetworkData data = null)
        {
            if(TestData!=null)
            {
                data = TestData;
            }
            //base.Initialize(RunAfterLateUpdateLoop, data);
            Debug.LogWarning($"Local Ip Address: {GetLocalIPAddress()}");
        }
        public static string GetLocalIPAddress()
        {
            string localIP = string.Empty;
            try
            {
                // Get the host's name
                string hostName = Dns.GetHostName();
                
                // Get the list of IPs associated with the host
                IPAddress[] hostIPs = Dns.GetHostAddresses(hostName);

                foreach (IPAddress ip in hostIPs)
                {
                    // Check if the address is IPv4 and not a loopback address
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        localIP = ip.ToString();
                        break;
                    }
                }
            }
            catch (SocketException e)
            {
                Debug.LogError($"Socket exception: {e.Message}");
            }

            if (string.IsNullOrEmpty(localIP))
            {
                Debug.LogError("Local IP address not found.");
            }

            return localIP;
        }
    }
}
