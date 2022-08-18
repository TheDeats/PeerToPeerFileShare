//Author: Jared Deaton

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace networkFileShare
{
    class Program
    {
        static readonly CancellationTokenSource s_cts = new CancellationTokenSource();
        public static void Main(string[] args)
        {
            RunNodesAsync();
        }

        public static async Task<bool> RunNodesAsync()
        {
            //Declaring port used and maximum number of nodes supported
            Int32 port = 13000;
            int maxNodes = 10;

            List<string> ipRange = new List<string>();

            TcpNode server = new TcpNode();
            TcpNode client = new TcpNode();

            //grab and printout ip address
            string ipAddress = server.getLocalIpAddress();
            Console.WriteLine($"Container IP: {ipAddress}");

            //set the node ports
            server.Port = port;
            client.Port = port;

            //set the node IP addresses
            server.IpAddress = ipAddress;
            client.IpAddress = ipAddress;

            //get the range of IP addresses to look for
            ipRange = getIPAddressRange(ipAddress, maxNodes);

            //run client and server nodes asynchronously
            //Console.WriteLine("Running task 1");
            var task1 = Task.Run(() => ListenAndShareAsync(server, maxNodes, ipRange));
            //Console.WriteLine("Running task 2");
            var task2 = Task.Run(() => ConnectAndShareAsync(client, maxNodes, ipRange));

            //wait for all threads to finish
            Task.WaitAll(task1, task2);

            return true;
        }

        public static async Task<bool> ListenAndShareAsync(TcpNode node, int maxNodes, List<string> ipRange)
        {
            while (true)
            {
                //blocking call, open the port and start listening
                Console.WriteLine($"Server container IP {node.IpAddress}:{node.Port}");
                node.OpenPortAndListen(ipRange);

                //will make it here once a client has connected
                Console.WriteLine("Server Connected!");

                //syncs files with the client and closes the connection
                node.ConnectToClientAndSyncFiles(ipRange);
            }
            
            return true;
        }

        public static async Task<bool> ConnectAndShareAsync(TcpNode node, int maxNodes, List<string> ipRange)
        {
            int connectionAttempts = maxNodes * 2;
            Console.WriteLine($"Client container IP {node.IpAddress}:{node.Port}");

            //node.FindTcpListeners(connectionAttempts, ipRange);
            //node.DisplayListenersFound();

            node.ConnectToServerAndSyncFiles(connectionAttempts, ipRange);

            return true;
        }

        /// <summary>
        /// Gets the range of ip addresses to test for
        /// </summary>
        /// <param name="ip"> the ip address </param>
        /// <param name="range"> the total number of ip addresses to test for </param>
        public static List<string> getIPAddressRange(string ip, int range)
        {
            List<string> ipRange = new List<string>();
            //split ip into array
            string[] splitIP = ip.Split('.');
            string builtIP;
            int position = 0;
            for (int i = 0; i < range; i++)
            {
                builtIP = ($"{splitIP[0]}.{splitIP[1]}.{splitIP[2]}.{i + 2}");
                //Console.WriteLine($"Built IP: {builtIP}");
                //Console.WriteLine($"ip: {ip}");
                if (!builtIP.Equals(ip))
                {
                    ipRange.Add(builtIP);
                    Console.WriteLine($"IP Range: {ipRange[position]}");
                }
                else
                {
                    position--;
                }
                position++;

            }
            Random rng = new Random();
            List<string> shuffleIPs = ipRange.OrderBy(x => rng.Next()).ToList();
            return shuffleIPs;
        }




    }
}
