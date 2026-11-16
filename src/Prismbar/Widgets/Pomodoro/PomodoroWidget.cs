using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `pomodoro` widget.
public sealed class PomodoroWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Pomodoro;
}

public sealed class PomodoroTile : UserControl { }

public sealed class PomodoroPopover : UserControl { }
