using Raylib_cs;
using ImGuiNET;
using rlImGui_cs;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

/*
    Naming convensions:
    
    Constants           :   UPPERCASE_SNAKE_CASE
    Global variables    :   PascalCase
    Local variables     :   camelCase
*/

namespace Simple_Graphing_Calculator
{
    struct Function(int ID, string func, Vector4 colour, FunctionTypes funcType)
    {
        public int ID = ID;
        public string func = func;
        public Vector4 colour = colour;
        public FunctionTypes funcType = funcType;
    }
    enum FunctionTypes
    {
        Y = 'y',
        X = 'x',
    }
    static class Program
    {
        const string FONT_PATH = "SpaceMono-Regular.ttf";
        const int FONT_SIZE = 117;
        static bool CustomFont = false;

        const int DEFAULT_RESOLUTION_X = 1920;
        const int DEFAULT_RESOLUTION_Y = 1080;
        const float DEFAULT_ZOOM = 100;

        static Vector2 Offset = new Vector2(DEFAULT_RESOLUTION_X / 2, DEFAULT_RESOLUTION_Y / 2);
        static int ResolutionX = DEFAULT_RESOLUTION_X;
        static int ResolutionY = DEFAULT_RESOLUTION_Y;

        static bool ShowGrid = true;
        static bool ShowAxes = true;
        static bool ShowAxesMarkers = true;

        static int SamplesPerPixel = 8; // MultiSampling
        static int MaxVerticalJumpThreshold = 5;
        static int MaxVerticalJumpThresholdNoFloorOrCeil = 400;

        static Vector3 BLACK_COLOUR = new(0, 0, 0);
        static Vector3 WHITE_COLOUR = new(1, 1, 1);
        static Vector3 DARK_GRAY_COLOUR = new(80 / 255f, 80 / 255f, 80 / 255f);
        static Vector3 LIGHT_GRAY_COLOUR = new(200 / 255f, 200 / 255f, 200 / 255f);

        static bool IsLightMode = false;
        static Vector3 BackgroundColour = BLACK_COLOUR;
        static Vector3 AxesColour = LIGHT_GRAY_COLOUR;
        static Vector3 GridColour = DARK_GRAY_COLOUR ;

        public static List<Function> functions = [];

