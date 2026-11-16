using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `tray` widget.
public sealed class TrayWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Tray;
}

public sealed class TrayTile : UserControl { }

public sealed class TrayPopover : UserControl { }
