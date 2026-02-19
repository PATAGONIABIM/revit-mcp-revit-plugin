import socket
import json
import sys

def test_location():
    host = '127.0.0.1'
    port = 2026
    
    command = {
        "command": "get_project_location",
        "args": {}
    }
    
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.settimeout(5)
            print(f"Connecting to {host}:{port}...")
            s.connect((host, port))
            
            message = json.dumps(command) + "\n"
            print(f"Sending: {message.strip()}")
            s.sendall(message.encode('utf-8'))
            
            data = s.recv(4096)
            response = data.decode('utf-8')
            print(f"Received: {response}")
            
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    test_location()
