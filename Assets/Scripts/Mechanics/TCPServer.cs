using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Platformer.Mechanics;

public class TCPServer : MonoBehaviour
{
    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];
    public int port = 12345;

    void Start()
    {
        StartServer();
    }

    void StartServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Debug.Log($"Server started on port {port}");

            // Begin accepting a connection
            server.BeginAcceptTcpClient(OnClientConnected, null);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error starting server: {e.Message}");
        }
    }

    void OnClientConnected(IAsyncResult result)
    {
        client = server.EndAcceptTcpClient(result);
        stream = client.GetStream();
        Debug.Log("Client connected");

        // Begin reading incoming data
        stream.BeginRead(buffer, 0, buffer.Length, OnDataReceived, null);
    }

    void OnDataReceived(IAsyncResult result)
    {
        if (stream == null) return;

        int bytesRead = stream.EndRead(result);
        if (bytesRead > 0)
        {
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log($"Received: {message}");

            // Handle joystick input or other commands here
            HandleInput(message);

            // Keep listening for data
            stream.BeginRead(buffer, 0, buffer.Length, OnDataReceived, null);
        }
    }

    void HandleInput(string input)
    {
        // Example: Split input into parts and use it
        string[] parts = input.Split(':');
        if (parts.Length == 2)
        {
            string command = parts[0];
            string value = parts[1];

            if (command == "MOVE")
            {
                Debug.Log($"Moving character with value: {value}");
              

            }
        }
    }
    
    void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
        server?.Stop();
    }
}
