import socket

# Server settings
UDP_IP = "127.0.0.1"  # Localhost (match Unity server IP)
UDP_PORT = 5005       # Match Unity server port

# Create a UDP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Send data to Unity
message = "Hello from Python!"
sock.sendto(message.encode(), (UDP_IP, UDP_PORT))
print(f"Message sent: {message}")

