using System.Windows.Controls;
using Prismbar.Core.Settings;

namespace Prismbar.Widgets;

// PLACEHOLDER — replaced by the full implementation of the `notes` widget.
public sealed class NotesWidget(WidgetContext ctx) : WidgetBase(ctx)
{
    public override string Id => WidgetIds.Notes;
}

public sealed class NotesTile : UserControl { }

public sealed class NotesPopover : UserControl { }
