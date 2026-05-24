using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using CraftSharp.Helpers;

namespace CraftSharp.Services.Core
{
    public class HotkeyService : IDisposable
    {
        private static HotkeyService? _instance;
        public static HotkeyService Instance => _instance ??= new HotkeyService();

        private readonly Dictionary<int, string> _registeredHotkeys = new();
        private readonly Dictionary<string, Action> _hotkeyActions = new();
        private HwndSource? _hwndSource;
        private IntPtr _hwnd = IntPtr.Zero;
        private int _nextHotkeyId = 1;

        private HotkeyService() { }

        public void HookWindow(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hwnd = helper.Handle;
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            _hwndSource?.AddHook(WndProc);
        }

        public bool RegisterHotkey(string hotkeyId, string hotkeyString, Action action)
        {
            if (string.IsNullOrEmpty(hotkeyString)) return false;

            UnregisterHotkeyById(hotkeyId);

            var (mod, vk) = ParseHotkeyString(hotkeyString);
            if (vk == 0) return false;

            int id = _nextHotkeyId++;
            bool success = Win32Helper.RegisterHotKey(_hwnd, id, mod, vk);
            if (success)
            {
                _registeredHotkeys[id] = hotkeyId;
                _hotkeyActions[hotkeyId] = action;
            }
            return success;
        }

        public void UnregisterHotkeyById(string hotkeyId)
        {
            int? idToRemove = null;
            foreach (var kvp in _registeredHotkeys)
            {
                if (kvp.Value == hotkeyId)
                {
                    Win32Helper.UnregisterHotKey(_hwnd, kvp.Key);
                    idToRemove = kvp.Key;
                    break;
                }
            }
            if (idToRemove.HasValue)
            {
                _registeredHotkeys.Remove(idToRemove.Value);
            }
            _hotkeyActions.Remove(hotkeyId);
        }

        public void UnregisterAll()
        {
            foreach (var id in _registeredHotkeys.Keys)
            {
                Win32Helper.UnregisterHotKey(_hwnd, id);
            }
            _registeredHotkeys.Clear();
        }

        public void ReRegisterAll(Dictionary<string, string> hotkeyMap)
        {
            foreach (var id in _registeredHotkeys.Keys)
            {
                Win32Helper.UnregisterHotKey(_hwnd, id);
            }
            _registeredHotkeys.Clear();

            foreach (var (hotkeyId, hotkeyString) in hotkeyMap)
            {
                if (_hotkeyActions.TryGetValue(hotkeyId, out var action))
                {
                    RegisterHotkey(hotkeyId, hotkeyString, action);
                }
            }
        }

        public static (uint mod, uint vk) ParseHotkeyString(string hotkeyString)
        {
            uint mod = 0;
            uint vk = 0;

            var parts = hotkeyString.Split('+');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
                    mod |= Win32Helper.MOD_CONTROL;
                else if (trimmed.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                    mod |= Win32Helper.MOD_SHIFT;
                else if (trimmed.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                    mod |= Win32Helper.MOD_ALT;
                else if (Enum.TryParse<Key>(trimmed, out var key))
                {
                    vk = (uint)KeyInterop.VirtualKeyFromKey(key);
                }
            }

            // 仅在有修饰键时附加 MOD_NOREPEAT
            if (mod != 0)
                mod |= Win32Helper.MOD_NOREPEAT;

            return (mod, vk);
        }

        public static Dictionary<string, string> GetDefaults()
        {
            return new Dictionary<string, string>
            {
                { "Inventory", "Ctrl+Alt+E" },
                { "Settings", "Ctrl+Alt+S" },
            };
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32Helper.WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                if (_registeredHotkeys.TryGetValue(hotkeyId, out var bizId) &&
                    _hotkeyActions.TryGetValue(bizId, out var action))
                {
                    Application.Current?.Dispatcher.Invoke(action);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (var id in _registeredHotkeys.Keys)
            {
                Win32Helper.UnregisterHotKey(_hwnd, id);
            }
            _registeredHotkeys.Clear();
            _hotkeyActions.Clear();

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
        }
    }
}
