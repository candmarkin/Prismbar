using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `volume` widget.
public sealed class VolumeWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Volume;
}

public sealed class VolumeTile : UserControl { }

public sealed class VolumePopover : UserControl { }
