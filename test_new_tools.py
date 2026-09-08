import socket
import json
import sys

def send_command(command_name, args=None):
    host = '127.0.0.1'
    port = 2026
    if args is None:
        args = {}
        
    payload = {
        "command": command_name,
        "args": args
    }
    
    try:
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            s.settimeout(10)
            print(f"\n--- Testing: {command_name} ---")
            s.connect((host, port))
            
            message = json.dumps(payload) + "\n"
            s.sendall(message.encode('utf-8'))
            
            data = s.recv(8192)
            response = data.decode('utf-8').strip()
            print(f"Response:\n{response}")
            return response
    except ConnectionRefusedError:
        print(f"[!] Could not connect to Revit on {host}:{port}. Is Revit running with RevitMCP plugin loaded?")
    except Exception as e:
        print(f"[!] Error executing {command_name}: {e}")

if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "all"
    
    if cmd == "all" or cmd == "get_project_info":
        send_command("get_project_info")
        
    if cmd == "all" or cmd == "threedelab_get_info":
        send_command("threedelab_get_info")
        
    if cmd == "all" or cmd == "threedelab_get_timer_info":
        send_command("threedelab_get_timer_info")
