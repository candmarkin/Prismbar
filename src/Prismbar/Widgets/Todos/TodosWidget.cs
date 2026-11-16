using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `todos` widget.
public sealed class TodosWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Todos;
}

public sealed class TodosTile : UserControl { }

public sealed class TodosPopover : UserControl { }