        public static void Main()
        {
            // Init
            Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
            Raylib.InitWindow(DEFAULT_RESOLUTION_X, DEFAULT_RESOLUTION_Y, "Simple Graphing Calculator");
            Raylib.SetTargetFPS(144);
            rlImGui.Setup();

            // Font
            ImGuiIOPtr io = ImGui.GetIO();
            if (File.Exists(FONT_PATH))
            {
                io.Fonts.Clear();
                io.Fonts.AddFontFromFileTTF(FONT_PATH, FONT_SIZE, null, ImGui.GetIO().Fonts.GetGlyphRangesGreek());
                io.Fonts.Build();
                rlImGui.ReloadFonts();
                ImGui.GetIO().FontGlobalScale = 0;
                CustomFont = true;
            }

            // Camera
            var targetDefault = new Vector2(0, 0);
            var view = new Camera2D(Offset, targetDefault, 0, DEFAULT_ZOOM);
            var CAMERA_DEFAULT_POSITION = new Vector2(60, 60);

            // Functions
            var FUNCTIONS_DEFAULT_POSITION = new Vector2(60, 200);

            // Settings
            var UTILS_DEFAULT_POSITION = new Vector2(1200, 800);
            var isMovingImGui = false;

            // Constants and functions
            var CONSTANTS_AND_FUNCTIONS_DEFAULT_POSITION = new Vector2(1645, 615);

            // Main Loop
            while (!Raylib.WindowShouldClose())
            {
                ResolutionX = Raylib.GetRenderWidth();
                ResolutionY = Raylib.GetRenderHeight();
                view.Offset = new Vector2(ResolutionX / 2, ResolutionY / 2);

                // Input
                Input(ref view, ref isMovingImGui);

                // Draw
                Raylib.BeginDrawing();
                    Raylib.ClearBackground(ConvertColour(BackgroundColour));
                    DrawAxes(ref view);
                    EditFunctions();
                    DrawFunctions(view);
                    DrawMousePosition(ref view);

                    // ImGui
                    rlImGui.Begin();

                        // Camera
                        ImGui.SetNextWindowPos(CAMERA_DEFAULT_POSITION, ImGuiCond.FirstUseEver);
                        CameraWindow(ref view);

                        // Functions
                        ImGui.SetNextWindowPos(FUNCTIONS_DEFAULT_POSITION, ImGuiCond.FirstUseEver);
                        FunctionsWindow();

                        // Settings
                        ImGui.SetNextWindowPos(UTILS_DEFAULT_POSITION, ImGuiCond.FirstUseEver);
                        UtilsWindow();

                        ImGui.SetNextWindowPos(CONSTANTS_AND_FUNCTIONS_DEFAULT_POSITION, ImGuiCond.FirstUseEver);
                        KnownConstantsWindow();

                    rlImGui.End();
                Raylib.EndDrawing();
            }

            Raylib.CloseWindow();
        }
        static void Input(ref Camera2D view, ref bool isMovingImGui)
        {
            // Zoom
            const float ZOOM_INCREMENT = 2.5f;
            view.Zoom += Raylib.GetMouseWheelMove() * ZOOM_INCREMENT;
            if (view.Zoom < 10) view.Zoom = 10;

            // Move
            if (Raylib.IsMouseButtonDown(MouseButton.Left) 
             || Raylib.IsMouseButtonDown(MouseButton.Right) 
             || Raylib.IsMouseButtonDown(MouseButton.Middle))
            {
                if (ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow)) isMovingImGui = true;
                if (!isMovingImGui 
                 || Raylib.IsMouseButtonDown(MouseButton.Right) 
                 || Raylib.IsMouseButtonDown(MouseButton.Middle)) 
                {
                    Vector2 mouseDelta = Raylib.GetMouseDelta() * (-1 / view.Zoom);
                    view.Target += mouseDelta;
                }
            }
            if (Raylib.IsMouseButtonUp(MouseButton.Left)) isMovingImGui = false;
            if (Raylib.IsKeyPressed(KeyboardKey.Enter)) AddFunction();
        }
        static void DrawAxes(ref Camera2D view) 
        {
            // Axes position
            var axes = Raylib.GetWorldToScreen2D(new Vector2(0, 0), view);

            // Grid
            var bottomRight = Raylib.GetScreenToWorld2D(new Vector2(ResolutionX, ResolutionY), view);
            var topLeft = Raylib.GetScreenToWorld2D(new Vector2(0, 0), view);
            var nOfLines = bottomRight - topLeft;

            // Colours
            var gridColour = ConvertColour(GridColour);
            var axesColour = ConvertColour(AxesColour);

            // Vertical
            int spacingX = (int)(ResolutionX / nOfLines.X);
            int gridOffsetX = (int)axes.X % spacingX;
            for (int i = 0; i < nOfLines.X + 1; i++)
            {
                int x = i * spacingX + gridOffsetX;
                if (ShowGrid) Raylib.DrawLine(x, 0, x, ResolutionX, gridColour);

                // Numbers
                if (view.Zoom < 75) continue;
                string positionMarker = Math.Round(Raylib.GetScreenToWorld2D(new Vector2(x, 0), view).X + .1).ToString();
                if (ShowAxesMarkers)
                {
                    if (positionMarker == "0") continue;
                    int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).X;
                    Raylib.DrawText(positionMarker, x - markerOffset / 2 - 1, (int)(axes.Y + 5), 20, axesColour);
                }
            }

