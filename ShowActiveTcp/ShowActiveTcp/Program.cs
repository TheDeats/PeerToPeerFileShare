using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace ShowActiveTcp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ShowActiveTcpConnections();
            ShowActiveTcpListeners();
        }

        public static void ShowActiveTcpConnections()
        {
            Console.WriteLine("Active TCP Connections");
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation c in connections)
            {
                Console.WriteLine("{0} <==> {1}",
                    c.LocalEndPoint.ToString(),
                    c.RemoteEndPoint.ToString());
            }
        }


        public static void ShowActiveTcpListeners()
        {
            Console.WriteLine("Active TCP Listeners");
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            Console.WriteLine("Computer name: {0}", properties.HostName);
            Console.WriteLine("Domain name:   {0}", properties.DomainName);
            Console.WriteLine("Node type:     {0:f}", properties.NodeType);
            Console.WriteLine("DHCP scope:    {0}", properties.DhcpScopeName);
            Console.WriteLine("WINS proxy?    {0}", properties.IsWinsProxy);
            IPEndPoint[] endPoints = properties.GetActiveTcpListeners();
            if(endPoints.Length > 0)
            {
                Console.WriteLine($"Found {endPoints.Length} endpoints");
                foreach (IPEndPoint e in endPoints)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            else
            {
                Console.WriteLine("No listeners found");
            }
        }

    }
}
