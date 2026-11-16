using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `battery` widget.
public sealed class BatteryWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Battery;
}

public sealed class BatteryTile : UserControl { }

public sealed class BatteryPopover : UserControl { }