            // Horizontal
            int spacingY = (int)(ResolutionY / nOfLines.Y);
            int gridOffsetY = (int)axes.Y % spacingY;
            for (int i = 0; i < nOfLines.Y + 1; i++)
            {
                int y = i * spacingY + gridOffsetY;
                if(ShowGrid) Raylib.DrawLine(0, y, ResolutionX, y, gridColour);

                // Numbers
                if (view.Zoom < 75) continue;
                string positionMarker = Convert.ToString(Math.Round(Raylib.GetScreenToWorld2D(new Vector2(0, y), view).Y + .1) * -1);
                if (ShowAxesMarkers)
                {
                    if (positionMarker == "-0") continue;
                    int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).Y;
                    Raylib.DrawText(positionMarker, (int)(axes.X + 5), y - markerOffset / 2 + 1, 20, axesColour);
                }
            }

            // Axes
            if (!ShowAxes) return;
            Raylib.DrawLine((int)axes.X, 0, (int)axes.X, ResolutionY, axesColour);
            Raylib.DrawLine(0, (int)axes.Y, ResolutionX, (int)axes.Y, axesColour);
        }
        static void DrawMousePosition(ref Camera2D view)
        {
            var screenPos = Raylib.GetMousePosition();
            var worldPos = Raylib.GetScreenToWorld2D(screenPos, view);

            string coords = $"{Math.Round(worldPos.X, 1)}, {-Math.Round(worldPos.Y, 1)}";
            int textWidth = Raylib.MeasureText(coords, 30);
            int x = ResolutionX - 5 - textWidth;

            Raylib.DrawText(coords, x, ResolutionY - 30, 30, ConvertColour(AxesColour));
        }
        static void CameraWindow(ref Camera2D view)
        {
            ImGui.Begin("Viewport", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
                view.Target.Y *= -1;
                ImGui.DragFloat2("Position", ref view.Target, .1f);
                view.Target.Y *= -1;
                ImGui.DragFloat("Zoom", ref view.Zoom, view.Zoom / 1000, 10);
                if (ImGui.Button("Reset Zoom")) view.Zoom = 100;
                ImGui.SameLine();
                if (ImGui.Button("Reset Location")) view.Target = new Vector2(0, 0);
            ImGui.End();
        }
        static void FunctionsWindow()
        {
            ImGui.Begin("Functions", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
                for (int i = 0; i < functions.Count; i++)
                {
                    var currentFunc = functions[i];
                    ImGui.PushID(i);
                    ImGui.ColorEdit4("", ref currentFunc.colour, ImGuiColorEditFlags.NoInputs);
                    ImGui.PopID();
                    ImGui.SameLine();
                    if (ImGui.Button(char.ToUpper((char)currentFunc.funcType) + currentFunc.ID.ToString()))
                        currentFunc.funcType = currentFunc.funcType == FunctionTypes.X ? FunctionTypes.Y : FunctionTypes.X;
                    ImGui.SameLine();
                    ImGui.Text("=");
                    ImGui.SameLine();
                    ImGui.PushID(i);
                    ImGui.InputText($"id: {currentFunc.ID}", ref currentFunc.func, 128);
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
                if (ImGui.Button("Add Function")) AddFunction();
            ImGui.End();
        }
        static void DrawFunctions(Camera2D View)
        {
            // Raylib.DrawLine is not thread safe
            var lines = new ConcurrentBag<(Vector2 Start, Vector2 End, Color Color)>();

            Parallel.ForEach(functions, func =>
            {
                var tokens = Calculator.Tokenise(func.func);
                if (tokens.Contains(new Operator(Operators.ERR))) return;
                var parser = new Parser(tokens);
                var parsedFunction = parser.parse();

                var color = new Color(
                    (int)(Math.Clamp(func.colour.X, 0, 1) * 255), 
                    (int)(Math.Clamp(func.colour.Y, 0, 1) * 255), 
                    (int)(Math.Clamp(func.colour.Z, 0, 1) * 255),
                    (int)(Math.Clamp(func.colour.W, 0, 1) * 255));

                var calculator = new Evaluator();
                calculator.id = func.ID;
                var previousPosition = new Vector2();
                bool firstPoint = true;
                bool isYFunc = func.funcType == FunctionTypes.Y;
                int resolution = isYFunc ? ResolutionX : ResolutionY;
                for (int i = 0; i < resolution * SamplesPerPixel; i++)
                {
                    double coord;
                    if (isYFunc) coord = Raylib.GetScreenToWorld2D(new Vector2(i / SamplesPerPixel, 0), View).X;
                    else coord = - Raylib.GetScreenToWorld2D(new Vector2(0, i / SamplesPerPixel), View).Y;

                    calculator.Reset();
                    calculator.coord = coord;
                    double realFunc = calculator.Interpret(parsedFunction);

                    if (calculator.error || double.IsInfinity(realFunc)) 
                    {
                        firstPoint = true;
                        continue;
                    }
                    Vector2 finalScreenPos = new(i / SamplesPerPixel, i / SamplesPerPixel);
                    if (isYFunc) finalScreenPos.Y = Raylib.GetWorldToScreen2D(new Vector2(0, -(float)realFunc), View).Y;
                    else finalScreenPos.X = Raylib.GetWorldToScreen2D(new Vector2((float)realFunc), View).X;

                    if (firstPoint) previousPosition = finalScreenPos;

                    lines.Add((previousPosition, finalScreenPos, color));
                    previousPosition = finalScreenPos;
                    firstPoint = false;
                }
            });

            foreach (var line in lines) Raylib.DrawLineV(line.Start, line.End, line.Color);
        }
        static void EditFunctions()
        {
            for (int i = 0; i < functions.Count; i++)
            {
                Function currentFunction = functions[i]; 
                // PI
                currentFunction.func = Regex.Replace(functions[i].func, @"pi", "π", RegexOptions.IgnoreCase);
                // Replace x<->y
                string correctInput = currentFunction.funcType == FunctionTypes.X ? "y" : "x";
                currentFunction.func = Regex.Replace(currentFunction.func, ((char)currentFunction.funcType).ToString(), correctInput, RegexOptions.IgnoreCase);

                if (functions[i].func == currentFunction.func) continue;
                functions[i] = currentFunction;
            }
        }
        static void UtilsWindow()
        {
            const float SNAP_INTERVAL = 0.5f;
            float fontScale = ImGui.GetIO().FontGlobalScale * (CustomFont ? 6 : 1);
            if (fontScale == 0) fontScale = 1;
            ImGui.Begin("Settings", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
            // Colour
                if (ImGui.Button(IsLightMode ? "Dark Mode" : "Light Mode")) 
                {
                    IsLightMode = !IsLightMode;
                    BackgroundColour = IsLightMode ? WHITE_COLOUR : BLACK_COLOUR;
                    AxesColour = IsLightMode ? DARK_GRAY_COLOUR : LIGHT_GRAY_COLOUR;
                    GridColour = IsLightMode ? LIGHT_GRAY_COLOUR : DARK_GRAY_COLOUR;
                }
                ImGui.SameLine();
                ImGui.ColorEdit3("Background Colour", ref BackgroundColour, ImGuiColorEditFlags.NoInputs);
                ImGui.SameLine();
                ImGui.ColorEdit3("Axes Colour", ref AxesColour, ImGuiColorEditFlags.NoInputs);
                ImGui.SameLine();
                ImGui.ColorEdit3("Grid Colour", ref GridColour, ImGuiColorEditFlags.NoInputs);
            // Scale
                ImGui.InputFloat("Ui scale", ref fontScale, SNAP_INTERVAL);
                fontScale = fontScale < .5 ? .5f : fontScale;
            // Grid
                if (ImGui.Button("Toogle Grid")) ShowGrid ^= true;
                ImGui.SameLine();
                if (ImGui.Button("Toogle Axes")) ShowAxes ^= true;
                ImGui.SameLine();
                if (ImGui.Button("Toogle Axes Markers")) ShowAxesMarkers ^= true;
            // Misc settings
                ImGui.InputInt("Samples Per Pixel", ref SamplesPerPixel);
                ImGui.InputInt("Max Vertical Jump Threshold Floor and Ceil", ref MaxVerticalJumpThreshold);
                ImGui.InputInt("Max Vertical Jump Threshold", ref MaxVerticalJumpThresholdNoFloorOrCeil);
            ImGui.End();
            ImGui.GetIO().FontGlobalScale = fontScale / (CustomFont ? 6 : 1);
        }
        static void KnownConstantsWindow()
        {
            ImGui.Begin("Constants and functions", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize);
                ImGui.Text("Constants:");
                ImGui.SameLine();
                // Whitespace to make window autosize properly
                ImGui.Text("\t \t \t ");
                ImGui.Text("π, e");
                ImGui.Text("Functions:");
                ImGui.Text("ln, log\nsin, cos, tan\nceil, floor");
            ImGui.End();
        }
        static void AddFunction()
        {
            var newFunc = new Function
            {
                func = "",
                colour = IsLightMode ? new Vector4(0, 0, 0, 1) : new Vector4(1, 1, 1, 1)
            };
            for (int i = 0; i <= functions.Count; i++)
            {
                if (!functions.Any(f => f.ID == i))
                {
                    newFunc.ID = i;
                    break;
                }
            }
            newFunc.funcType = FunctionTypes.Y;
            functions.Add(newFunc);
        }
        // Colour Conversion (vec4)
        static Color ConvertColour(Vector4 colour) => new(
            (int)(Math.Clamp(colour.X, 0, 1) * 255), 
            (int)(Math.Clamp(colour.Y, 0, 1) * 255), 
            (int)(Math.Clamp(colour.Z, 0, 1) * 255),
            (int)(Math.Clamp(colour.W, 0, 1) * 255));
        // Colour Conversion (vec3)
        static Color ConvertColour(Vector3 colour) => new(
            (int)(Math.Clamp(colour.X, 0, 1) * 255), 
            (int)(Math.Clamp(colour.Y, 0, 1) * 255), 
            (int)(Math.Clamp(colour.Z, 0, 1) * 255), 255);
    }
}
