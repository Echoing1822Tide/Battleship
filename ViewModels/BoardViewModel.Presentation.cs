using Microsoft.Maui.Graphics;

namespace BattleshipMaui.ViewModels;

public partial class BoardViewModel
{
    private const double LanPlacementCellSize = 42;
    public const double BattleCellSize = 82;

    private static double _activeRenderCellSize = CellSize;
    private bool _turnCinematicsEnabled;
    private bool _hasConfiguredTurnCinematicsPreference;
    private bool _commanderVoiceEnabled = true;
    private bool _hasConfiguredCommanderVoicePreference;
    private string _turnTransitionCoordinate = "--";
    private bool _isIntelBubbleVisible;
    private string _intelBubbleTitle = string.Empty;
    private string _intelBubbleMessage = string.Empty;
    private int _intelBubbleSequenceId;

    public static double ActiveRenderCellSize => _activeRenderCellSize;

    public double BoardCellSize => IsPlacementPhase
        ? (IsLanMode ? LanPlacementCellSize : CellSize)
        : BattleCellSize;

    public double BoardTitleFontSize => IsPlacementPhase ? 36 : 52;
    public double BoardAxisFontSize => IsPlacementPhase ? 12 : 18;

    public Thickness GameplayContentPadding => ShowCompactCommandHeader
        ? new Thickness(8, 6, 8, 10)
        : new Thickness(14, 10, 14, 14);

    public bool ShowExpandedCommandHeader => IsPlacementPhase || IsGameOver || IsSettingsOpen || IsOverlayVisible;

    public bool ShowCompactCommandHeader => !ShowExpandedCommandHeader;

    public bool ShowDetailedBattleStatusCard => !ShowCompactCommandHeader && !(IsLanMode && IsPlacementPhase);

    public string CompactHeaderTitle => string.IsNullOrWhiteSpace(TurnMessage)
        ? AppVariant.PublicAppName
        : TurnMessage;

    public string CompactHeaderStatus => string.IsNullOrWhiteSpace(StatusMessage)
        ? CurrentGameStatsLine
        : StatusMessage;

    public bool TurnCinematicsEnabled
    {
        get => _turnCinematicsEnabled;
        set
        {
            if (_turnCinematicsEnabled == value)
                return;

            _turnCinematicsEnabled = value;
            _hasConfiguredTurnCinematicsPreference = true;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public bool CommanderVoiceEnabled
    {
        get => _commanderVoiceEnabled;
        set
        {
            if (_commanderVoiceEnabled == value)
                return;

            _commanderVoiceEnabled = value;
            _hasConfiguredCommanderVoicePreference = true;
            OnPropertyChanged();
            SaveSettings();
        }
    }

    public string TurnTransitionCoordinate
    {
        get => _turnTransitionCoordinate;
        private set
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? "--" : value;
            if (_turnTransitionCoordinate == normalized)
                return;

            _turnTransitionCoordinate = normalized;
            OnPropertyChanged();
        }
    }

    public bool IsIntelBubbleVisible
    {
        get => _isIntelBubbleVisible;
        private set
        {
            if (_isIntelBubbleVisible == value)
                return;

            _isIntelBubbleVisible = value;
            OnPropertyChanged();
        }
    }

    public string IntelBubbleTitle
    {
        get => _intelBubbleTitle;
        private set
        {
            if (_intelBubbleTitle == value)
                return;

            _intelBubbleTitle = value;
            OnPropertyChanged();
        }
    }

    public string IntelBubbleMessage
    {
        get => _intelBubbleMessage;
        private set
        {
            if (_intelBubbleMessage == value)
                return;

            _intelBubbleMessage = value;
            OnPropertyChanged();
        }
    }

    private void RefreshGameplayChrome()
    {
        OnPropertyChanged(nameof(ShowExpandedCommandHeader));
        OnPropertyChanged(nameof(ShowCompactCommandHeader));
        OnPropertyChanged(nameof(ShowDetailedBattleStatusCard));
        OnPropertyChanged(nameof(CompactHeaderTitle));
        OnPropertyChanged(nameof(CompactHeaderStatus));
        OnPropertyChanged(nameof(GameplayContentPadding));
        OnPropertyChanged(nameof(BoardTitleFontSize));
        OnPropertyChanged(nameof(BoardAxisFontSize));
    }

    private void UpdateBoardRenderMetrics()
    {
        double nextCellSize = BoardCellSize;
        if (Math.Abs(_activeRenderCellSize - nextCellSize) < 0.001)
            return;

        _activeRenderCellSize = nextCellSize;
        OnPropertyChanged(nameof(BoardPixelSize));
        OnPropertyChanged(nameof(BoardFramePixelSize));
        OnPropertyChanged(nameof(CellPixelSize));
        OnPropertyChanged(nameof(AxisRailPixelSize));
        OnPropertyChanged(nameof(BoardRailSpacingPixelSize));
        OnPropertyChanged(nameof(MissPegPixelSize));
        RefreshBoardGeometry();
    }

    private void RefreshBoardGeometry()
    {
        if (IsPlacementPreviewVisible)
            RefreshPlacementPreview();

        foreach (var ship in PlayerShipSprites)
            ship.RefreshGeometry(BoardCellSize);

        foreach (var ship in EnemyShipSprites)
            ship.RefreshGeometry(BoardCellSize);
    }

    private void ConfigureTurnTransition(
        string title,
        string message,
        string? coordinate = null,
        Color? accentColor = null,
        bool isThinkingPrompt = false)
    {
        TurnTransitionTitle = title;
        TurnTransitionMessage = message;
        TurnTransitionCoordinate = coordinate ?? "--";
        IsThinkingPromptActive = isThinkingPrompt;
        if (!isThinkingPrompt)
            ThinkingDots = string.Empty;

        TurnTransitionSpinnerColor = accentColor is Color color
            ? color
            : ResolveThemeColor("GameColorAccent", "#35F4FF");
    }

    private void ResetTurnTransitionPresentation()
    {
        TurnTransitionCoordinate = "--";
        TurnTransitionSpinnerColor = ResolveThemeColor("GameColorAccent", "#35F4FF");
    }

    private async Task ShowLanIntelBubbleAsync(string title, string message)
    {
        if (!IsLanMode || !TurnCinematicsEnabled || ReduceMotionMode)
            return;

        int sequenceId = ++_intelBubbleSequenceId;
        IntelBubbleTitle = title;
        IntelBubbleMessage = message;
        IsIntelBubbleVisible = true;

        try
        {
            await Task.Delay(2200);
        }
        catch
        {
        }

        if (sequenceId != _intelBubbleSequenceId)
            return;

        IsIntelBubbleVisible = false;
    }
}
