using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using LabelStudio.ViewModels;

namespace LabelStudio.Views;

public partial class EditorView : UserControl
{
    private ElementViewModel? _dragging;
    private Control?          _draggingContainer;
    private Point             _dragStart;
    private Point             _elementOrigin;
    private Canvas?           _labelCanvas;

    public EditorView()
    {
        InitializeComponent();
        // Loaded fires after ScrollViewer and all templates are fully applied,
        // so GetVisualDescendants reliably finds deeply-nested named controls.
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.Loaded -= OnLoaded; // wire once

        _labelCanvas = this.GetVisualDescendants()
                           .OfType<Canvas>()
                           .FirstOrDefault(c => c.Name == "LabelCanvas");

        if (_labelCanvas is null) return;

        // Attach handlers to LabelCanvas with Bubble + handledEventsToo so we
        // receive events even when inner controls (TextBlock, etc.) handle them first.
        _labelCanvas.AddHandler(PointerPressedEvent,  OnCanvasPointerPressed,
                                RoutingStrategies.Bubble, handledEventsToo: true);
        _labelCanvas.AddHandler(PointerMovedEvent,    OnCanvasPointerMoved,
                                RoutingStrategies.Bubble, handledEventsToo: true);
        _labelCanvas.AddHandler(PointerReleasedEvent, OnCanvasPointerReleased,
                                RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed) return;
        if (DataContext is not EditorViewModel vm) return;

        // Walk up the visual tree from the hit element to find the ContentPresenter
        // that is a direct child of the inner ItemsPanel Canvas (not LabelCanvas itself).
        var v = e.Source as Visual;
        ContentPresenter? container = null;
        ElementViewModel? target    = null;

        while (v is not null)
        {
            if (v is ContentPresenter cp
                && cp.GetVisualParent() is Canvas innerCanvas
                && !ReferenceEquals(innerCanvas, _labelCanvas))
            {
                container = cp;
                target    = cp.DataContext as ElementViewModel;
                break;
            }
            v = v.GetVisualParent();
        }

        if (target is null || container is null)
        {
            vm.SelectElement(null);
            return;
        }

        // Explicit non-nullable cast — we just verified both are non-null above.
        var safeContainer = (ContentPresenter)container;

        vm.SelectElement(target);
        _dragging          = target;
        _draggingContainer = safeContainer;
        _dragStart         = e.GetPosition(_labelCanvas);
        // Read the actual rendered Canvas position rather than relying on the VM value.
        _elementOrigin     = new Point(safeContainer.GetValue(Canvas.LeftProperty),
                                       safeContainer.GetValue(Canvas.TopProperty));
        e.Pointer.Capture(_labelCanvas);
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragging is null || _draggingContainer is null || _labelCanvas is null) return;
        if (!e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            // Button released outside window — cancel drag
            FinishDrag();
            return;
        }

        var pos     = e.GetPosition(_labelCanvas);
        var newLeft = Math.Clamp(_elementOrigin.X + pos.X - _dragStart.X,
                                 0, Math.Max(0, _labelCanvas.Width  - _dragging.Width));
        var newTop  = Math.Clamp(_elementOrigin.Y + pos.Y - _dragStart.Y,
                                 0, Math.Max(0, _labelCanvas.Height - _dragging.Height));

        // Directly set Canvas attached properties on the container for guaranteed
        // immediate visual update (bypasses style-binding priority question entirely).
        _draggingContainer.SetValue(Canvas.LeftProperty, newLeft);
        _draggingContainer.SetValue(Canvas.TopProperty,  newTop);

        // Keep the ViewModel in sync so the properties panel reflects live position.
        _dragging.Left = newLeft;
        _dragging.Top  = newTop;

        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragging is null) return;
        FinishDrag();
        e.Handled = true;
    }

    private void FinishDrag()
    {
        _dragging?.SyncPosition();
        _dragging          = null;
        _draggingContainer = null;
    }
}

