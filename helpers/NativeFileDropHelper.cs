using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Interop;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 原生拖放辅助类 - 支持 Windows 原生拖拽缩略图显示
    ///
    /// 使用 IDropTargetHelper 接口让 Windows Shell 显示文件拖拽时的图标缩略图，
    /// 解决 WPF 透明窗口 (AllowsTransparency="True") 不显示拖拽缩略图的问题。
    /// </summary>
    public static class NativeDropHelper
    {
        /// <summary>
        /// 为窗口注册原生拖放（仅显示缩略图，不接受文件）
        /// 鼠标光标始终显示为禁止状态
        /// </summary>
        public static IDisposable RegisterForThumbnail(Window window)
        {
            var target = new NativeFileDropTarget(window, (_, _) => { }, _ => false);
            target.Attach();
            return target;
        }

        /// <summary>
        /// 为窗口注册原生拖放（显示缩略图 + 处理文件放置）
        /// </summary>
        /// <param name="window">目标窗口</param>
        /// <param name="onDrop">文件放置回调</param>
        /// <param name="canDropAt">判断鼠标位置是否接受文件（返回true显示箭头光标，false显示禁止光标）</param>
        public static IDisposable RegisterWithDropHandler(
            Window window,
            Action<IReadOnlyList<string>, System.Windows.Point> onDrop,
            Func<System.Windows.Point, bool> canDropAt)
        {
            var target = new NativeFileDropTarget(window, onDrop, canDropAt);
            target.Attach();
            return target;
        }
    }

    internal sealed class NativeFileDropTarget : NativeMethods.IDropTarget, IDisposable
    {
        private readonly Window _window;
        private readonly Action<IReadOnlyList<string>, System.Windows.Point> _onDrop;
        private readonly Func<System.Windows.Point, bool> _canDropAt;
        private readonly nint _hwnd;
        private readonly NativeMethods.IDropTargetHelper? _dropTargetHelper;
        private bool _isAttached;

        public NativeFileDropTarget(
            Window window,
            Action<IReadOnlyList<string>, System.Windows.Point> onDrop,
            Func<System.Windows.Point, bool> canDropAt)
        {
            _window = window;
            _onDrop = onDrop;
            _canDropAt = canDropAt;
            _hwnd = new WindowInteropHelper(window).Handle;
            _dropTargetHelper = NativeMethods.TryCreateDropTargetHelper();
        }

        /// <summary>
        /// 注册原生拖放（替代 WPF AllowDrop）
        /// </summary>
        public void Attach()
        {
            if (_isAttached)
                return;

            int result = NativeMethods.RegisterDragDrop(_hwnd, this);
            if (result == NativeMethods.DRAGDROP_E_ALREADYREGISTERED)
            {
                NativeMethods.RevokeDragDrop(_hwnd);
                result = NativeMethods.RegisterDragDrop(_hwnd, this);
            }

            Marshal.ThrowExceptionForHR(result);
            _isAttached = true;
        }

        public void Dispose()
        {
            if (!_isAttached)
                return;

            NativeMethods.RevokeDragDrop(_hwnd);
            _isAttached = false;
        }

        int NativeMethods.IDropTarget.DragEnter(ComDataObject pDataObj, uint grfKeyState, NativeMethods.POINTL pt, ref uint pdwEffect)
        {
            pdwEffect = ResolveEffect(pDataObj, pdwEffect, pt);
            _dropTargetHelper?.DragEnter(_hwnd, pDataObj, ref pt, pdwEffect);
            return NativeMethods.S_OK;
        }

        int NativeMethods.IDropTarget.DragOver(uint grfKeyState, NativeMethods.POINTL pt, ref uint pdwEffect)
        {
            pdwEffect = ResolveEffect(null, pdwEffect, pt);
            _dropTargetHelper?.DragOver(ref pt, pdwEffect);
            return NativeMethods.S_OK;
        }

        int NativeMethods.IDropTarget.DragLeave()
        {
            _dropTargetHelper?.DragLeave();
            return NativeMethods.S_OK;
        }

        int NativeMethods.IDropTarget.Drop(ComDataObject pDataObj, uint grfKeyState, NativeMethods.POINTL pt, ref uint pdwEffect)
        {
            pdwEffect = ResolveEffect(pDataObj, pdwEffect, pt);
            _dropTargetHelper?.Drop(pDataObj, ref pt, pdwEffect);

            // 只有在有效区域才处理放置
            if (pdwEffect != NativeMethods.DROPEFFECT_NONE)
            {
                string[] paths = NativeMethods.ReadFileDropList(pDataObj);
                if (paths.Length > 0)
                {
                    // 将屏幕坐标转换为 WPF 窗口坐标
                    var screenPoint = new System.Windows.Point(pt.X, pt.Y);
                    _window.Dispatcher.Invoke(() => _onDrop(paths, screenPoint));
                }
            }

            return NativeMethods.S_OK;
        }

        private uint ResolveEffect(ComDataObject? dataObject, uint allowedEffects, NativeMethods.POINTL pt)
        {
            // 检查数据格式
            if (dataObject is not null && !NativeMethods.ContainsFileDrop(dataObject))
            {
                return NativeMethods.DROPEFFECT_NONE;
            }

            // 调用回调判断鼠标位置是否接受文件
            var screenPoint = new System.Windows.Point(pt.X, pt.Y);
            bool canDrop = _window.Dispatcher.Invoke(() => _canDropAt(screenPoint));

            if (!canDrop)
            {
                return NativeMethods.DROPEFFECT_NONE;
            }

            if ((allowedEffects & NativeMethods.DROPEFFECT_COPY) != 0)
                return NativeMethods.DROPEFFECT_COPY;

            if ((allowedEffects & NativeMethods.DROPEFFECT_MOVE) != 0)
                return NativeMethods.DROPEFFECT_MOVE;

            if ((allowedEffects & NativeMethods.DROPEFFECT_LINK) != 0)
                return NativeMethods.DROPEFFECT_LINK;

            return NativeMethods.DROPEFFECT_NONE;
        }
    }

    internal static class NativeMethods
    {
        internal const int S_OK = 0;
        internal const int DRAGDROP_E_ALREADYREGISTERED = unchecked((int)0x80040101);
        internal const uint DROPEFFECT_NONE = 0;
        internal const uint DROPEFFECT_COPY = 1;
        internal const uint DROPEFFECT_MOVE = 2;
        internal const uint DROPEFFECT_LINK = 4;

        private const short CF_HDROP = 15;
        private static readonly Guid DragDropHelperClsid = new("4657278A-411B-11D2-839A-00C04FD918D0");

        internal static IDropTargetHelper? TryCreateDropTargetHelper()
        {
            try
            {
                Type? helperType = Type.GetTypeFromCLSID(DragDropHelperClsid, throwOnError: true);
                return helperType is null ? null : Activator.CreateInstance(helperType) as IDropTargetHelper;
            }
            catch
            {
                return null;
            }
        }

        internal static bool ContainsFileDrop(ComDataObject dataObject)
        {
            FORMATETC format = CreateFileDropFormat();
            return dataObject.QueryGetData(ref format) == S_OK;
        }

        internal static string[] ReadFileDropList(ComDataObject dataObject)
        {
            FORMATETC format = CreateFileDropFormat();
            dataObject.GetData(ref format, out STGMEDIUM medium);

            try
            {
                if (medium.unionmember == nint.Zero)
                    return [];

                uint fileCount = DragQueryFile(medium.unionmember, 0xFFFFFFFF, null, 0);
                string[] paths = new string[fileCount];

                for (uint i = 0; i < fileCount; i++)
                {
                    uint length = DragQueryFile(medium.unionmember, i, null, 0);
                    char[] buffer = new char[length + 1];
                    DragQueryFile(medium.unionmember, i, buffer, (uint)buffer.Length);
                    paths[i] = new string(buffer, 0, (int)length);
                }

                return paths;
            }
            finally
            {
                ReleaseStgMedium(ref medium);
            }
        }

        private static FORMATETC CreateFileDropFormat()
        {
            return new FORMATETC
            {
                cfFormat = CF_HDROP,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = TYMED.TYMED_HGLOBAL,
            };
        }

        [DllImport("ole32.dll")]
        internal static extern int RegisterDragDrop(nint hwnd, IDropTarget pDropTarget);

        [DllImport("ole32.dll")]
        internal static extern int RevokeDragDrop(nint hwnd);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern uint DragQueryFile(nint hDrop, uint iFile, char[]? lpszFile, uint cch);

        [DllImport("ole32.dll")]
        internal static extern void ReleaseStgMedium(ref STGMEDIUM pmedium);

        [ComImport]
        [Guid("00000122-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDropTarget
        {
            [PreserveSig]
            int DragEnter(ComDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect);

            [PreserveSig]
            int DragOver(uint grfKeyState, POINTL pt, ref uint pdwEffect);

            [PreserveSig]
            int DragLeave();

            [PreserveSig]
            int Drop(ComDataObject pDataObj, uint grfKeyState, POINTL pt, ref uint pdwEffect);
        }

        [ComImport]
        [Guid("4657278B-411B-11D2-839A-00C04FD918D0")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDropTargetHelper
        {
            void DragEnter(nint hwndTarget, ComDataObject dataObject, ref POINTL point, uint effect);
            void DragLeave();
            void DragOver(ref POINTL point, uint effect);
            void Drop(ComDataObject dataObject, ref POINTL point, uint effect);
            void Show(bool show);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINTL
        {
            public int X;
            public int Y;
        }
    }
}