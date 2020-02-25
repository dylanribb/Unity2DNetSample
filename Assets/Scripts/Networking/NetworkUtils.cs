using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public class NetworkUtils
{

    public static long PingServer(string serverAddr)
    {

        System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping();
        PingOptions options = new PingOptions();
        options.DontFragment = true;

        string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
        int timeout = 120;

        IPAddress serverIP;
        int port;
        EndpointParse(serverAddr, out serverIP, out port, 0);
        PingReply reply = pingSender.Send(serverIP, timeout, buffer, options);

        if (reply.Status == IPStatus.Success)
        {
            return reply.RoundtripTime;
        }
        else
        {
            Debug.LogError($"Ping Server Failed: {reply.Status.ToString()}");
        }

        return -1;
    }

    public static bool EndpointParse(string endpoint, out IPAddress address, out int port, int defaultPort)
    {
        string address_part;
        address = null;
        port = 0;

        if (endpoint.Contains(":"))
        {
            int.TryParse(endpoint.AfterLast(":"), out port);
            address_part = endpoint.BeforeFirst(":");
        }
        else
            address_part = endpoint;

        if (port == 0) { port = defaultPort; }

        // Resolve in case we got a hostname
        var resolvedAddress = System.Net.Dns.GetHostAddresses(address_part);
        foreach (var r in resolvedAddress)
        {
            if (r.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // Pick first ipv4
                address = r;
                return true;
            }
        }
        return false;
    }
}
