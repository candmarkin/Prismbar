using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `calendar` widget.
public sealed class CalendarWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Calendar;
}

public sealed class CalendarTile : UserControl { }

public sealed class CalendarPopover : UserControl { }
