using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Prismbar.Controls;

public partial class PopoverHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(PopoverHeader));

    public static readonly DependencyProperty ActionsProperty =
        DependencyProperty.Register(nameof(Actions), typeof(object), typeof(PopoverHeader));

    public PopoverHeader() => InitializeComponent();

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Optional extra header buttons placed left of the gear.</summary>
    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }
}

/// <summary>null/empty → Collapsed, otherwise Visible.</summary>
public sealed class NullToCollapsedConverter : IValueConverter
{
    public static readonly NullToCollapsedConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is null || value is string { Length: 0 } ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>true → Collapsed, false → Visible.</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c) => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>Positive number → Visible, otherwise Collapsed (badges).</summary>
public sealed class PositiveToVisibilityConverter : IValueConverter
{
    public static readonly PositiveToVisibilityConverter Instance = new();
    public object Convert(object? value, Type t, object? p, CultureInfo c) =>
        value is int i && i > 0 || value is long l && l > 0 || value is double d && d > 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}
