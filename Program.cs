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

namespace Simple_Graphing_Calculator
{
    class Program
    {
        const int DEFAULT_RESOLUTION_X = 1920;
        const int DEFAULT_RESOLUTION_Y = 1080;
        const float DEFAULT_ZOOM = 100;

        static Vector2 Offset = new Vector2(DEFAULT_RESOLUTION_X / 2, DEFAULT_RESOLUTION_Y / 2);
        static int ResolutionX = DEFAULT_RESOLUTION_X;
        static int ResolutionY = DEFAULT_RESOLUTION_Y;

        public static void Main()
        {
            // Init
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(DEFAULT_RESOLUTION_X, DEFAULT_RESOLUTION_Y, "Simple Graphing Calculator");
            Raylib.SetTargetFPS(144);
            rlImGui.Setup();

            // Camera
            Vector2 targetDefault = new Vector2(0, 0);
            Camera2D view = new Camera2D(Offset, targetDefault, 0, DEFAULT_ZOOM);
            Vector2 cameraDefaultPosition = new Vector2(60, 60);

            // Functions
            Vector2 functionsDefaultPosition = new Vector2(60, 160);
            var functions = new List<(string, Vector3)>();

            // Main Loop
            while (!Raylib.WindowShouldClose())
            {
                ResolutionX = Raylib.GetRenderWidth();
                ResolutionY = Raylib.GetRenderHeight();
                Offset = new Vector2(ResolutionX / 2, ResolutionY / 2);

                // Input
                Input(ref view);

                // Draw
                Raylib.BeginDrawing();
                    Raylib.ClearBackground(Color.Black);
                    DrawAxes(ref view);
                    DrawFunctions(ref view, functions);
                    DrawMousePosition(ref view);

                    // ImGui
                    rlImGui.Begin();

                        // Camera
                        ImGui.SetNextWindowPos(cameraDefaultPosition, ImGuiCond.FirstUseEver);
                        CameraWindow(ref view);

                        // Functions
                        ImGui.SetNextWindowPos(functionsDefaultPosition, ImGuiCond.FirstUseEver);
                        FunctionsWindow(ref functions);

                        // Settings
                        UtilsWindow();

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
            Vector2 bottomRight = Raylib.GetScreenToWorld2D(new Vector2(ResolutionX, ResolutionY), view);
            Vector2 topLeft = Raylib.GetScreenToWorld2D(new Vector2(0, 0), view);
            Vector2 nOfLines = bottomRight - topLeft;

            // Vertical
            int spacingX = (int)(ResolutionX / nOfLines.X);
            int gridOffsetX = (int)axes.X % spacingX;
            for (int i = 0; i < nOfLines.X + 1; i++)
            {
                int x = i * spacingX + gridOffsetX;
                Raylib.DrawLine(x, 0, x, ResolutionX, Color.DarkGray);

                // Numbers
                if (view.Zoom < 75) continue;
                string positionMarker = Math.Round(Raylib.GetScreenToWorld2D(new Vector2(x, 0), view).X + .1).ToString();
                if (positionMarker == "0") continue;
                int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).X;
                Raylib.DrawText(positionMarker, x - markerOffset / 2 - 1, (int)(axes.Y + 5), 20, Color.LightGray);
            }

            // Horizontal
            int spacingY = (int)(ResolutionY / nOfLines.Y);
            int gridOffsetY = (int)axes.Y % spacingY;
            for (int i = 0; i < nOfLines.Y + 1; i++)
            {
                int y = i * spacingY + gridOffsetY;
                Raylib.DrawLine(0, y, ResolutionX, y, Color.DarkGray);

                // Numbers
                if (view.Zoom < 75) continue;
                string positionMarker = Convert.ToString(Math.Round(Raylib.GetScreenToWorld2D(new Vector2(0, y), view).Y + .1) * -1);
                if (positionMarker == "-0") continue;
                int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).Y;
                Raylib.DrawText(positionMarker, (int)(axes.X + 5), y - markerOffset / 2 + 1, 20, Color.LightGray);
            }

            // Axes
            Raylib.DrawLine((int)axes.X, 0, (int)axes.X, ResolutionY, Color.LightGray);
            Raylib.DrawLine(0, (int)axes.Y, ResolutionX, (int)axes.Y, Color.LightGray);
        }
        static void DrawMousePosition(ref Camera2D view)
        {
            Vector2 screenPos = Raylib.GetMousePosition();
            Vector2 worldPos = Raylib.GetScreenToWorld2D(screenPos, view);

            string coords = $"{Math.Round(worldPos.X, 1)}, {-Math.Round(worldPos.Y, 1)}";
            int textWidth = Raylib.MeasureText(coords, 30);
            int x = ResolutionX - 5 - textWidth;

            Raylib.DrawText(coords, x, ResolutionY - 30, 30, Color.LightGray);
        }
        static void CameraWindow(ref Camera2D view)
        {
            ImGui.Begin("Viewport", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.InputFloat2("Position", ref view.Target);
                ImGui.SliderFloat("Zoom", ref view.Zoom, 10, 200);
                if (ImGui.Button("Reset Zoom")) view.Zoom = 100;
                ImGui.SameLine();
                if (ImGui.Button("Reset Location")) view.Target = new Vector2(0, 0);
            ImGui.End();
        }
        static void FunctionsWindow(ref List<(string func, Vector3 colour)> functions)
        {
            ImGui.Begin("Functions", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
                for (int i = 0; i < functions.Count; i++)
                {
                    var currentFunc = functions[i];
                    ImGui.PushID(i);
                    ImGui.ColorEdit3("", ref currentFunc.colour, ImGuiColorEditFlags.NoInputs);
                    ImGui.PopID();
                    ImGui.SameLine();
                    ImGui.PushID(i);
                    ImGui.InputText("= Y", ref currentFunc.func, 128);
                    ImGui.PopID();
                    functions[i] = currentFunc;
                    ImGui.SameLine();
                    ImGui.PushID(i);
                    if (ImGui.Button("X"))
                    {
                        functions.RemoveAt(i);
                        i--;
                    }
                    ImGui.PopID();
                }
                if (ImGui.Button("Add Function")) functions.Add(("", new Vector3(255, 255, 255)));
            ImGui.End();
        }
        static void DrawFunctions(ref Camera2D View, List<(string func, Vector3 colour)> functions)
        {
            foreach (var func in functions)
            {
                List<IToken> tokens = Calculator.Tokenise(func.func);
                Parser parser = new Parser(tokens);
                IExpression parsedFunction = parser.parseArithmetic();
                for (int i = 0; i < ResolutionX; i++)
                {
                    double x = Raylib.GetScreenToWorld2D(new Vector2(i, 0), View).X;

                    Evaluator.Reset();
                    Evaluator.x = x;
                    double realY = - Evaluator.Interpret(parsedFunction);

                    if (Evaluator.error) continue;
                    double y = Raylib.GetWorldToScreen2D(new Vector2(0, (float)realY), View).Y;

                    Color color = new Color(
                        func.colour.X * 255, 
                        func.colour.Y * 255, 
                        func.colour.Z * 255);
                    Raylib.DrawPixel(i, (int)y, color);
                }
            }
        }
        static void UtilsWindow()
        {
            ImGui.Begin("Settings", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.SliderFloat("Ui scale", ref ImGui.GetIO().FontGlobalScale, .5f, 5);
            ImGui.End();
        }
    }
}
