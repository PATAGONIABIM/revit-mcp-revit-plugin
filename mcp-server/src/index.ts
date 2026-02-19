import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
    CallToolRequestSchema,
    ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { z } from "zod";
import * as net from "net";

const server = new Server(
    {
        name: "revit-mcp-server",
        version: "1.0.0",
    },
    {
        capabilities: {
            tools: {},
        },
    }
);

// Helper to send command to Revit Plugin
async function sendToRevit(command: string, args: any): Promise<any> {
    return new Promise((resolve, reject) => {
        const client = new net.Socket();
        let dataBuffer = "";

        client.connect(2026, "127.0.0.1", () => {
            const request = { command, args };
            client.write(JSON.stringify(request) + "\n");
        });

        client.on("data", (data) => {
            dataBuffer += data.toString();
            // Revit plugin sends newline-delimited JSON responses
            const newlineIndex = dataBuffer.indexOf("\n");
            if (newlineIndex !== -1) {
                const line = dataBuffer.substring(0, newlineIndex);
                client.destroy();
                try {
                    // Strip BOM and whitespace
                    const cleanLine = line.trim().replace(/^\uFEFF/, '');
                    const response = JSON.parse(cleanLine);
                    resolve(response);
                } catch (e) {
                    reject(new Error(`Failed to parse response: ${line}`));
                }
            }
        });

        client.on("error", (err) => {
            reject(err);
        });

        // Timeout after 30 seconds
        client.setTimeout(30000, () => {
            client.destroy();
            reject(new Error("Connection to Revit timed out after 30s"));
        });
    });
}

