using System.ComponentModel;
using BattleshipMaui.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui;

public partial class MainPage : ContentPage
{
    private BoardViewModel? _viewModel;
    private BoardViewMode _currentBoardMode = BoardViewMode.Enemy;
    private bool _isBoardTransitionRunning;

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
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: true);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel is not null)
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: true);

        if (_viewModel?.IsOverlayVisible == true)
            _ = AnimateOverlayAsync(_viewModel);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (e.PropertyName == nameof(BoardViewModel.IsOverlayVisible) && _viewModel.IsOverlayVisible)
            _ = AnimateOverlayAsync(_viewModel);

        if (e.PropertyName == nameof(BoardViewModel.BoardViewMode))
            _ = AnimateBoardModeTransitionAsync(_viewModel.BoardViewMode, instant: false);
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

    private void ApplyBoardModeInstant(BoardViewMode mode)
    {
        bool enemyVisible = mode == BoardViewMode.Enemy;

        EnemyBoardPage.IsVisible = enemyVisible;
        EnemyBoardPage.Opacity = enemyVisible ? 1 : 0;
        EnemyBoardPage.TranslationX = 0;
        EnemyBoardPage.Scale = 1;

        PlayerBoardPage.IsVisible = !enemyVisible;
        PlayerBoardPage.Opacity = enemyVisible ? 0 : 1;
        PlayerBoardPage.TranslationX = 0;
        PlayerBoardPage.Scale = 1;

        _currentBoardMode = mode;
    }

    private async Task AnimateBoardModeTransitionAsync(BoardViewMode targetMode, bool instant)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (EnemyBoardPage is null || PlayerBoardPage is null)
                return;

            if (_currentBoardMode == targetMode)
            {
                ApplyBoardModeInstant(targetMode);
                return;
            }

            if (_isBoardTransitionRunning)
            {
                ApplyBoardModeInstant(targetMode);
                return;
            }

            bool reduceMotion = instant || _viewModel?.ReduceMotionMode == true;
            if (reduceMotion)
            {
                ApplyBoardModeInstant(targetMode);
                return;
            }

            var incoming = targetMode == BoardViewMode.Enemy ? EnemyBoardPage : PlayerBoardPage;
            var outgoing = targetMode == BoardViewMode.Enemy ? PlayerBoardPage : EnemyBoardPage;

            double speed = AnimationRuntimeSettings.SpeedMultiplier;
            double width = CommandCenterBoardHost.Width;
            if (width < 1)
                width = Math.Max(EnemyBoardPage.Width, PlayerBoardPage.Width);

            if (width < 1 && _viewModel is not null)
                width = _viewModel.BoardFramePixelSize + 24;

            if (width < 1)
                width = 420;

            double direction = targetMode == BoardViewMode.Player ? 1 : -1;
            double incomingStart = direction * Math.Max(80, width * 0.24);
            double outgoingEnd = -direction * Math.Max(48, width * 0.16);
            uint duration = ScaleDuration(280, speed);

            _isBoardTransitionRunning = true;
            try
            {
                incoming.IsVisible = true;
                incoming.Opacity = 0;
                incoming.TranslationX = incomingStart;
                incoming.Scale = 0.985;

                outgoing.IsVisible = true;
                outgoing.Scale = 1;

                await Task.WhenAll(
                    incoming.FadeToAsync(1, duration, Easing.CubicOut),
                    incoming.TranslateToAsync(0, 0, duration, Easing.CubicOut),
                    incoming.ScaleToAsync(1, duration, Easing.CubicOut),
                    outgoing.FadeToAsync(0, ScaleDuration(210, speed), Easing.CubicIn),
                    outgoing.TranslateToAsync(outgoingEnd, 0, duration, Easing.CubicIn),
                    outgoing.ScaleToAsync(0.99, duration, Easing.CubicIn));

                outgoing.IsVisible = false;
                outgoing.Opacity = 0;
                outgoing.TranslationX = 0;
                outgoing.Scale = 1;

                incoming.IsVisible = true;
                incoming.Opacity = 1;
                incoming.TranslationX = 0;
                incoming.Scale = 1;
                _currentBoardMode = targetMode;
            }
            finally
            {
                _isBoardTransitionRunning = false;
            }
        });
    }
}
