using MauiBattleship.Core.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Maui.ApplicationModel;

namespace MauiBattleship.Views;

public partial class GamePage : ContentPage
{
    private readonly IFleetService _fleet;
    private GameViewModel? _viewModel;

    public GamePage(IFleetService fleet)
    {
        InitializeComponent();
        _fleet = fleet;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        var lines = _fleet.DemoInterfaces();
        Output.Text = string.Join(Environment.NewLine, lines);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameViewModel.PlayerCells) ||
            e.PropertyName == nameof(GameViewModel.ComputerCells))
        {
            // Rebuild the grids if cells collection changes
            MainThread.BeginInvokeOnMainThread(InitializeBoards);
        }
    }

    private void InitializeBoards()
    {
        if (_viewModel == null) return;

        BuildBoard(PlayerBoardGrid, _viewModel.PlayerCells, isPlayerBoard: true);
        BuildBoard(ComputerBoardGrid, _viewModel.ComputerCells, isPlayerBoard: false);
    }

    private void BuildBoard(Grid grid, IEnumerable<CellViewModel> cells, bool isPlayerBoard)
    {
        grid.Children.Clear();
        grid.RowDefinitions.Clear();
        grid.ColumnDefinitions.Clear();

        for (int i = 0; i < GameBoard.BoardSize; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(CellSize));
            grid.ColumnDefinitions.Add(new ColumnDefinition(CellSize));
        }

        foreach (var cellVm in cells)
        {
            var button = CreateCellButton(cellVm, isPlayerBoard);
            grid.Add(button, cellVm.Column, cellVm.Row);
        }
    }

    private Button CreateCellButton(CellViewModel cellVm, bool isPlayerBoard)
    {
        var button = new Button
        {
            WidthRequest = CellSize,
            HeightRequest = CellSize,
            CornerRadius = 2,
            Padding = 0,
            Margin = 0,
            FontSize = 14,
            BorderWidth = 0
        };

        button.SetBinding(Button.BackgroundColorProperty, new Binding(nameof(CellViewModel.CellColor), source: cellVm));
        button.SetBinding(Button.TextProperty, new Binding(nameof(CellViewModel.CellText), source: cellVm));

        button.Clicked += (s, e) =>
        {
            if (_viewModel == null) return;

            if (isPlayerBoard)
            {
                _viewModel.PlayerBoardClickCommand.Execute(cellVm);
            }
            else
            {
                _viewModel.ComputerBoardClickCommand.Execute(cellVm);
            }
        };

        return button;
    }
}