server.setRequestHandler(ListToolsRequestSchema, async () => {
    return {
        tools: [
            {
                name: "get_levels",
                description:
                    "Get all levels in the project with their IDs and elevations.",
                inputSchema: z.object({}),
            },
            {
                name: "get_wall_types",
                description: "Get all available wall types in the project.",
                inputSchema: z.object({}),
            },
            {
                name: "create_level",
                description: "Create a new level at a specific elevation.",
                inputSchema: z.object({
                    elevation: z.number().describe("Elevation of the level in internal units (feet)"),
                    name: z.string().optional().describe("Name of the level"),
                }),
            },
            {
                name: "create_grid",
                description: "Create a grid line between two points.",
                inputSchema: z.object({
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point"),
                    name: z.string().optional().describe("Name of the grid"),
                }),
            },
            {
                name: "create_wall",
                description: "Create a wall given start/end points, level, and type.",
                inputSchema: z.object({
                    start: z.object({ x: z.number(), y: z.number(), z: z.number() }),
                    end: z.object({ x: z.number(), y: z.number(), z: z.number() }),
                    level_id: z.string().describe("Element ID of the level"),
                    type_name: z
                        .string()
                        .optional()
                        .describe(
                            "Name of the wall type. If omitted, uses the first available type."
                        ),
                }),
            },
            {
                name: "create_arc_wall",
                description: "Create a wall along an arc defined by three points.",
                inputSchema: z.object({
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point"),
                    p3: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point on arc"),
                    level_id: z.string().describe("Element ID of the level"),
                    type_name: z.string().optional().describe("Wall type name"),
                    height: z.number().optional().describe("Wall height in feet"),
                }),
            },
            {
                name: "create_ellipse_wall",
                description: "Create a wall along a full ellipse.",
                inputSchema: z.object({
                    center: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Center of the ellipse"),
                    radius_x: z.number().describe("Radius along X axis"),
                    radius_y: z.number().describe("Radius along Y axis"),
                    level_id: z.string().describe("Element ID of the level"),
                    type_name: z.string().optional().describe("Wall type name"),
                    height: z.number().optional().describe("Wall height in feet"),
                }),
            },
            {
                name: "create_floor",
                description:
                    "Create a floor from a list of points defining a closed loop.",
                inputSchema: z.object({
                    points: z
                        .array(z.object({ x: z.number(), y: z.number(), z: z.number() }))
                        .describe("List of points forming a closed loop"),
                    level_id: z.string().describe("Element ID of the level"),
                }),
            },
            {
                name: "create_column",
                description: "Create a structural or architectural column.",
                inputSchema: z.object({
                    location: z.object({ x: z.number(), y: z.number(), z: z.number() }),
                    level_id: z.string().describe("Element ID of the level"),
                    type_name: z.string().optional().describe("Name of the column family type"),
                }),
            },
            {
                name: "create_window",
                description: "Create a window in a wall.",
                inputSchema: z.object({
                    wall_id: z.string().describe("Element ID of the host wall"),
                    location: z.object({ x: z.number(), y: z.number(), z: z.number() }),
                    level_id: z.string().describe("Element ID of the level"),
                    type_name: z.string().optional().describe("Name of the window family type"),
                }),
            },
            {
                name: "create_door",
                description: "Create a door in a wall.",
                inputSchema: z.object({
                    wall_id: z.string().describe("Element ID of the host wall"),
                    location: z.object({ x: z.number(), y: z.number(), z: z.number() }),
                    level_id: z.string().describe("Element ID of the level"),
                    type_name: z.string().optional().describe("Name of the door family type"),
                }),
            },
            {
                name: "create_opening",
                description: "Create a rectangular opening in a wall.",
                inputSchema: z.object({
                    wall_id: z.string().describe("Element ID of the host wall"),
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("First corner of opening on wall face"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Diagonal corner of opening on wall face"),
                }),
            },
            {
                name: "create_roof",
                description: "Create a footprint roof from a list of points.",
                inputSchema: z.object({
                    points: z
                        .array(z.object({ x: z.number(), y: z.number(), z: z.number() }))
                        .describe("List of points forming a closed loop (footprint)"),
                    level_id: z.string().describe("Element ID of the level (base level)"),
                    type_name: z.string().optional().describe("Name of the roof type"),
                    slope: z.number().optional().describe("Slope as a decimal percentage (e.g., 0.03 for 3%)"),
                }),
            },
            {
                name: "create_beam",
                description: "Create a structural beam between two points.",
                inputSchema: z.object({
                    start: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point"),
                    end: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point"),
                    level_id: z.string().describe("Element ID of the level needed for reference"),
                    type_name: z.string().optional().describe("Name of the Structural Framing family"),
                }),
            },
            {
                name: "create_railing",
                description: "Create a railing along a path.",
                inputSchema: z.object({
                    points: z
                        .array(z.object({ x: z.number(), y: z.number(), z: z.number() }))
                        .describe("List of points forming the path"),
                    level_id: z.string().describe("Element ID of the level"),
                    type_name: z.string().optional().describe("Name of the Railing type"),
                }),
            },
            {
                name: "create_text_note",
                description: "Create a text note.",
                inputSchema: z.object({
                    text: z.string().describe("Content of the note"),
                    location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Location point"),
                    view_id: z.string().optional().describe("View ID (defaults to active view)"),
                    type_id: z.string().optional().describe("Text Note Type ID"),
                }),
            },
            {
                name: "create_tag",
                description: "Tag an element.",
                inputSchema: z.object({
                    element_id: z.string().describe("Element to tag"),
                    location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Location of the tag head"),
                    view_id: z.string().optional().describe("View ID (defaults to active view)"),
                }),
            },
            {
                name: "create_dimension",
                description: "Create a linear dimension between specified elements (e.g. Grids).",
                inputSchema: z.object({
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point of dimension line"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point of dimension line"),
                    element_ids: z.array(z.string()).describe("List of Element IDs (Grids) to dimension"),
                    view_id: z.string().optional().describe("View ID (defaults to active view)"),
                }),
            },
            {
                name: "get_views",
                description: "Get available views (Plans, 3D).",
                inputSchema: z.object({}),
            },
            {
                name: "create_duct",
                description: "Create a duct between two points.",
                inputSchema: z.object({
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point"),
                    level_id: z.string().describe("Level ID"),
                    system_type: z.string().optional().describe("Mechanical System Type Name"),
                    duct_type: z.string().optional().describe("Duct Type Name"),
                }),
            },
            {
                name: "create_pipe",
                description: "Create a pipe between two points.",
                inputSchema: z.object({
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point"),
                    level_id: z.string().describe("Level ID"),
                    system_type: z.string().optional().describe("Piping System Type Name"),
                    pipe_type: z.string().optional().describe("Pipe Type Name"),
                }),
            },
            {
                name: "get_mep_types",
                description: "Get available Duct, Pipe and System types.",
                inputSchema: z.object({}),
            },
            {
                name: "create_room",
                description: "Create a room at a specific location.",
                inputSchema: z.object({
                    location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point inside the room"),
                    level_id: z.string().describe("Level ID"),
                    name: z.string().optional().describe("Room Name"),
                    number: z.string().optional().describe("Room Number"),
                }),
            },
            {
                name: "create_room_separator",
                description: "Create room separator lines.",
                inputSchema: z.object({
                    points: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).describe("Points defining the separator lines"),
                    level_id: z.string().describe("Level ID"),
                }),
            },
            {
                name: "tag_room",
                description: "Tag a room.",
                inputSchema: z.object({
                    room_id: z.string().describe("Room Element ID"),
                    location: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Location of the tag"),
                    view_id: z.string().optional().describe("View ID (defaults to active view)"),
                }),
            },
            {
                name: "create_material",
                description: "Create or retrieve a material.",
                inputSchema: z.object({
                    name: z.string().describe("Material Name"),
                    color: z.object({ r: z.number(), g: z.number(), b: z.number() }).optional().describe("RGB Color (0-255)"),
                }),
            },
            {
                name: "create_toposolid",
                description: "Create a toposolid from a boundary profile.",
                inputSchema: z.object({
                    points: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).describe("Boundary Points"),
                    level_id: z.string().describe("Level ID"),
                }),
            },
            {
                name: "create_mass",
                description: "Create a simple mass form (extrusion).",
                inputSchema: z.object({
                    points: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).describe("Base Profile Points"),
                    height: z.number().describe("Extrusion Height"),
                }),
            },
            {
                name: "get_roof_types",
                description: "Get available roof types in the project.",
                inputSchema: z.object({}),
            },
            {
                name: "get_grids",
                description: "Get all grids in the project.",
                inputSchema: z.object({}),
            },
            {
                name: "get_railing_types",
                description: "Get available railing types in the project.",
                inputSchema: z.object({}),
            },
            {
                name: "get_families",
                description: "Get available families for a category.",
                inputSchema: z.object({
                    category: z.enum(["Doors", "Windows", "Columns", "StructuralFraming"]).describe("Category to list families for"),
                }),
            },
            {
                name: "get_project_location",
                description: "Get the project location (city, lat, lon).",
                inputSchema: z.object({}),
            },
            {
                name: "get_project_base_point",
                description: "Get the project base point and survey point information.",
                inputSchema: z.object({}),
            },
            {
                name: "create_stairs",
                description: "Create a straight run of stairs.",
                inputSchema: z.object({
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Start point of run"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("End point of run"),
                    bottom_level_id: z.string().describe("Bottom Level ID"),
                    top_level_id: z.string().describe("Top Level ID"),
                }),
            },
            {
                name: "delete_element",
                description: "Delete an element from the project.",
                inputSchema: z.object({
                    id: z.string().describe("Element ID to delete"),
                }),
            },
            {
                name: "rotate_element",
                description: "Rotate an element around a vertical axis.",
                inputSchema: z.object({
                    id: z.string().describe("Element ID to rotate"),
                    angle_degrees: z.number().describe("Angle in degrees (clockwise is positive)"),
                    axis_point: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Point for rotation axis (defaults to center)"),
                }),
            },
            {
                name: "move_element",
                description: "Move an element by a vector.",
                inputSchema: z.object({
                    element_id: z.string().describe("Element ID to move"),
                    translation: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Translation vector"),
                }),
            },
            {
                name: "copy_element",
                description: "Copy an element by a vector.",
                inputSchema: z.object({
                    element_id: z.string().describe("Element ID to copy"),
                    translation: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Translation vector"),
                }),
            },
            {
                name: "mirror_element",
                description: "Mirror an element across a plane defined by a point and normal.",
                inputSchema: z.object({
                    element_id: z.string().describe("Element ID to mirror"),
                    plane_point: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point on the mirror plane"),
                    plane_normal: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Normal vector of the mirror plane"),
                    copy: z.boolean().optional().describe("Whether to copy the element (default: false)"),
                }),
            },
            {
                name: "array_element",
                description: "Create a linear array of an element.",
                inputSchema: z.object({
                    element_id: z.string().describe("Element ID to array"),
                    count: z.number().describe("Total number of elements in array"),
                    translation: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Translation vector to the second element"),
                }),
            },
            {
                name: "align_elements",
                description: "Align two elements using their faces.",
                inputSchema: z.object({
                    reference1: z.string().describe("Stable representation of the target face"),
                    reference2: z.string().describe("Stable representation of the source face to move"),
                }),
            },
            {
                name: "select_face",
                description: "Prompt the user to select a face in Revit.",
                inputSchema: z.object({
                    prompt: z.string().optional().describe("Message to show in status bar"),
                }),
            },
            {
                name: "select_element",
                description: "Prompt the user to select an element in Revit.",
                inputSchema: z.object({
                    prompt: z.string().optional().describe("Message to show in status bar"),
                }),
            },
            {
                name: "create_reference_plane",
                description: "Create a reference plane.",
                inputSchema: z.object({
                    p1: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("First point on plane"),
                    p2: z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Second point on plane"),
                    cut_vector: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Cut vector (defaults to Z axis)"),
                    name: z.string().optional().describe("Name of the reference plane"),
                }),
            },
            {
                name: "create_extrusion",
                description: "Create a DirectShape extrusion (Generic Model). Supports complex profiles (Lines, Arcs, Circles, Splines).",
                inputSchema: z.object({
                    profile: z.array(z.object({
                        type: z.enum(["Line", "Arc", "Circle", "Spline"]).describe("Curve type"),
                        start: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Start point (Line/Arc)"),
                        end: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("End point (Line/Arc)"),
                        point_on_arc: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Point on arc (Arc)"),
                        center: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Center point (Circle)"),
                        radius: z.number().optional().describe("Radius in meters (Circle)"),
                        normal: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional().describe("Normal vector (Circle)"),
                        points: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).optional().describe("Control points (Spline)"),
                    })).optional().describe("List of curves forming the closed loop"),
                    points: z.array(z.object({ x: z.number(), y: z.number(), z: z.number() })).optional().describe("Legacy: Simple polygon points"),
                    height: z.number().optional().describe("Extrusion height in meters (default 1.0)"),
                    name: z.string().optional().describe("Name of the created element"),
                }),
            },
            {
                name: "create_dynamo_geometry",
                description: "Create complex geometry (e.g. Boolean Unions) using Dynamo's ProtoGeometry engine.",
                inputSchema: z.object({
                    name: z.string().optional().describe("Name of the element"),
                }),
            },
            {
                name: "create_column_grid",
                description: "Create a grid of columns and axes. Parametric design example.",
                inputSchema: z.object({
                    width: z.number().describe("Total width of the grid in meters (X axis)"),
                    depth: z.number().describe("Total depth of the grid in meters (Y axis)"),
                    spacing_x: z.number().describe("Spacing between columns in X axis (meters)"),
                    spacing_y: z.number().describe("Spacing between columns in Y axis (meters)"),
                    height: z.number().optional().describe("Height of the columns in meters (default 3.0)"),
                }),
            },
        ],
    };
});

server.setRequestHandler(CallToolRequestSchema, async (request) => {
    try {
        const args = request.params.arguments ?? {};
        const result = await sendToRevit(request.params.name, args);

        if (result.error) {
            return {
                content: [
                    { type: "text", text: `Error from Revit: ${result.error}` },
                ],
                isError: true,
            };
        }

        return {
            content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
        };
    } catch (error) {
        return {
            content: [{ type: "text", text: `Communication Error: ${error}` }],
            isError: true,
        };
    }
});

async function run() {
    const transport = new StdioServerTransport();
    await server.connect(transport);
    console.error("Revit MCP Server running on stdio");
}

run().catch((error) => {
    console.error("Server error:", error);
    process.exit(1);
});
