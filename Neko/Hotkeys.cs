
using System.Collections.Generic;
using ImGuiNET;

namespace Neko;

public enum Key
{
    LBUTTON = 1, RBUTTON = 2, MBUTTON = 4,

    SPACE = 32,
    PRIOR = 33, NEXT = 34, END = 35, HOME = 36,
    LEFT = 37, UP = 38, RIGHT = 39, DOWN = 40,

    KEY0 = 48, KEY1 = 49, KEY2 = 50, KEY3 = 51, KEY4 = 52, KEY5 = 53, KEY6 = 54, KEY7 = 55, KEY8 = 56, KEY9 = 57,

    A = 65, B = 66, C = 67, D = 68, E = 69, F = 70, G = 71, H = 72, I = 73, J = 74, K = 75, L = 76, M = 77,
    N = 78, O = 79, P = 80, Q = 81, R = 82, S = 83, T = 84, U = 85, V = 86, W = 87, X = 88, Y = 89, Z = 90,

    F1 = 112, F2 = 113, F3 = 114, F4 = 115, F5 = 116, F6 = 117,
    F7 = 118, F8 = 119, F9 = 120, F10 = 121, F11 = 122, F12 = 123,
}

public enum HotkeyCondition
{
    AlwaysOn,
    AlwaysOff,
    OnMouseOver,
}

public class Hotkey
{
    public HotkeyCondition Condition;
    public Key Key;
    public string Name;

    private bool isDragging;
    private bool pressedLastCall;


    public static readonly Dictionary<Key, string> KeyNames = new() {
        {Key.LBUTTON, "Left Mouse Button"}, {Key.RBUTTON, "Right Mouse Button"}, {Key.MBUTTON, "Middle Mouse Button"},

        {Key.SPACE, "Space"},
        {Key.PRIOR, "Page Up"}, {Key.NEXT, "Page Down"}, {Key.END, "End"}, {Key.HOME, "Home"},
        {Key.LEFT, "Left Arrow"}, {Key.UP, "Up Arrow"}, {Key.RIGHT, "Right Arrow"}, {Key.DOWN, "Down Arrow"},

        {Key.KEY0, "0"}, {Key.KEY1, "1"}, {Key.KEY2, "2"}, {Key.KEY3, "3"}, {Key.KEY4, "4"},
        {Key.KEY5, "5"}, {Key.KEY6, "6"}, {Key.KEY7, "7"}, {Key.KEY8, "8"}, {Key.KEY9, "9"},

        {Key.A, "A"}, {Key.B, "B"}, {Key.C, "C"}, {Key.D, "D"}, {Key.E, "E"}, {Key.F, "F"}, {Key.G, "G"},
        {Key.H, "H"}, {Key.I, "I"}, {Key.J, "J"}, {Key.K, "K"}, {Key.L, "L"}, {Key.M, "M"}, {Key.N, "N"},
        {Key.O, "O"}, {Key.P, "P"}, {Key.Q, "Q"}, {Key.R, "R"}, {Key.S, "S"}, {Key.T, "T"}, {Key.U, "U"},
        {Key.V, "V"}, {Key.W, "W"}, {Key.X, "X"}, {Key.Y, "Y"}, {Key.Z, "Z"},

        {Key.F1, "F1"}, {Key.F2, "F2"}, {Key.F3, "F3"}, {Key.F4, "F4"},   {Key.F5, "F5"},   {Key.F6, "F6"},
        {Key.F7, "F7"}, {Key.F8, "F8"}, {Key.F9, "F9"}, {Key.F10, "F10"}, {Key.F11, "F11"}, {Key.F12, "F12"},
    };

    public static readonly Dictionary<HotkeyCondition, string> ConditionNames = new() {
        {HotkeyCondition.AlwaysOn, "Always On"},
        {HotkeyCondition.AlwaysOff, "Always Off"},
        {HotkeyCondition.OnMouseOver, "On Mouse Over"},
    };

    public Hotkey(string name)
    {
        Condition = HotkeyCondition.AlwaysOff;
        Key = Key.KEY0;
        Name = name;
    }

    public Hotkey(string name, HotkeyCondition condition, Key key)
    {
        Condition = condition;
        Key = key;
        Name = name;
    }

    public string KeyName => GetKeyName(Key);
    public static string GetKeyName(Key k) => KeyNames.TryGetValue(k, out var name) ? name : k.ToString();
    public string ConditionName => ConditionNames[Condition];

    public bool IsHeld()
    {
        if (Condition == HotkeyCondition.AlwaysOff)
            return false;
        if (Condition == HotkeyCondition.AlwaysOn)
            return KeyHeld((Dalamud.Game.ClientState.Keys.VirtualKey)Key);

        var held = KeyHeld((Dalamud.Game.ClientState.Keys.VirtualKey)Key);
        isDragging = held && ((ImGui.IsWindowHovered() && !pressedLastCall) || isDragging);
        pressedLastCall = held; // this is needed, so it wont trigger if you hold the key and move the mouse over the window later
        return isDragging;
    }

    public bool IsPressed() =>
        Condition != HotkeyCondition.AlwaysOff
            && (Condition == HotkeyCondition.AlwaysOn
            ? KeyPressed((Dalamud.Game.ClientState.Keys.VirtualKey)Key)
            : ImGui.IsWindowHovered() && KeyPressed((Dalamud.Game.ClientState.Keys.VirtualKey)Key));

    private static bool KeyHeld(Dalamud.Game.ClientState.Keys.VirtualKey key) =>
        key == Dalamud.Game.ClientState.Keys.VirtualKey.LBUTTON
            ? ImGui.IsMouseDown(ImGuiMouseButton.Left)
            : key == Dalamud.Game.ClientState.Keys.VirtualKey.RBUTTON
            ? ImGui.IsMouseDown(ImGuiMouseButton.Right)
            : key == Dalamud.Game.ClientState.Keys.VirtualKey.MBUTTON
            ? ImGui.IsMouseDown(ImGuiMouseButton.Middle)
            : Plugin.KeyState[key];

    private static bool KeyPressed(Dalamud.Game.ClientState.Keys.VirtualKey key)
    {
        if (key == Dalamud.Game.ClientState.Keys.VirtualKey.LBUTTON)
            return ImGui.IsMouseClicked(ImGuiMouseButton.Left);
        if (key == Dalamud.Game.ClientState.Keys.VirtualKey.RBUTTON)
            return ImGui.IsMouseClicked(ImGuiMouseButton.Right);
        if (key == Dalamud.Game.ClientState.Keys.VirtualKey.MBUTTON)
            return ImGui.IsMouseClicked(ImGuiMouseButton.Middle);

        if (Plugin.KeyState[key])
        {
            Plugin.KeyState[key] = false;
            return true;
        }
        return false;
    }
}
