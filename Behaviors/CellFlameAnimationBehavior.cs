using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui.Behaviors;

public sealed class CellFlameAnimationBehavior : Behavior<VisualElement>
{
    private VisualElement? _associatedObject;
    private BoardCellVm? _cell;
    private CancellationTokenSource? _animationCts;
    private ShotMarkerState _lastMarkerState = ShotMarkerState.None;

    protected override void OnAttachedTo(VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.BindingContextChanged += OnBindingContextChanged;
        bindable.PropertyChanged += OnAssociatedObjectPropertyChanged;
        AttachToCell(bindable.BindingContext as BoardCellVm);
    }

    protected override void OnDetachingFrom(VisualElement bindable)
    {
        bindable.BindingContextChanged -= OnBindingContextChanged;
        bindable.PropertyChanged -= OnAssociatedObjectPropertyChanged;
        AttachToCell(null);
        StopFlameAnimation(resetVisual: true);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not VisualElement element)
            return;

        AttachToCell(element.BindingContext as BoardCellVm);
    }

    private void OnAssociatedObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VisualElement.IsVisible))
            return;

        if (_associatedObject is null || _cell is null)
            return;

        if (_associatedObject.IsVisible && _cell.MarkerState == ShotMarkerState.Hit)
            StartFlameAnimation();
    }

    private void AttachToCell(BoardCellVm? cell)
    {
        if (_cell is not null)
            _cell.PropertyChanged -= OnCellPropertyChanged;

        _cell = cell;
        _lastMarkerState = cell?.MarkerState ?? ShotMarkerState.None;

        if (_cell is null || _associatedObject is null)
        {
            StopFlameAnimation(resetVisual: true);
            return;
        }

        _cell.PropertyChanged += OnCellPropertyChanged;

        if (_cell.MarkerState == ShotMarkerState.Hit)
            StartFlameAnimation();
        else
            StopFlameAnimation(resetVisual: true);
    }

    private void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BoardCellVm.MarkerState))
            return;

        var cell = _cell;
        if (cell is null)
            return;

        if (cell.MarkerState == ShotMarkerState.Hit && _lastMarkerState != ShotMarkerState.Hit)
        {
            StartFlameAnimation();
        }
        else if (cell.MarkerState != ShotMarkerState.Hit && _lastMarkerState == ShotMarkerState.Hit)
        {
            StopFlameAnimation(resetVisual: true);
        }

        _lastMarkerState = cell.MarkerState;
    }

    private void StartFlameAnimation()
    {
        var view = _associatedObject;
        if (view is null)
            return;

        if (_animationCts is not null)
            return;

        var cts = new CancellationTokenSource();
        _animationCts = cts;
        _ = RunFlameAnimationLoopAsync(view, cts.Token);
    }

    private void StopFlameAnimation(bool resetVisual)
    {
        if (_animationCts is not null)
        {
            _animationCts.Cancel();
            _animationCts.Dispose();
            _animationCts = null;
        }

        if (!resetVisual || _associatedObject is null)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_associatedObject is null)
                return;

            _associatedObject.AbortAnimation("CellFlameFlicker");
            _associatedObject.Opacity = 0;
            _associatedObject.Scale = 1;
        });
    }

    private async Task RunFlameAnimationLoopAsync(VisualElement view, CancellationToken cancellationToken)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                view.AbortAnimation("CellFlameFlicker");
                view.Opacity = AnimationRuntimeSettings.ReduceMotion ? 0.72 : 0.58;
                view.Scale = AnimationRuntimeSettings.ReduceMotion ? 1 : 0.9;
            });

            while (!cancellationToken.IsCancellationRequested)
            {
                if (AnimationRuntimeSettings.ReduceMotion)
                {
                    await Task.Delay((int)ScaleDuration(220), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                double peakOpacity = 0.82 + (Random.Shared.NextDouble() * 0.12);
                double valleyOpacity = 0.6 + (Random.Shared.NextDouble() * 0.1);
                double peakScale = 1.03 + (Random.Shared.NextDouble() * 0.12);
                double valleyScale = 0.9 + (Random.Shared.NextDouble() * 0.08);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    view.AbortAnimation("CellFlameFlicker");
                    await Task.WhenAll(
                        view.FadeToAsync(peakOpacity, ScaleDuration(120), Easing.CubicOut),
                        view.ScaleToAsync(peakScale, ScaleDuration(120), Easing.CubicOut));

                    if (cancellationToken.IsCancellationRequested)
                        return;

                    await Task.WhenAll(
                        view.FadeToAsync(valleyOpacity, ScaleDuration(180), Easing.CubicInOut),
                        view.ScaleToAsync(valleyScale, ScaleDuration(180), Easing.CubicInOut));
                });

                await Task.Delay((int)ScaleDuration(45), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when a hit animation cycle is interrupted by reset/theme/board recycle.
        }
        finally
        {
            if (_associatedObject is not null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_associatedObject is null)
                        return;

                    _associatedObject.AbortAnimation("CellFlameFlicker");
                    _associatedObject.Opacity = 0;
                    _associatedObject.Scale = 1;
                });
            }
        }
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }
}
