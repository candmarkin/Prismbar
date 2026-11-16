using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `screentime` widget.
public sealed class ScreenTimeWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.ScreenTime;
}

public sealed class ScreenTimeTile : UserControl { }

public sealed class ScreenTimePopover : UserControl { }
