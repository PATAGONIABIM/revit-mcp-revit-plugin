# Revit MCP + Revit Plugin (2026)

[Español abajo]

## English

### Overview
This project integrates **Revit 2026** with the **Model Context Protocol (MCP)**, allowing AI assistants (like Claude, Gemini, etc.) to interact directly with Revit. 

It consists of two main components:
1. **Revit Plugin (C#)**: A Revit Add-in that starts a TCP Socket Server inside Revit. It accepts commands to query and modify the BIM model (create walls, levels, grids, etc.).
2. **MCP Server (Node.js/TypeScript)**: A standalone server that implements the MCP protocol. It receives tool calls from the AI, translates them into socket commands, and sends them to the Revit Plugin.

### Features
*   **Analysis**: Query Levels, Grids, Walls, and other elements.
*   **Modeling**: Create Walls, Doors, Windows, Floors, Roofs, and more.
*   **Metadata**: Retrieve project location, base points, and specific element parameters.
*   **Bi-directional Communication**: Real-time feedback from Revit to the AI.

### Installation

#### 1. Revit Plugin (C#)
*   Open the solution `RevitMCP.sln` in Visual Studio.
*   Restore NuGet packages (Revit API references).
*   Build the solution.
*   Copy the output `.dll` and `.addin` file to your Revit Addins folder (usually `%AppData%\Autodesk\Revit\Addins\2026`).
*   Start Revit 2026. You should see a notification that the server has started on port **2026**.

#### 2. MCP Server (Node.js)
*   Navigate to the `mcp-server` directory.
*   Install dependencies:
    ```bash
    npm install
    ```
*   Build the project (if using TypeScript):
    ```bash
    npm run build
    ```
*   Start the server:
    ```bash
    node dist/index.js
    ```
    Or connect it to your MCP client (like Claude Desktop or an IDE assistant) by adding the configuration to your MCP settings file.

---

## Español

### Descripción General
Este proyecto integra **Revit 2026** con el **Model Context Protocol (MCP)**, permitiendo que asistentes de IA (como Claude, Gemini, etc.) interactúen directamente con Revit.

Consta de dos componentes principales:
1.  **Plugin de Revit (C#)**: Un Add-in de Revit que inicia un Servidor de Sockets TCP dentro de Revit. Acepta comandos para consultar y modificar el modelo BIM (crear muros, niveles, rejillas, etc.).
2.  **Servidor MCP (Node.js/TypeScript)**: Un servidor independiente que implementa el protocolo MCP. Recibe llamadas de herramientas de la IA, las traduce en comandos de socket y las envía al Plugin de Revit.

### Características
*   **Análisis**: Consultar Niveles, Rejillas, Muros y otros elementos.
*   **Modelado**: Crear Muros, Puertas, Ventanas, Suelos, Techos y más.
*   **Metadatos**: Obtener ubicación del proyecto, puntos base y parámetros de elementos.
*   **Comunicación Bidireccional**: Feedback en tiempo real desde Revit a la IA.

### Instalación

#### 1. Plugin de Revit (C#)
*   Abre la solución `RevitMCP.sln` en Visual Studio.
*   Restaura los paquetes NuGet (referencias a la API de Revit).
*   Compila la solución.
*   Copia el archivo `.dll` de salida y el archivo `.addin` a tu carpeta de Addins de Revit (usualmente `%AppData%\Autodesk\Revit\Addins\2026`).
*   Inicia Revit 2026. Deberías ver una notificación de que el servidor ha iniciado en el puerto **2026**.

#### 2. Servidor MCP (Node.js)
*   Navega al directorio `mcp-server`.
*   Instala las dependencias:
    ```bash
    npm install
    ```
*   Compila el proyecto (si usas TypeScript):
    ```bash
    npm run build
    ```
*   Inicia el servidor:
    ```bash
    node dist/index.js
    ```
    O conéctalo a tu cliente MCP (como Claude Desktop o un asistente de IDE) añadiendo la configuración a tu archivo de configuración MCP.
