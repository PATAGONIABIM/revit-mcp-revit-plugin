import socket
import json
import sys

def send_dynamo_command(command_name, args=None):
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
            print(f"\n--- Sending command: {command_name} ---")
            s.connect((host, port))
            
            message = json.dumps(payload) + "\n"
            s.sendall(message.encode('utf-8'))
            
            data = s.recv(16384)
            response = data.decode('utf-8').strip()
            print(f"Response:\n{response}")
            return response
    except ConnectionRefusedError:
        print(f"[!] Could not connect to Revit on {host}:{port}. Is Revit running with RevitMCP loaded?")
    except Exception as e:
        print(f"[!] Error: {e}")

if __name__ == "__main__":
    action = sys.argv[1] if len(sys.argv) > 1 else "list"
    
    if action == "list":
        send_dynamo_command("dynamo_list_scripts")
    elif action == "generate":
        send_dynamo_command("dynamo_generate_graph", {
            "name": "MCP_Dynamo_Column_Placer",
            "description": "Graph generated via Revit MCP",
            "inputs": [
                {"name": "Spacing", "type": "number", "default_value": 5.0},
                {"name": "Prefix", "type": "string", "default_value": "COL-"}
            ],
            "python_code": "# Python logic\nOUT = f'Processed spacing: {IN[0]} with prefix: {IN[1]}'"
        })
    elif action == "info":
        path = sys.argv[2] if len(sys.argv) > 2 else r"D:\MCP\REVIT\scripts\test_generated_wall_filter.dyn"
        send_dynamo_command("dynamo_get_script_info", {"script_path": path})
    elif action == "geometry":
        send_dynamo_command("create_dynamo_geometry", {"name": "Test Sphere"})
    else:
        print("Usage: python test_dynamo.py [list|generate|info|geometry]")
