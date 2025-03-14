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

        static bool IsLightMode = false;
        static Color BackgroundColour = Color.Black;
        static Color AxesColour = Color.LightGray;
        static Color GridColour = Color.DarkGray;

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
            Vector2 targetDefault = new Vector2(0, 0);
            Camera2D view = new Camera2D(Offset, targetDefault, 0, DEFAULT_ZOOM);
            Vector2 cameraDefaultPosition = new Vector2(60, 60);

            // Functions
            Vector2 functionsDefaultPosition = new Vector2(60, 200);

            // Settings
            Vector2 UtilsDefaultPosition = new Vector2(1200, 800);
            bool isMovingImGui = false;

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
                    Raylib.ClearBackground(BackgroundColour);
                    DrawAxes(ref view);
                    EditFunctions();
                    DrawFunctions(view);
                    DrawMousePosition(ref view);

                    // ImGui
                    rlImGui.Begin();

                        // Camera
                        ImGui.SetNextWindowPos(cameraDefaultPosition, ImGuiCond.FirstUseEver);
                        CameraWindow(ref view);

                        // Functions
                        ImGui.SetNextWindowPos(functionsDefaultPosition, ImGuiCond.FirstUseEver);
                        FunctionsWindow();

                        // Settings
                        ImGui.SetNextWindowPos(UtilsDefaultPosition, ImGuiCond.FirstUseEver);
                        UtilsWindow();

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
                if (ShowGrid) Raylib.DrawLine(x, 0, x, ResolutionX, GridColour);

                // Numbers
                if (view.Zoom < 75) continue;
                string positionMarker = Math.Round(Raylib.GetScreenToWorld2D(new Vector2(x, 0), view).X + .1).ToString();
                if (ShowAxesMarkers)
                {
                    if (positionMarker == "0") continue;
                    int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).X;
                    Raylib.DrawText(positionMarker, x - markerOffset / 2 - 1, (int)(axes.Y + 5), 20, AxesColour);
                }
            }

            // Horizontal
            int spacingY = (int)(ResolutionY / nOfLines.Y);
            int gridOffsetY = (int)axes.Y % spacingY;
            for (int i = 0; i < nOfLines.Y + 1; i++)
            {
                int y = i * spacingY + gridOffsetY;
                if(ShowGrid) Raylib.DrawLine(0, y, ResolutionX, y, GridColour);

                // Numbers
                if (view.Zoom < 75) continue;
                string positionMarker = Convert.ToString(Math.Round(Raylib.GetScreenToWorld2D(new Vector2(0, y), view).Y + .1) * -1);
                if (ShowAxesMarkers)
                {
                    if (positionMarker == "-0") continue;
                    int markerOffset = (int)Raylib.MeasureTextEx(Raylib.GetFontDefault(), positionMarker, 20, 5).Y;
                    Raylib.DrawText(positionMarker, (int)(axes.X + 5), y - markerOffset / 2 + 1, 20, AxesColour);
                }
            }

            // Axes
            if (!ShowAxes) return;
            Raylib.DrawLine((int)axes.X, 0, (int)axes.X, ResolutionY, AxesColour);
            Raylib.DrawLine(0, (int)axes.Y, ResolutionX, (int)axes.Y, AxesColour);
        }
        static void DrawMousePosition(ref Camera2D view)
        {
            Vector2 screenPos = Raylib.GetMousePosition();
            Vector2 worldPos = Raylib.GetScreenToWorld2D(screenPos, view);

            string coords = $"{Math.Round(worldPos.X, 1)}, {-Math.Round(worldPos.Y, 1)}";
            int textWidth = Raylib.MeasureText(coords, 30);
            int x = ResolutionX - 5 - textWidth;

            Raylib.DrawText(coords, x, ResolutionY - 30, 30, AxesColour);
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
                List<IToken> tokens = Calculator.Tokenise(func.func);
                if (tokens.Contains(new Operator(Operators.ERR))) return;
                Parser parser = new Parser(tokens);
                IExpression parsedFunction = parser.parse();

                Color color = new Color(
                    (int)(Math.Clamp(func.colour.X, 0, 1) * 255), 
                    (int)(Math.Clamp(func.colour.Y, 0, 1) * 255), 
                    (int)(Math.Clamp(func.colour.Z, 0, 1) * 255),
                    (int)(Math.Clamp(func.colour.W, 0, 1) * 255));

                Evaluator calculator = new Evaluator();
                calculator.id = func.ID;
                Vector2 previousPosition = new Vector2();
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
                currentFunction.func = Regex.Replace(functions[i].func, ((char)currentFunction.funcType).ToString(), correctInput, RegexOptions.IgnoreCase);

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
                    BackgroundColour = IsLightMode ? Color.White : Color.Black;
                    AxesColour = IsLightMode ? Color.DarkGray : Color.LightGray;
                    GridColour = IsLightMode ? Color.LightGray : Color.DarkGray;
                }
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
        static void AddFunction()
        {
            var newFunc = new Function();
            newFunc.func = "";
            newFunc.colour = IsLightMode ? new Vector4(0, 0, 0, 1) : new Vector4(1, 1, 1, 1);
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
    }
}
