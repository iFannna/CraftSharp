using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// ItemsControl 方向键导航：按可视位置把键盘焦点移到相邻项（图标网格等无选择语义的集合用）
    /// </summary>
    internal static class DirectionalFocusHelper
    {
        private enum Direction { Left, Right, Up, Down }

        /// <summary>
        /// 给 ItemsControl 挂方向键导航（Left/Right/Up/Down 在项之间移动焦点）
        /// </summary>
        public static void Attach(ItemsControl itemsControl)
        {
            itemsControl.PreviewKeyDown += (_, e) =>
            {
                var direction = e.Key switch
                {
                    Key.Left => (Direction?)Direction.Left,
                    Key.Right => Direction.Right,
                    Key.Up => Direction.Up,
                    Key.Down => Direction.Down,
                    _ => null
                };
                if (direction != null && MoveFocus(itemsControl, direction.Value))
                    e.Handled = true;
            };
        }

        /// <summary>
        /// 把焦点移到集合里第一个可聚焦项（窗口打开时的方向键起点）
        /// </summary>
        public static void FocusFirst(ItemsControl itemsControl)
        {
            var candidates = new List<FrameworkElement>();
            CollectFocusable(itemsControl, candidates);
            if (candidates.Count > 0)
                Keyboard.Focus(candidates[0]);
        }

        private static bool MoveFocus(ItemsControl itemsControl, Direction direction)
        {
            var focused = Keyboard.FocusedElement as FrameworkElement;
            if (focused == null || !IsDescendant(itemsControl, focused))
                return false;

            var candidates = new List<FrameworkElement>();
            CollectFocusable(itemsControl, candidates);
            if (candidates.Count == 0)
                return false;

            var origin = Center(itemsControl, focused);
            FrameworkElement? best = null;
            var bestScore = double.PositiveInfinity;

            foreach (var candidate in candidates)
            {
                if (candidate == focused)
                    continue;

                var center = Center(itemsControl, candidate);
                double primary = direction switch
                {
                    Direction.Right => center.X - origin.X,
                    Direction.Left => origin.X - center.X,
                    Direction.Down => center.Y - origin.Y,
                    Direction.Up => origin.Y - center.Y,
                    _ => 0
                };
                if (primary <= 0)
                    continue;

                // 主轴向距离为主，横向偏差加权做同排/同列优先
                double cross = direction is Direction.Left or Direction.Right
                    ? Math.Abs(center.Y - origin.Y)
                    : Math.Abs(center.X - origin.X);
                var score = primary + cross * 10;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            if (best == null)
                return false;

            best.BringIntoView();
            Keyboard.Focus(best);
            return true;
        }

        private static System.Windows.Point Center(Visual relativeTo, FrameworkElement element)
        {
            return element.TransformToVisual(relativeTo)
                .Transform(new System.Windows.Point(element.ActualWidth / 2, element.ActualHeight / 2));
        }

        private static bool IsDescendant(DependencyObject parent, DependencyObject node)
        {
            while (node != null)
            {
                if (ReferenceEquals(node, parent))
                    return true;
                node = VisualTreeHelper.GetParent(node);
            }
            return false;
        }

        private static void CollectFocusable(DependencyObject parent, List<FrameworkElement> result)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe && fe.Focusable && fe.IsVisible)
                    result.Add(fe);
                CollectFocusable(child, result);
            }
        }
    }
}
