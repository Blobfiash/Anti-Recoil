using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle,
        string lpClassName,
        string lpWindowName,
        uint dwStyle,
        int x, int y,
        int nWidth, int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInstance,
        IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern bool SetPixel(IntPtr hdc, int x, int y, uint crColor);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint WS_POPUP = 0x80000000;
    private const uint WS_VISIBLE = 0x10000000;
    private const uint WS_EX_TOPMOST = 0x00000008;

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private static IntPtr CrosshairWindow;
    private static int dotSize = 20;
    private static int screenWidth;
    private static int screenHeight;

    private static bool isMovingEnabled = false;
    private static bool isDotVisible = false;
    private static int moveSpeed = 6;

    private static string currentProfile = "default";
    private static Dictionary<string, UserProfile> userProfiles = new Dictionary<string, UserProfile>();

    private static string[] crosshairShapes = { "Circle", "Square", "Cross" };
    private static int selectedShapeIndex = 0;

    static void Main()
    {
        screenWidth = GetSystemMetrics(SM_CXSCREEN);
        screenHeight = GetSystemMetrics(SM_CYSCREEN);

        LoadProfiles();

        var monitorThread = new Thread(MonitorMouse);
        monitorThread.Start();

        ShowMainMenu();

        while (true)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                break;

            Thread.Sleep(100);
        }

        SaveProfiles();
    }

    private static void LoadProfiles()
    {
        if (File.Exists("profiles.txt"))
        {
            var lines = File.ReadAllLines("profiles.txt");
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 4)
                {
                    var profile = new UserProfile
                    {
                        DotSize = int.Parse(parts[0]),
                        IsMovingEnabled = bool.Parse(parts[1]),
                        MoveSpeed = int.Parse(parts[2]),
                        Shape = parts[3]
                    };
                    userProfiles[parts[3]] = profile;
                }
            }
        }
    }

    private static void SaveProfiles()
    {
        using (var writer = new StreamWriter("profiles.txt"))
        {
            foreach (var kvp in userProfiles)
            {
                var profile = kvp.Value;
                writer.WriteLine($"{profile.DotSize},{profile.IsMovingEnabled},{profile.MoveSpeed},{kvp.Key}");
            }
        }
    }

    private static void ShowMainMenu()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Anti Recoil");
            Console.WriteLine("1. Main");
            Console.Write("Select an option: ");

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.D1)
            {
                ShowMainPart();
            }
            else if (key == ConsoleKey.F5)
            {
                break;
            }
        }
    }

    private static void ShowMainPart()
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Main Menu:");
            Console.WriteLine("1. Change Crosshair Size (Current: " + dotSize + ")");
            Console.WriteLine("2. Toggle Anti Recoil (Current: " + (isMovingEnabled ? "Enabled" : "Disabled") + ")");
            Console.WriteLine("5. Toggle Crosshair (Current: " + (isDotVisible ? "Visible" : "Hidden") + ")");
            Console.WriteLine("6. Change Recoil Speed (Current: " + moveSpeed + ")");
            Console.WriteLine("7. Change Crosshair Shape (Current: " + crosshairShapes[selectedShapeIndex] + ")");
            Console.WriteLine("8. Save Current Profile");
            Console.WriteLine("9. Load User Profile");
            Console.WriteLine("F5. Back to Main Menu");
            Console.Write("Select an option: ");

            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.D1)
            {
                Console.Write("Enter Crosshair size (current: " + dotSize + "): ");
                if (int.TryParse(Console.ReadLine(), out int newDotSize) && newDotSize > 0)
                {
                    dotSize = newDotSize;
                }
            }
            else if (key == ConsoleKey.D2)
            {
                isMovingEnabled = !isMovingEnabled;
            }
            else if (key == ConsoleKey.D5)
            {
                ToggleCrosshair();
            }
            else if (key == ConsoleKey.D6)
            {
                Console.Write("Enter movement speed (current: " + moveSpeed + "): ");
                if (int.TryParse(Console.ReadLine(), out int newSpeed) && newSpeed > 0)
                {
                    moveSpeed = newSpeed;
                }
            }
            else if (key == ConsoleKey.D7)
            {
                CrosshairShape();
            }
            else if (key == ConsoleKey.D8)
            {
                SaveCurrentProfile();
            }
            else if (key == ConsoleKey.D9)
            {
                LoadUserProfile();
            }
            else if (key == ConsoleKey.F5)
            {
                break;
            }
        }
    }

    private static void CrosshairShape()
    {
        Console.Clear();
        Console.WriteLine("Select Crosshair Shape:");
        for (int i = 0; i < crosshairShapes.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {crosshairShapes[i]}");
        }
        Console.Write("Select an option: ");
        var key = Console.ReadKey(true).Key;
        if (key >= ConsoleKey.D1 && key <= ConsoleKey.D3)
        {
            selectedShapeIndex = (int)key - (int)ConsoleKey.D1;
        }
    }

    private static void SaveCurrentProfile()
    {
        Console.Write("Enter a profile name to save: ");
        string profileName = Console.ReadLine();
        if (!userProfiles.ContainsKey(profileName))
        {
            userProfiles[profileName] = new UserProfile
            {
                DotSize = dotSize,
                IsMovingEnabled = isMovingEnabled,
                MoveSpeed = moveSpeed,
                Shape = crosshairShapes[selectedShapeIndex]
            };
            Console.WriteLine("Profile saved");
        }
        else
        {
            Console.WriteLine("Already exists Choose a different name");
        }
        Console.ReadKey();
    }

    private static void LoadUserProfile()
    {
        Console.Clear();
        foreach (var profile in userProfiles.Keys)
        {
            Console.WriteLine(profile);
        }
        Console.Write("Select a profile: ");
        string profileName = Console.ReadLine();
        if (userProfiles.TryGetValue(profileName, out UserProfile userProfile))
        {
            dotSize = userProfile.DotSize;
            isMovingEnabled = userProfile.IsMovingEnabled;
            moveSpeed = userProfile.MoveSpeed;
            selectedShapeIndex = Array.IndexOf(crosshairShapes, userProfile.Shape);
            Console.WriteLine("Profile loaded");
        }
        else
        {
            Console.WriteLine("Profile not found");
        }
        Console.ReadKey();
    }

    private static void ToggleCrosshair()
    {
        if (isDotVisible)
        {
            RemoveCrosshair();
            isDotVisible = false;
        }
        else
        {
            CreateCrosshair();
            isDotVisible = true;
        }
    }

    private static void CreateCrosshair()
    {
        int x = (screenWidth / 2) - (dotSize / 2);
        int y = (screenHeight / 2) - (dotSize / 2);

        CrosshairWindow = CreateWindowEx(WS_EX_TOPMOST, "Static", "", WS_POPUP | WS_VISIBLE,
            x, y, dotSize, dotSize, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        IntPtr hdc = GetDC(CrosshairWindow);

        switch (crosshairShapes[selectedShapeIndex])
        {
            case "Circle":
                Circle(hdc, dotSize / 2);
                break;
            case "Square":
                Square(hdc);
                break;
            case "Cross":
                Cross(hdc);
                break;
        }

        ReleaseDC(CrosshairWindow, hdc);
    }

    private static void Circle(IntPtr hdc, int radius)
    {
        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j <= radius * radius)
                {
                    SetPixel(hdc, radius + i, radius + j, 0x00FF0000);
                }
            }
        }
    }

    private static void Square(IntPtr hdc)
    {
        for (int i = 0; i < dotSize; i++)
        {
            for (int j = 0; j < dotSize; j++)
            {
                SetPixel(hdc, i, j, 0x00FF0000);
            }
        }
    }

    private static void Cross(IntPtr hdc)
    {
        int center = dotSize / 2;
        for (int i = 0; i < dotSize; i++)
        {
            SetPixel(hdc, center, i, 0x00FF0000);
            SetPixel(hdc, i, center, 0x00FF0000);
        }
    }

    private static void RemoveCrosshair()
    {
        if (CrosshairWindow != IntPtr.Zero)
        {
            DestroyWindow(CrosshairWindow);
            CrosshairWindow = IntPtr.Zero;
        }
    }


    //Main part of the anti recoil
    private static void MoveMouseDown()
    {
        GetCursorPos(out POINT currentMousePos);
        mouse_event(MOUSEEVENTF_MOVE, 0, (uint)moveSpeed, 0, IntPtr.Zero);
    }
    private static void MonitorMouse()
    {
        while (true)
        {
            if (isMovingEnabled)
            {
                bool LeftButtonDown = (GetAsyncKeyState(0x01) & 0x8000) != 0;

                if (LeftButtonDown)
                {
                    MoveMouseDown();
                }
            }

            Thread.Sleep(50);
        }
    }
}
class UserProfile
{
    public int DotSize { get; set; }
    public bool IsMovingEnabled { get; set; }
    public int MoveSpeed { get; set; }
    public string Shape { get; set; }
}
