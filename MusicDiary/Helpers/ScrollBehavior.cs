using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MusicDiary.Helpers;

// A presentation-only behavior: every newly opened page starts at the top.
public static class ScrollBehavior
{
    public static readonly DependencyProperty ResetOnChangeProperty = DependencyProperty.RegisterAttached(
        "ResetOnChange", typeof(object), typeof(ScrollBehavior), new PropertyMetadata(null, OnPageChanged));
    public static object GetResetOnChange(DependencyObject target) => target.GetValue(ResetOnChangeProperty);
    public static void SetResetOnChange(DependencyObject target, object value) => target.SetValue(ResetOnChangeProperty, value);
    private static void OnPageChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        if (target is ScrollViewer scroll)
            scroll.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(scroll.ScrollToTop));
    }
}
