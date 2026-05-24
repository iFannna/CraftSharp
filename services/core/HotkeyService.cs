using System;
using System.Collections.Generic;
using System.Linq;
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

        // hotkeyString → Win32 注册 ID
        private readonly Dictionary<string, int> _hotkeyRegIds = new();
        // Win32 注册 ID → hotkeyString
        private readonly Dictionary<int, string> _regIdToHotkeyString = new();
        // hotkeyString → 业务ID列表（支持重复快捷键多个动作）
        private readonly Dictionary<string, List<string>> _hotkeyStringToBizIds = new();
        // 业务ID → 动作
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

            // 先移除旧绑定（会清理 _hotkeyActions 中的旧值）
            UnregisterBizId(hotkeyId);

            // 保存动作
            _hotkeyActions[hotkeyId] = action;

            // 添加到新快捷键字符串的列表
            if (!_hotkeyStringToBizIds.ContainsKey(hotkeyString))
                _hotkeyStringToBizIds[hotkeyString] = new List<string>();
            _hotkeyStringToBizIds[hotkeyString].Add(hotkeyId);

            // 如果这个快捷键字符串还没注册过 Win32，则注册
            if (!_hotkeyRegIds.ContainsKey(hotkeyString))
            {
                var (mod, vk) = ParseHotkeyString(hotkeyString);
                if (vk == 0) return false;

                int id = _nextHotkeyId++;
                bool success = Win32Helper.RegisterHotKey(_hwnd, id, mod, vk);
                if (success)
                {
                    _hotkeyRegIds[hotkeyString] = id;
                    _regIdToHotkeyString[id] = hotkeyString;
                }
                return success;
            }

            return true;
        }

        public void UnregisterBizId(string hotkeyId)
        {
            // 从 _hotkeyStringToBizIds 中移除
            foreach (var kvp in _hotkeyStringToBizIds.ToList())
            {
                if (kvp.Value.Remove(hotkeyId))
                {
                    if (kvp.Value.Count == 0)
                    {
                        // 没有业务 ID 使用这个快捷键了，注销 Win32 注册
                        if (_hotkeyRegIds.TryGetValue(kvp.Key, out var regId))
                        {
                            Win32Helper.UnregisterHotKey(_hwnd, regId);
                            _hotkeyRegIds.Remove(kvp.Key);
                            _regIdToHotkeyString.Remove(regId);
                        }
                        _hotkeyStringToBizIds.Remove(kvp.Key);
                    }
                    break;
                }
            }
            _hotkeyActions.Remove(hotkeyId);
        }

        public void UnregisterAll()
        {
            foreach (var id in _hotkeyRegIds.Values)
            {
                Win32Helper.UnregisterHotKey(_hwnd, id);
            }
            _hotkeyRegIds.Clear();
            _regIdToHotkeyString.Clear();
            _hotkeyStringToBizIds.Clear();
        }

        public void ReRegisterAll(Dictionary<string, string> hotkeyMap)
        {
            // 注销所有 Win32 注册
            foreach (var id in _hotkeyRegIds.Values)
            {
                Win32Helper.UnregisterHotKey(_hwnd, id);
            }
            _hotkeyRegIds.Clear();
            _regIdToHotkeyString.Clear();
            _hotkeyStringToBizIds.Clear();

            // 按快捷键字符串分组，每组只注册一次 Win32
            var grouped = new Dictionary<string, List<string>>();
            foreach (var (bizId, hotkeyString) in hotkeyMap)
            {
                if (!_hotkeyActions.ContainsKey(bizId)) continue;
                if (string.IsNullOrEmpty(hotkeyString)) continue;

                if (!grouped.ContainsKey(hotkeyString))
                    grouped[hotkeyString] = new List<string>();
                grouped[hotkeyString].Add(bizId);
            }

            foreach (var (hotkeyString, bizIds) in grouped)
            {
                var (mod, vk) = ParseHotkeyString(hotkeyString);
                if (vk == 0) continue;

                int id = _nextHotkeyId++;
                bool success = Win32Helper.RegisterHotKey(_hwnd, id, mod, vk);
                if (success)
                {
                    _hotkeyRegIds[hotkeyString] = id;
                    _regIdToHotkeyString[id] = hotkeyString;
                    _hotkeyStringToBizIds[hotkeyString] = bizIds;
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

            if (mod != 0)
                mod |= Win32Helper.MOD_NOREPEAT;

            return (mod, vk);
        }

        public static Dictionary<string, string> GetDefaults()
        {
            return new Dictionary<string, string>
            {
                { "DesktopIcons", "Ctrl+Alt+D" },
                { "Settings", "Ctrl+Alt+S" },
                { "StatusBar", "Ctrl+Alt+H" },
                { "Crosshair", "Ctrl+Alt+C" },
                { "Inventory", "Ctrl+Alt+E" },
                { "DropItem", "Q" },
            };
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32Helper.WM_HOTKEY)
            {
                int regId = wParam.ToInt32();
                if (_regIdToHotkeyString.TryGetValue(regId, out var hotkeyString) &&
                    _hotkeyStringToBizIds.TryGetValue(hotkeyString, out var bizIds))
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        foreach (var bizId in bizIds)
                        {
                            if (_hotkeyActions.TryGetValue(bizId, out var action))
                                action();
                        }
                    });
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (var id in _hotkeyRegIds.Values)
            {
                Win32Helper.UnregisterHotKey(_hwnd, id);
            }
            _hotkeyRegIds.Clear();
            _regIdToHotkeyString.Clear();
            _hotkeyStringToBizIds.Clear();
            _hotkeyActions.Clear();

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
        }
    }
}
