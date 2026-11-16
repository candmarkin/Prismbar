using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `mic` widget.
public sealed class MicWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Mic;
}

public sealed class MicTile : UserControl { }

public sealed class MicPopover : UserControl { }
