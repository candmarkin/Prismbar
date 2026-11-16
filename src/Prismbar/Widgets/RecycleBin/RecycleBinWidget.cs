using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `recyclebin` widget.
public sealed class RecycleBinWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.RecycleBin;
}

public sealed class RecycleBinTile : UserControl { }

public sealed class RecycleBinPopover : UserControl { }
