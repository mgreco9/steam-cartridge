using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using CartridgeDesigner.Avalonia.ViewModels.DesignViewModel;

namespace CartridgeDesigner.Avalonia.Views.DesignView;

public partial class DesignEditorView : UserControl
{
    private DesignEditorViewModel? _vm => DataContext as DesignEditorViewModel;

    private bool _dragging;
    private Point _lastPoint;

    public DesignEditorView()
    {
        InitializeComponent();
    }

    private void StartDrag(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        _lastPoint = e.GetPosition(this);
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    private void DragMove(object? sender, PointerEventArgs e)
    {
        if (!_dragging || _vm == null)
            return;

        var pos = e.GetPosition(this);
        var delta = pos - _lastPoint;

        _vm.Drag(delta.X, delta.Y);

        _lastPoint = pos;
    }

    private void EndDrag(object? sender, PointerReleasedEventArgs e)
    {
        _dragging = false;
        Cursor = Cursor.Default;
    }

    private void ZoomImage(object? sender, PointerWheelEventArgs e)
    {
        if (_vm == null)
            return;

        const double zoomSpeed = 0.1;

        var delta = e.Delta.Y * zoomSpeed;

        _vm.Scale(delta);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        UpdateBorder();
    }

    private void OnBorderSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateBorder();
    }

    private void UpdateBorder()
    {
        if (_vm == null)
            return;

        Matrix? transform = CropFrame.TransformToVisual(Viewport);
        if (transform == null)
            return;

        Rect cropRect = new Rect(0, 0, CropFrame.Bounds.Width, CropFrame.Bounds.Height).TransformToAABB(transform.Value);

        _vm.ScaleBorder(cropRect);
    }
}
