using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class JoystickController : MonoBehaviour
{
    private TcpListener listener;
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];

    public float xAxis;
    public float yAxis;
    public bool buttonA;

    void Start()
    {
        // Listen for incoming connection from Raspberry Pi
        listener = new TcpListener(IPAddress.Any, 12345);
        listener.Start();
        listener.BeginAcceptTcpClient(OnConnect, null);
    }

    void OnConnect(IAsyncResult ar)
    {
        client = listener.EndAcceptTcpClient(ar);
        stream = client.GetStream();
        stream.BeginRead(buffer, 0, buffer.Length, OnReceive, null);
        Debug.Log("Connected to Raspberry Pi!");
    }

    void OnReceive(IAsyncResult ar)
    {
        int bytesRead = stream.EndRead(ar);
        if (bytesRead > 0)
        {
            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            string[] values = data.Trim().Split(',');

            if (values.Length == 3)
            {
                // Parse joystick data
                xAxis = float.Parse(values[0]);
                yAxis = float.Parse(values[1]);
                buttonA = values[2] == "1";

                Debug.Log($"X: {xAxis}, Y: {yAxis}, Button A: {buttonA}");
            }

            // Continue reading data
            stream.BeginRead(buffer, 0, buffer.Length, OnReceive, null);
        }
    }

    void Update()
    {
        // Example: Control player movement based on joystick input
        transform.Translate(new Vector3(xAxis, yAxis, 0) * Time.deltaTime * 5);

        // Example: Trigger action if button A is pressed
        if (buttonA)
        {
            Debug.Log("Button A Pressed");
        }
    }

    void OnApplicationQuit()
    {
        // Cleanup on exit
        stream?.Close();
        client?.Close();
        listener?.Stop();
    }
}
