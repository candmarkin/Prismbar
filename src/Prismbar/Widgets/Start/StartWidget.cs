using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `start` widget.
public sealed class StartWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Start;
    public override bool HasPopover => false;
}

public sealed class StartTile : UserControl { }
