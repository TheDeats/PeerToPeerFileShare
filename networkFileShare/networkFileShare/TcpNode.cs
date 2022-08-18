using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace networkFileShare
{
    class TcpNode
    {
        #region Fields
        /// <summary>
        /// local tcp listener used by server thread
        /// </summary>
        private TcpListener server = null;
        /// <summary>
        /// local tcp client used by client thread
        /// </summary>
        private TcpClient serverClient = null;
        /// <summary>
        /// Array of file paths found within a directory
        /// </summary>
        private string[][] AllFilesPaths;
        /// <summary>
        /// Array of folder and file names. Array layout shown below
        /// Array 1: [0]Folder name [1+N]File name
        /// </summary>
        private string[][] FileDirNames;
        /// <summary>
        /// local ipAddress
        /// </summary>
        private string ipAddress;
        /// <summary>
        /// local port
        /// </summary>
        private int port;
        /// <summary>
        /// for testing only, list to hold listeners found
        /// </summary>
        private List<string> tcpListeners;
        /// <summary>
        /// The number of attempts a client has made to connect to any server
        /// </summary>
        private int connectionNumber;
        #endregion

        #region Properties
        public string IpAddress 
        {
            get { return this.ipAddress; } 
            set { this.ipAddress = value; } 
        }
        public int Port 
        { 
            get { return this.port; } 
            set { this.port = value; } 
        }
        
        #endregion

        #region Constructors
        public TcpNode()
        {
        }
        public TcpNode(string _ip, int _port)
        {
            this.ipAddress = _ip;
            this.port = _port;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Get the ip address of the node
        /// </summary>
        /// <returns></returns>
        public string getLocalIpAddress()
        {
            string ipDNS = "Error: Could not get IP Address";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipDNS = ip.ToString();
                }
            }
            return ipDNS;
        }

        /// <summary>
        /// Gets all the directories and files within the directory. Sets both AllFilesPaths and FileDirNames
        /// </summary>
        public void GetLatestFilesInDirectory()
        {
            try
            {
                string dir = @"./";
                string[] dirName;
                string[] fileName;
                if (Directory.Exists(dir))
                {
                    string[] subDirectories = Directory.GetDirectories(dir);

                    AllFilesPaths = new string[subDirectories.Length][];
                    FileDirNames = new string[subDirectories.Length][];
                    for (int i = 0; i < subDirectories.Length; i++)
                    {
                        Console.WriteLine($"Looking in directory {subDirectories[i]}");

                        //get all file paths in a directory
                        AllFilesPaths[i] = Directory.GetFiles(subDirectories[i]);
                        FileDirNames[i] = new string[AllFilesPaths[i].Length + 1];

                        //split directory path to single out the directory name, first position of FileDirNames is the directory name
                        dirName = subDirectories[i].Split('/');
                        FileDirNames[i][0] = dirName[1];

                        //all following positions are the file names
                        for (int j = 0; j < AllFilesPaths[i].Length; j++)
                        {
                            //split the file path and grab the file name
                            fileName = AllFilesPaths[i][j].Split('/');
                            FileDirNames[i][j + 1] = fileName[2];
                        }
                    }
                }

                else
                {
                    Console.WriteLine("Directory not found");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            
        }

        /// <summary>
        /// Displays all the file paths and the count of files and directories
        /// </summary>
        public void showFileAndDirCount()
        {
            //Console.WriteLine($"AllFiles length {allFiles.Length} AllFiles[0] length {allFiles[0].Length}");
            if(AllFilesPaths[0] is null)
            {
                Console.WriteLine("Could not find any directories or files");
            }
            else
            {
                int dirCount = 0;
                int fileCount = 0;
                for (int i = 0; i < AllFilesPaths.Length; i++)
                {
                    dirCount++;
                    for (int j = 0; j < AllFilesPaths[i].Length; j++)
                    {
                        fileCount++;
                        Console.WriteLine(AllFilesPaths[i][j]);
                    }
                }
                Console.WriteLine($"Found {dirCount} directories and {fileCount} files");
            }
            
        }

        /// <summary>
        /// Displays all the folder names and files found within
        /// </summary>
        public void showDirAndFileNames()
        {
            //Console.WriteLine($"AllFiles length {allFiles.Length} AllFiles[0] length {allFiles[0].Length}");
            if (FileDirNames[0] is null)
            {
                Console.WriteLine("Could not find any directory or file names");
            }
            else
            {
                int dirCount = 0;
                int fileCount = 0;
                for (int i = 0; i < FileDirNames.Length; i++)
                {
                    dirCount++;
                    for (int j = 1; j < FileDirNames[i].Length; j++)
                    {
                        fileCount++;
                        Console.WriteLine($"{FileDirNames[i][0]}: {FileDirNames[i][j]}");
                    }
                }
                Console.WriteLine($"Found {dirCount} directories and {fileCount} files");
            }

        }

        /// <summary>
        /// Opens a port and starts listening
        /// </summary>
        /// <param name="ipRange"></param>
        public void OpenPortAndListen(List<string> ipRange)
        {
            try
            {
                IPAddress localAddr = IPAddress.Parse(this.ipAddress);
                //Console.WriteLine("IP Address: " + localAddr);

                server = new TcpListener(localAddr, this.Port);

                // Start listening for client requests.
                server.Start();

                //blocking call
                Console.WriteLine("Server Waiting for a connection... ");
                serverClient = server.AcceptTcpClient();

                //remove connected client ip from list of nodes to connect to
                string clientIp = ((IPEndPoint)serverClient.Client.RemoteEndPoint).Address.ToString();
                //Console.WriteLine($"Client string to remove: {clientIp}");
                //Console.WriteLine($"IPRange 0: {ipRange[0]}");
                if (ipRange.Contains(clientIp)){
                    ipRange.Remove(clientIp);
                    Console.WriteLine($"Server removed {clientIp}");
                }

                //while (true)
                //{

                //}
            }
            catch (SocketException e)
            {
                Console.WriteLine($"OpenPortandListen SocketException: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception: {e}");
            }
        }

        /// <summary>
        /// Connects to a clients and syncs up all files
        /// </summary>
        public void ConnectToClientAndSyncFiles(List<string> ipRange)
        {
            try
            {
                // Get a stream object for reading and writing
                NetworkStream stream = serverClient.GetStream();

                //grab all the files and folders this container has
                GetLatestFilesInDirectory();
                //check the file paths and names I grabbed
                //showFileAndDirCount();
                //showDirAndFileNames();

                //double check client isn't connected
                //string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();


                //receive count of all the client folders so we're ready to receive them
                int numFoldersReceived = ReceiveCount(stream);
                Console.WriteLine($"Received folder count of {numFoldersReceived} from client");

                //send a count of all folders to the client so they can be ready to receive them
                int folderCount = CountAllLocalFolders();
                Console.WriteLine($"Found {folderCount} folders to send to client");
                SendCount(stream, folderCount);
                Console.WriteLine($"Folder count of {folderCount} sent to client");

                //receive all folder names and create a list of ones I need
                String receivedText = "";
                List<string> foldersNeeded = new List<string>();
                for (int j = 0; j < numFoldersReceived; j++)
                {
                    receivedText = ReceiveMessage(stream);
                    if (!Directory.Exists(@"./" + receivedText))
                    {
                        foldersNeeded.Add(receivedText);
                        Console.WriteLine($"Server: added this folder to list of ones we need bcause I couldnt find it: {receivedText}");
                    }
                    SendMessage(stream, $"Received folder {receivedText} thanks");
                }

                //send all folder names
                for (int j = 0; j < folderCount; j++)
                {
                    SendMessage(stream, FileDirNames[j][0]);
                    Console.WriteLine($"Sent folder (from FileDirNames) {FileDirNames[j][0]} to client");
                    ReceiveMessage(stream);
                }

                //receive a count of all folders they want so we're ready to receive they're names
                // Read the response bytes
                numFoldersReceived = ReceiveCount(stream);
                Console.WriteLine($"Received needed folder count of {numFoldersReceived} from client");

                //send a count of all folders I want so its ready to receive them
                folderCount = foldersNeeded.Count;
                // Translate the passed message into ASCII and store it as a Byte array.
                SendCount(stream, folderCount);
                Console.WriteLine($"Needed folder count of {folderCount} sent to client");

                //receive list of folders they want
                receivedText = "";
                List<string> foldersTheyWant = new List<string>();
                for (int j = 0; j < numFoldersReceived; j++)
                {
                    receivedText = ReceiveMessage(stream);
                    foldersTheyWant.Add(receivedText);
                    Console.WriteLine($"Server: added this folder to list of ones they need because I received it: {receivedText}");
                    SendMessage(stream, $"Received wanted folder {receivedText} thanks");
                }

                //send list of folders I want
                for (int j = 0; j < foldersNeeded.Count; j++)
                {
                    SendMessage(stream, foldersNeeded[j]);
                    Console.WriteLine($"Sent folder I need {foldersNeeded[j]} to client");
                    ReceiveMessage(stream);
                }

                //receive count of files in all the folders we'll receive
                int numFilesReceived = ReceiveCount(stream);
                Console.WriteLine($"Received file count of {numFilesReceived} from client");

                //send count of files in all the folders they will receive
                int numFilesToSend = CountFilesToSend(foldersTheyWant);
                // Translate the passed message into ASCII and store it as a Byte array.
                SendCount(stream, numFilesToSend);
                Console.WriteLine($"Sent {numFilesToSend} to client");

                //to receive all files (loop: get file name, get file)
                for (int j = 0; j < numFilesReceived; j++)
                {
                    ReceiveFile(stream);
                }

                //to send all files (loop: send file path, send file)
                List<string> filesToSend = getFilesToSend(foldersTheyWant);
                for (int j = 0; j < filesToSend.Count; j++)
                {
                    SendFile(stream, filesToSend[j]);
                }

                // Shutdown and end connection
                serverClient.Close();

            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
            
        }

        /// <summary>
        /// Connects to a server and syncs up all files
        /// </summary>
        /// <param name="connectionAttempts">number of connection attmepts</param>
        /// <param name="ipRange">the range of ip addresses to connect to</param>
        public void ConnectToServerAndSyncFiles(int connectionAttempts, List<string> ipRange)
        {
            string ipToConnect = "";
            connectionNumber = 0;
            for (int i = 0; i < connectionAttempts; i++)
            {
                try
                {
                    //Console.WriteLine($"{connectionNumber}%{ipRange.Count} value:{connectionNumber % ipRange.Count}");
                    ipToConnect = ipRange[connectionNumber % ipRange.Count];
                    //Console.WriteLine($"ipAddress is {this.ipAddress}");
                    Console.WriteLine($"IP to try {ipToConnect}");

                    //only connect if the ip still remains in the list of ip's we need to connect to
                    if (ipRange.Contains(ipToConnect))
                    {
                        connectionNumber++;
                        //try to connect to the port
                        Console.WriteLine($"Looking for listener at {ipToConnect}");
                        TcpClient client = new TcpClient(ipToConnect, this.port);
                        Console.WriteLine($"Client connected to {ipToConnect}");

                        //if you made it here, the port connected. Remove the ip from the list of ip's we need to connect to
                        if (ipRange.Contains(ipToConnect))
                        {
                            ipRange.Remove(ipToConnect);
                            Console.WriteLine($"Client removed {ipToConnect}");
                        }

                        //get the client stream so we can read and write data
                        NetworkStream stream = client.GetStream();

                        //grab all the files and folders this container has
                        GetLatestFilesInDirectory();
                        //check the file paths and names I grabbed
                        //showFileAndDirCount();
                        //showDirAndFileNames();

                        //send a count of all folders so the server so they can be ready to receive them
                        Console.WriteLine("About to count folders");
                        int folderCount = CountAllLocalFolders();
                        Console.WriteLine($"folder count: {folderCount}");
                        SendCount(stream, folderCount);
                        Console.WriteLine($"Folder count of {folderCount} sent to server");

                        //receive count of all the server folders so we're ready to receive them
                        int numFoldersReceived = ReceiveCount(stream);
                        Console.WriteLine($"Received folder count of {numFoldersReceived} from server");

                        //send all folder names
                        for( int j = 0; j < folderCount; j++)
                        {
                            SendMessage(stream, FileDirNames[j][0]);
                            Console.WriteLine($"Sent folder (from FileDirNames) {FileDirNames[j][0]} to server");
                            ReceiveMessage(stream);
                        }

                        //receive all folder names and create a list of ones I need
                        String receivedText = "";
                        List<string> foldersNeeded = new List<string>();
                        for (int j = 0; j < numFoldersReceived; j++)
                        {
                            receivedText = ReceiveMessage(stream);
                            if(!Directory.Exists(@"./" + receivedText))
                            {
                                foldersNeeded.Add(receivedText);
                                Console.WriteLine($"Client: added this folder to list of ones we need bcause I couldnt find it: {receivedText}");
                            }
                            SendMessage(stream, $"client received folder {receivedText}");
                        }

                        //send a count of all folders I want so its ready to receive them
                        folderCount = foldersNeeded.Count;
                        SendCount(stream, folderCount);
                        Console.WriteLine($"Needed folder count of {folderCount} sent to server");

                        //receive a count of all folders they want so we're ready to receive they're names
                        // Read the response bytes
                        numFoldersReceived = ReceiveCount(stream);
                        Console.WriteLine($"Received needed folder count of {numFoldersReceived} from server");

                        //send list of folders I want
                        for (int j = 0; j < foldersNeeded.Count; j++)
                        {
                            SendMessage(stream, foldersNeeded[j]);
                            Console.WriteLine($"Sent folder I need {foldersNeeded[j]} to server");
                            ReceiveMessage(stream);
                        }

                        //receive list of folders they want
                        receivedText = "";
                        List<string> foldersTheyWant = new List<string>();
                        for (int j = 0; j < numFoldersReceived; j++)
                        {
                            receivedText = ReceiveMessage(stream);
                            foldersTheyWant.Add(receivedText);
                            Console.WriteLine($"Client: added this folder to list of ones they need because I received it: {receivedText}");
                            SendMessage(stream, $"client received wanted folder {receivedText} thanks");
                        }

                        //send count of files in all the folders they will receive
                        int numFilesToSend = CountFilesToSend(foldersTheyWant);
                        if (numFilesToSend > 0)
                        {
                            SendCount(stream, numFilesToSend);
                        }

                        //receive count of files in all the folders we'll receive
                        int numFilesReceived = ReceiveCount(stream);
                        Console.WriteLine($"Received file count of {numFilesReceived} from server");

                        //to send all files (loop: send file path, send file)
                        List<string> filesToSend = getFilesToSend(foldersTheyWant);
                        for (int j = 0; j < filesToSend.Count; j++)
                        {
                            SendFile(stream, filesToSend[j]);
                        }

                        //to receive all files (loop: get file name, get file)
                        for (int j = 0; j < numFilesReceived; j++)
                        {
                            ReceiveFile(stream);
                        }

                        //done sending files, close the connection and connect to the next server
                        client.Close();
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine($"Client couldn't connect to {ipToConnect}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception {e}");
                }
            }
        }

        /// <summary>
        /// for testing only, creates a list of listeners found
        /// </summary>
        /// <param name="connectionAttempts"> number of allowed connection attempts</param>
        public void FindTcpListeners(int connectionAttempts, List<string> ipRange)
        {
            tcpListeners = new List<string>();
            string ipToConnect = "";
            connectionNumber = 0;
            for(int i = 0; i < connectionAttempts; i++)
            {
                try
                {

                    //Console.WriteLine($"{connectionNumber}%{ipRange.Count} value:{connectionNumber % ipRange.Count}");
                    ipToConnect = ipRange[connectionNumber % ipRange.Count];
                    //Console.WriteLine($"ipAddress is {this.ipAddress}");
                    Console.WriteLine($"IP to try {ipToConnect}");
                    if (ipRange.Contains(ipToConnect)){
                        connectionNumber++;
                        //try to connect to the port
                        Console.WriteLine($"Looking for listener at {ipToConnect}");
                        TcpClient client = new TcpClient(ipToConnect, this.port);

                        //if you made it here, the port connected
                        if (ipRange.Contains(ipToConnect))
                        {
                            ipRange.Remove(ipToConnect);
                            Console.WriteLine($"Client string was removed");
                        }
                        tcpListeners.Add(ipToConnect);
                        Console.WriteLine($"Added listener at {ipToConnect}");
                        client.Close();
                        
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine($"Client couldn't connect to {ipToConnect}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception {e}");
                }
            }
        }

        /// <summary>
        /// Displays all listeners found
        /// </summary>
        public void DisplayListenersFound()
        {
            foreach (string listener in tcpListeners)
            {
                Console.WriteLine($"Found listener: {listener}");
            }
        }

        /// <summary>
        /// counts all the files our container has
        /// </summary>
        /// <returns>the number of files we have</returns>
        public int CountAllLocalFiles()
        {
            int count = 0;
            for(int i = 0; i < AllFilesPaths.Length; i++)
            {
                for(int j = 0; j < AllFilesPaths[i].Length; j++)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// counts all the folders our container has
        /// </summary>
        /// <returns>the number of folders our container has</returns>
        public int CountAllLocalFolders()
        {
            return FileDirNames.Length;
        }

        /// <summary>
        /// Counts all the files to send
        /// </summary>
        /// <param name="folders"></param>
        /// <returns>the number of files to send</returns>
        public int CountFilesToSend(List<string> folders)
        {
            int count = 0;
            for(int i = 0; i < FileDirNames.Length; i++)
            {
                if (FileDirNames[i][0].Equals(folders[i]))
                {
                    for(int j = 1; j < FileDirNames[i].Length; j++)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// grabs all the files we need to send
        /// </summary>
        /// <param name="foldersWanted"></param>
        /// <returns>a list of all the files we need to send</returns>
        public List<string> getFilesToSend(List<string> foldersWanted)
        {
            List<string> filesToSend = new List<string>();
            for(int i = 0; i < FileDirNames.Length; i++)
            {
                if (FileDirNames[i][0].Equals(foldersWanted[i])){
                    for (int j = 0; j < FileDirNames[i].Length - 1; j++)
                    {
                        filesToSend.Add(AllFilesPaths[i][j]);
                    }
                }
                
            }
            return filesToSend;
        }

        /// <summary>
        /// Receive a message from the connected client
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>a string of the received message</returns>
        public string ReceiveMessage(NetworkStream stream)
        {
            byte[] data = new Byte[256];
            int bytes = stream.Read(data, 0, data.Length);
            string messageReceived = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
            return messageReceived;
        }

        /// <summary>
        /// Receive a count from the connected client so we know how many data points to expect
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>returns true is no errors happen when trying to receive the message</returns>
        public int ReceiveCount(NetworkStream stream)
        {
            byte[] data = new Byte[4];
            int bytes = stream.Read(data, 0, data.Length);
            int numFilesReceived = BitConverter.ToInt32(data, 0);
            return numFilesReceived;
        }

        /// <summary>
        /// Send a message to the connected client
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns>Will return true if no errors happen when trying to receive the count</returns>
        public bool SendMessage(NetworkStream stream, string message)
        {
            byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
            return true;
        }

        /// <summary>
        /// Send a count of the data point we wish to send so they know how many to expect
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="count"></param>
        /// <returns>returns true is no errors happen when trying to send the message</returns>
        public bool SendCount(NetworkStream stream, int count)
        {
            byte[] data = BitConverter.GetBytes(count);
            stream.Write(data, 0, data.Length);
            return true;
        }

        /// <summary>
        /// Receive a file from the connected client and save it
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>returns true is no errors happen when trying to receive the file</returns>
        public bool ReceiveFile(NetworkStream stream)
        {
            string filePath = "";
            string[] splitPath;
            string folder = "";
            string fileName = "";
            byte[] data = new Byte[256];

            //get file path
            filePath = ReceiveMessage(stream);
            Console.WriteLine($"Server: Filepath received: {filePath}");  //testing only
            SendMessage(stream, "got the filePath thanks");

            //split the path to get the folder and file name
            splitPath = filePath.Split('/');
            folder = splitPath[1];
            fileName = splitPath[2];
            Console.WriteLine($"Server: received this file name: {fileName}");  //testing only

            //get file
            stream.Read(data, 0, data.Length);

            //create a directory for the file if needed
            Directory.CreateDirectory(".//" + folder);

            //write all sent bytes to a files
            File.WriteAllBytes((".//" + folder + "//" + fileName), data);
            Console.WriteLine("Saved file ./" + folder + "/" + fileName);  //testing only
            SendMessage(stream, $"got the file {fileName} thanks");

            return true;
        }

        /// <summary>
        /// Send a file to the connected client
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="filePath"></param>
        /// <returns>returns true is no errors happen when trying to send the file</returns>
        public bool SendFile(NetworkStream stream, string filePath)
        {
            byte[] fileBeingSent;

            //send file path
            SendMessage(stream, filePath);
            Console.WriteLine($"Sent the file name im about to send {filePath}");  //testing only
            ReceiveMessage(stream);

            //send file
            fileBeingSent = File.ReadAllBytes(filePath);
            stream.Write(fileBeingSent, 0, fileBeingSent.Length);
            Console.WriteLine($"Sent file the server needed {filePath}");  //testing only
            ReceiveMessage(stream);

            return true;
        }

        #endregion
    }
}
