using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `ram` widget.
public sealed class RamWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Ram;
}

public sealed class RamTile : UserControl { }

public sealed class RamPopover : UserControl { }
