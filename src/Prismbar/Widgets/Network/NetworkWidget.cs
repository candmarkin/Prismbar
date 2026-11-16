using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `network` widget.
public sealed class NetworkWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Network;
}

public sealed class NetworkTile : UserControl { }

public sealed class NetworkPopover : UserControl { }
