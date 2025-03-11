using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;

/*
    Naming convensions:
    
    Constants           :   UPPERCASE_SNAKE_CASE
    Global variables    :   PascalCase
    Local variables     :   camelCase
*/

namespace Simple_Graphing_Calculator;
class Program
{
    const int RESOLUTION_X = 1920;
    const int RESOLUTION_Y = 1080;
    const float DEFAULT_ZOOM = 1080 / 10;

    static Vector2 Offset = new Vector2(RESOLUTION_X / 2, RESOLUTION_Y / 2);

    public static void Main()
    {
        // Init
        Raylib.InitWindow(RESOLUTION_X, RESOLUTION_Y, "Simple Graphing Calculator");
        Raylib.SetTargetFPS(144);
        rlImGui.Setup();

        // Camera
        Vector2 targetDefault = new Vector2(0, 0);
        Camera2D view = new Camera2D(Offset, targetDefault, 0, DEFAULT_ZOOM);
        Vector2 cameraDefaultPosition = new Vector2(60, 60);
        Vector2 cameraDefaultSize = new Vector2(300, 80);

        // Main Loop
        while (!Raylib.WindowShouldClose())
        {
            // Input
            Input(ref view);

            // Draw
            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                DrawAxes(ref view);

                // ImGui
                rlImGui.Begin();

                    // Camera
                    ImGui.SetNextWindowPos(cameraDefaultPosition, ImGuiCond.FirstUseEver);
                    ImGui.SetNextWindowSize(cameraDefaultSize, ImGuiCond.FirstUseEver);
                    ImGui.Begin("Camera", ImGuiWindowFlags.NoSavedSettings);
                        ImGui.InputFloat2("Position", ref view.Target);
                        ImGui.SliderFloat("Zoom", ref view.Zoom, 10, 200);
                    ImGui.End();

                    // Functions
                    ImGui.SetNextWindowPos(new Vector2(60, 160), ImGuiCond.FirstUseEver);
                    ImGui.Begin("Functions");
                        ImGui.Text("This is some text");
                    ImGui.End();
                rlImGui.End();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
    static void Input(ref Camera2D view)
    {
        // Zoom
        const float ZOOM_INCREMENT = 2.5f;
        view.Zoom += Raylib.GetMouseWheelMove() * ZOOM_INCREMENT;
        if (view.Zoom < 10) view.Zoom = 10;

        // Move
        if (Raylib.IsMouseButtonDown(MouseButton.Right))
        {
            Vector2 mouseDelta = Raylib.GetMouseDelta() * (-1 / view.Zoom);
            view.Target += mouseDelta;
        }
    }
    static void DrawAxes(ref Camera2D view) 
    {
        // Axes position
        Vector2 axes = Raylib.GetWorldToScreen2D(new Vector2(0, 0), view);

        // Grid
        Vector2 bottomRight = Raylib.GetScreenToWorld2D(new Vector2(RESOLUTION_X, RESOLUTION_Y), view);
        Vector2 topLeft = Raylib.GetScreenToWorld2D(new Vector2(0, 0), view);
        Vector2 nOfLines = new Vector2(bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

        // Vertical
        int spacingX = (int)(RESOLUTION_X / nOfLines.X);
        int gridOffsetX = (int)axes.X % spacingX;
        for (int i = 0; i < nOfLines.X; i++)
        {
            int x = i * spacingX + gridOffsetX;
            Raylib.DrawLine(x, 0, x, RESOLUTION_Y, Color.DarkGray);

            // Numbers
            if (view.Zoom < 75) continue;
            string positionMarker = Math.Round(Raylib.GetScreenToWorld2D(new Vector2(x, 0), view).X + .1).ToString();
            if (positionMarker == "0") continue;
            int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).X;
            Raylib.DrawText(positionMarker, x - markerOffset / 2 - 1, (int)(axes.Y + 5), 20, Color.LightGray);
        }

        // Horizontal
        int spacingY = (int)(RESOLUTION_Y / nOfLines.Y);
        int gridOffsetY = (int)axes.Y % spacingY;
        for (int i = 0; i < nOfLines.Y; i++)
        {
            int y = i * spacingY + gridOffsetY;
            Raylib.DrawLine(0, y, RESOLUTION_X, y, Color.DarkGray);

            // Numbers
            if (view.Zoom < 75) continue;
            string positionMarker = Math.Round(Raylib.GetScreenToWorld2D(new Vector2(0, y), view).Y + .1).ToString();
            if (positionMarker == "0") continue;
            int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).Y;
            Raylib.DrawText(positionMarker, (int)(axes.X + 5), y - markerOffset / 2 + 1, 20, Color.LightGray);
        }

        // Axes
        Raylib.DrawLine((int)axes.X, 0, (int)axes.X, RESOLUTION_Y, Color.LightGray);
        Raylib.DrawLine(0, (int)axes.Y, RESOLUTION_X, (int)axes.Y, Color.LightGray);
    }
}