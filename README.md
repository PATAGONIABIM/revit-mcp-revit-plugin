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
*   **Metadata & Project Info**: Retrieve and update Project Information (Project Name, Number, Client, Author/Architect, Issue Date, Status, Address, Organization, Custom Parameters), location, and base points.
*   **3DELAB Tools Integration**: Direct interface to `3DELAB Tools` (`D:\REVIT_3DELAB_TOOLS`):
    *   Inspect plugin capabilities and commands (`threedelab_get_info`).
    *   Query session and project timer, rates, and costs (`threedelab_get_timer_info`).
    *   Automated spatial batch wall joining (`threedelab_batch_wall_join`).
    *   High-resolution view image export (`threedelab_export_views`).
    *   In-place family detection & template mapping (`threedelab_check_inplace_family`).
    *   Face paint removal (`threedelab_remove_paint`).
*   **Dynamo Integration**: Complete Dynamo workflow automation:
    *   Discover and list `.dyn` scripts (`dynamo_list_scripts`).
    *   Inspect inputs, outputs, Python nodes, and dependencies (`dynamo_get_script_info`).
    *   Modify input values without opening GUI (`dynamo_modify_inputs`).
    *   Execute Dynamo scripts headlessly on active model (`dynamo_run_script`).
    *   Generate `.dyn` graphs from scratch with clean visual layout and CPython3 code (`dynamo_generate_graph`).
    *   Execute Python scripts dynamically in Revit context (`dynamo_run_python`).
*   **Bi-directional Communication**: Real-time feedback from Revit to the AI.

### Installation

#### 1. Revit Plugin (C#)
*   Open `RevitMCP.Plugin/RevitMCP.Plugin.csproj` in Visual Studio (or build using `dotnet build RevitMCP.Plugin`).
*   Restore NuGet packages (Revit API references).
*   Build the project.
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
*   **Metadatos e Información de Proyecto**: Consultar y actualizar información de proyecto (Nombre, Número, Mandante/Cliente, Arquitecto/Autor, Fecha de emisión, Estado, Dirección, Organización y parámetros personalizados).
*   **Integración con 3DELAB Tools**: Interfaz directa con `3DELAB Tools` (`D:\REVIT_3DELAB_TOOLS`):
    *   Consultar capacidades y comandos del plugin (`threedelab_get_info`).
    *   Consultar tiempos de sesión, acumulado por proyecto, tarifa por hora y costos (`threedelab_get_timer_info`).
    *   Unión automática de muros en batch por hashing espacial (`threedelab_batch_wall_join`).
    *   Exportación directa de vistas a imágenes en alta resolución (`threedelab_export_views`).
    *   Inspección y plantilla de conversión de familias in-situ (`threedelab_check_inplace_family`).
    *   Remoción de pintura de caras (`threedelab_remove_paint`).
*   **Integración con Dynamo**: Automatización completa de flujos de Dynamo:
    *   Búsqueda y listado de scripts `.dyn` (`dynamo_list_scripts`).
    *   Inspección de entradas (`inputs`), salidas (`outputs`), nodos Python y dependencias (`dynamo_get_script_info`).
    *   Modificación de valores de entrada sin abrir Dynamo (`dynamo_modify_inputs`).
    *   Ejecución automatizada de grafos `.dyn` en el modelo activo (`dynamo_run_script`).
    *   Generación de grafos `.dyn` por IA con layout visual y código CPython3 (`dynamo_generate_graph`).
    *   Ejecución dinámica de scripts Python con la API de Revit (`dynamo_run_python`).
*   **Comunicación Bidireccional**: Feedback en tiempo real desde Revit a la IA.

### Instalación

#### 1. Plugin de Revit (C#)
*   Abre `RevitMCP.Plugin/RevitMCP.Plugin.csproj` en Visual Studio (o compila ejecutando `dotnet build RevitMCP.Plugin`).
*   Restaura los paquetes NuGet (referencias a la API de Revit).
*   Compila el proyecto.
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
