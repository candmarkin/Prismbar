using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `weather` widget.
public sealed class WeatherWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Weather;
}

public sealed class WeatherTile : UserControl { }

public sealed class WeatherPopover : UserControl { }
