using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui.Behaviors;

public sealed class CellFlameAnimationBehavior : Behavior<VisualElement>
{
    private VisualElement? _associatedObject;
    private BoardCellVm? _cell;
    private ShotMarkerState _lastMarkerState = ShotMarkerState.None;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.BindingContextChanged += OnBindingContextChanged;
        AttachToCell(bindable.BindingContext as BoardCellVm);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.BindingContextChanged -= OnBindingContextChanged;
        AttachToCell(null);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement element)
            return;

        AttachToCell(element.BindingContext as BoardCellVm);
    }

    private void AttachToCell(BoardCellVm? cell)
    {
        if (_cell is not null)
            _cell.PropertyChanged -= OnCellPropertyChanged;

        _cell = cell;
        _lastMarkerState = cell?.MarkerState ?? ShotMarkerState.None;

        if (_cell is null || _associatedObject is null)
            return;

        _cell.PropertyChanged += OnCellPropertyChanged;
    }

    private async void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BoardCellVm.MarkerState))
            return;

        var view = _associatedObject;
        var cell = _cell;
        if (view is null || cell is null)
            return;

        if (cell.MarkerState == ShotMarkerState.Hit && _lastMarkerState != ShotMarkerState.Hit)
            await RunFlameAnimationAsync(view).ConfigureAwait(false);

        _lastMarkerState = cell.MarkerState;
    }

    private static async Task RunFlameAnimationAsync(VisualElement view)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (AnimationRuntimeSettings.ReduceMotion)
            {
                view.Opacity = 0.62;
                view.Scale = 1;
                return;
            }

            view.Opacity = 0;
            view.Scale = 0.45;

            await Task.WhenAll(
                view.FadeToAsync(0.86, ScaleDuration(130), Easing.CubicOut),
                view.ScaleToAsync(1.1, ScaleDuration(130), Easing.CubicOut));

            await Task.WhenAll(
                view.FadeToAsync(0.64, ScaleDuration(210), Easing.CubicInOut),
                view.ScaleToAsync(0.96, ScaleDuration(210), Easing.CubicInOut));

            await Task.WhenAll(
                view.FadeToAsync(0.74, ScaleDuration(160), Easing.CubicInOut),
                view.ScaleToAsync(1.02, ScaleDuration(160), Easing.CubicInOut));
        });
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }
}
