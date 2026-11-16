using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `apps` widget.
public sealed class AppsWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Apps;
    public override bool HasPopover => false;
}

public sealed class AppsTile : UserControl { }
