using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Prismbar.Controls
{
    /// <summary>Renders a Lucide icon from Resources/Icons.xaml ("Icon.&lt;name&gt;") as a stroked path.</summary>
    public sealed class IconPath : ContentControl
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon), typeof(string), typeof(IconPath), new PropertyMetadata(null, (d, _) => ((IconPath)d).Update()));

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke), typeof(Brush), typeof(IconPath), new PropertyMetadata(null));

        private readonly Path _path;

        public IconPath()
        {
            Focusable = false;
            IsTabStop = false;
            Width = 18;
            Height = 18;
            _path = new Path
            {
                Stretch = Stretch.Uniform,
                StrokeThickness = 1.75,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                SnapsToDevicePixels = false,
            };
            _path.SetResourceReference(Shape.StrokeProperty, "Brush.Foreground");
            Content = _path;
        }

        public string? Icon
        {
            get => (string?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>Overrides the default <c>Brush.Foreground</c> stroke.</summary>
        public Brush? Stroke
        {
            get => (Brush?)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == StrokeProperty)
            {
                if (e.NewValue is Brush b) _path.Stroke = b;
                else _path.SetResourceReference(Shape.StrokeProperty, "Brush.Foreground");
            }
        }

        private void Update()
        {
            _path.Data = Icon is null ? null : TryFindResource("Icon." + Icon) as Geometry;
        }
    }
}

namespace Prismbar.Dock
{
    /// <summary>Clickable dock tile hosting a widget's compact view.</summary>
    public sealed class DockTile : ContentControl
    {
        private bool _leftDown;

        public event EventHandler? TileClick;
        public event EventHandler? TileRightClick;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _leftDown = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _leftDown = false;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (!_leftDown) return;
            _leftDown = false;
            TileClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            e.Handled = true;
            TileRightClick?.Invoke(this, EventArgs.Empty);
        }
    }
}
