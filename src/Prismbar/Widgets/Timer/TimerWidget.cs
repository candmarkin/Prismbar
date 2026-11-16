using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `timer` widget.
public sealed class TimerWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Timer;
}

public sealed class TimerTile : UserControl { }

public sealed class TimerPopover : UserControl { }
