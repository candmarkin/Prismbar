using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `downloads` widget.
public sealed class DownloadsWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Downloads;
}

public sealed class DownloadsTile : UserControl { }

public sealed class DownloadsPopover : UserControl { }
