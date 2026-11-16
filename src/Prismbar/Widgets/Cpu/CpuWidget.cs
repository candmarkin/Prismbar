using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `cpu` widget.
public sealed class CpuWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Cpu;
}

public sealed class CpuTile : UserControl { }

public sealed class CpuPopover : UserControl { }
