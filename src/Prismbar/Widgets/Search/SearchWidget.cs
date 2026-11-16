using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `search` widget.
public sealed class SearchWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Search;
}

public sealed class SearchTile : UserControl { }

public sealed class SearchPopover : UserControl { }
