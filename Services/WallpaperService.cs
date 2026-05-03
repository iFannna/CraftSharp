using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CraftSharp
{
    /// <summary>
    /// 壁纸服务 - 调用Windows原生API设置壁纸
    /// </summary>
    public static class WallpaperService
    {
        #region Windows API 导入

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        #endregion

        /// <summary>
        /// 设置桌面壁纸
        /// </summary>
        /// <param name="imagePath">图片路径</param>
        /// <param name="style">壁纸样式（填充、适应、拉伸等）</param>
        public static void SetWallpaper(string imagePath, WallpaperStyle style = WallpaperStyle.Fill)
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException("壁纸文件不存在", imagePath);
            }

            // 设置壁纸样式
            SetWallpaperStyle(style);

            // 设置壁纸
            int result = SystemParametersInfo(
                SPI_SETDESKWALLPAPER,
                0,
                imagePath,
                SPIF_UPDATEINIFILE | SPIF_SENDCHANGE
            );

            if (result == 0)
            {
                throw new Exception("设置壁纸失败，系统API调用返回错误");
            }
        }

        /// <summary>
        /// 清除壁纸（恢复默认）
        /// </summary>
        public static void ClearWallpaper()
        {
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        /// <summary>
        /// 设置壁纸样式（通过注册表）
        /// </summary>
        private static void SetWallpaperStyle(WallpaperStyle style)
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Control Panel\Desktop", true);

            if (key == null)
            {
                throw new Exception("无法访问注册表");
            }

            // WallpaperStyle: 0=居中, 2=拉伸, 6=适应, 10=填充, 22=跨区
            // TileWallpaper: 0=不平铺, 1=平铺
            key.SetValue("WallpaperStyle", style switch
            {
                WallpaperStyle.Center => "0",
                WallpaperStyle.Tile => "1",
                WallpaperStyle.Stretch => "2",
                WallpaperStyle.Fit => "6",
                WallpaperStyle.Fill => "10",
                WallpaperStyle.Span => "22",
                _ => "10"
            });

            key.SetValue("TileWallpaper", style == WallpaperStyle.Tile ? "1" : "0");
        }
    }

    /// <summary>
    /// 壁纸显示样式
    /// </summary>
    public enum WallpaperStyle
    {
        Center,  // 居中
        Tile,    // 平铺
        Stretch, // 拉伸
        Fit,     // 适应
        Fill,    // 填充（默认）
        Span     // 跨区
    }
}