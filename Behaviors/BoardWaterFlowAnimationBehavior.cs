using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace BattleshipMaui.Behaviors;

public sealed class BoardWaterFlowAnimationBehavior : Behavior<Grid>
{
    private const string WaveLayerAClassId = "BoardWaveLayerA";
    private const string WaveLayerBClassId = "BoardWaveLayerB";

    private Grid? _associatedObject;
    private CancellationTokenSource? _animationCts;

    protected override void OnAttachedTo(Grid bindable)
    {
        base.OnAttachedTo(bindable);
        _associatedObject = bindable;
        bindable.PropertyChanged += OnAssociatedObjectPropertyChanged;
        StartAnimation();
    }

    protected override void OnDetachingFrom(Grid bindable)
    {
        bindable.PropertyChanged -= OnAssociatedObjectPropertyChanged;
        StopAnimation(resetVisual: true);
        _associatedObject = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnAssociatedObjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(VisualElement.IsVisible))
            return;

        if (_associatedObject?.IsVisible == true)
            StartAnimation();
        else
            StopAnimation(resetVisual: true);
    }

    private void StartAnimation()
    {
        var surface = _associatedObject;
        if (surface is null || !surface.IsVisible || _animationCts is not null)
            return;

        var cts = new CancellationTokenSource();
        _animationCts = cts;
        _ = RunWaterLoopAsync(surface, cts.Token);
    }

    private void StopAnimation(bool resetVisual)
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

            ResetWaveVisuals(_associatedObject);
        });
    }

    private async Task RunWaterLoopAsync(Grid surface, CancellationToken cancellationToken)
    {
        try
        {
            await MainThread.InvokeOnMainThreadAsync(() => ResetWaveVisuals(surface));

            var (layerA, layerB) = ResolveWaveLayers(surface);
            if (layerA is null && layerB is null)
                return;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (AnimationRuntimeSettings.ReduceMotion)
                {
                    await Task.Delay((int)ScaleDuration(900), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                await AnimatePhaseAsync(
                    surface,
                    targetAX: -12,
                    targetAY: 2,
                    targetAOpacity: 0.68,
                    targetBX: 10,
                    targetBY: -3,
                    targetBOpacity: 0.5,
                    duration: ScaleDuration(3200)).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await AnimatePhaseAsync(
                    surface,
                    targetAX: 9,
                    targetAY: -2,
                    targetAOpacity: 0.53,
                    targetBX: -11,
                    targetBY: 3,
                    targetBOpacity: 0.38,
                    duration: ScaleDuration(3600)).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when board visual tree is recycled or mode switches.
        }
        finally
        {
            if (_associatedObject is not null)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_associatedObject is null)
                        return;

                    ResetWaveVisuals(_associatedObject);
                });
            }
        }
    }

    private static async Task AnimatePhaseAsync(
        Grid surface,
        double targetAX,
        double targetAY,
        double targetAOpacity,
        double targetBX,
        double targetBY,
        double targetBOpacity,
        uint duration)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var (layerA, layerB) = ResolveWaveLayers(surface);
            var tasks = new List<Task>(4);

            if (layerA is not null)
            {
                tasks.Add(layerA.TranslateToAsync(targetAX, targetAY, duration, Easing.SinInOut));
                tasks.Add(layerA.FadeToAsync(targetAOpacity, duration, Easing.CubicInOut));
            }

            if (layerB is not null)
            {
                tasks.Add(layerB.TranslateToAsync(targetBX, targetBY, duration, Easing.SinInOut));
                tasks.Add(layerB.FadeToAsync(targetBOpacity, duration, Easing.CubicInOut));
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        });
    }

    private static void ResetWaveVisuals(Grid surface)
    {
        var (layerA, layerB) = ResolveWaveLayers(surface);
        if (layerA is not null)
        {
            layerA.TranslationX = 0;
            layerA.TranslationY = 0;
            layerA.Opacity = 0.58;
        }

        if (layerB is not null)
        {
            layerB.TranslationX = 0;
            layerB.TranslationY = 0;
            layerB.Opacity = 0.42;
        }
    }

    private static (Border? LayerA, Border? LayerB) ResolveWaveLayers(Grid surface)
    {
        Border? layerA = surface.Children
            .OfType<Border>()
            .FirstOrDefault(border => string.Equals(border.ClassId, WaveLayerAClassId, StringComparison.Ordinal));
        Border? layerB = surface.Children
            .OfType<Border>()
            .FirstOrDefault(border => string.Equals(border.ClassId, WaveLayerBClassId, StringComparison.Ordinal));
        return (layerA, layerB);
    }

    private static uint ScaleDuration(uint baseDuration)
    {
        double scaled = baseDuration * AnimationRuntimeSettings.SpeedMultiplier;
        return (uint)Math.Clamp((int)scaled, 120, 12000);
    }
}
