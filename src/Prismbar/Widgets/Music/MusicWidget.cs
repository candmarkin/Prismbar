using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `music` widget.
public sealed class MusicWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Music;
}

public sealed class MusicTile : UserControl { }

public sealed class MusicPopover : UserControl { }
