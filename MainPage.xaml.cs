using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui;

public partial class MainPage : ContentPage
{
    private BoardViewModel? _viewModel;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        base.OnBindingContextChanged();
        _viewModel = BindingContext as BoardViewModel;

        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel?.IsOverlayVisible == true)
            _ = AnimateOverlayAsync(_viewModel);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (e.PropertyName == nameof(BoardViewModel.IsOverlayVisible) && _viewModel.IsOverlayVisible)
            _ = AnimateOverlayAsync(_viewModel);
    }

    private async Task AnimateOverlayAsync(BoardViewModel vm)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (!vm.IsOverlayVisible)
                return;

            double speed = AnimationRuntimeSettings.SpeedMultiplier;
            if (vm.ReduceMotionMode)
            {
                OverlayScrim.Opacity = 1;
                OverlayCard.Opacity = 1;
                OverlayCard.Scale = 1;
                foreach (var child in FleetRecapStack.Children.OfType<VisualElement>())
                {
                    child.Opacity = 1;
                    child.TranslationY = 0;
                }
                return;
            }

            uint cardDuration = ScaleDuration(280, speed);
            OverlayScrim.Opacity = 0;
            OverlayCard.Opacity = 0;
            OverlayCard.Scale = 0.92;

            await Task.WhenAll(
                OverlayScrim.FadeToAsync(1, cardDuration, Easing.CubicInOut),
                OverlayCard.FadeToAsync(1, cardDuration, Easing.CubicOut),
                OverlayCard.ScaleToAsync(1, cardDuration, Easing.CubicOut));

            if (!vm.ShowOverlayRecap || FleetRecapStack.Children.Count == 0)
                return;

            int index = 0;
            foreach (var child in FleetRecapStack.Children.OfType<VisualElement>())
            {
                child.Opacity = 0;
                child.TranslationY = 8;
                await Task.Delay((int)ScaleDuration((uint)(35 + (index * 18)), speed));
                _ = Task.WhenAll(
                    child.FadeToAsync(1, ScaleDuration(180, speed), Easing.CubicOut),
                    child.TranslateToAsync(0, 0, ScaleDuration(180, speed), Easing.CubicOut));
                index++;
            }
        });
    }

    private static uint ScaleDuration(uint baseDuration, double speed)
    {
        double scaled = baseDuration * speed;
        return (uint)Math.Clamp((int)scaled, 30, 2000);
    }
}
