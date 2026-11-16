using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `github` widget.
public sealed class GitHubWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.GitHub;
}

public sealed class GitHubTile : UserControl { }

public sealed class GitHubPopover : UserControl { }
