using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Input;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
        
}

public class SearchViewModel : ViewModel
{
    public enum ESortSizeMode
    {
        None,
        Ascending,
        Descending
    }

    private string _filterText;
    public string FilterText
    {
        get => _filterText;
        set => SetProperty(ref _filterText, value);
    }

    private bool _hasRegexEnabled;
    public bool HasRegexEnabled
    {
        get => _hasRegexEnabled;
        set => SetProperty(ref _hasRegexEnabled, value);
    }

    private bool _hasMatchCaseEnabled;
    public bool HasMatchCaseEnabled
    {
        get => _hasMatchCaseEnabled;
        set => SetProperty(ref _hasMatchCaseEnabled, value);
    }

    private ESortSizeMode _currentSortSizeMode = ESortSizeMode.None;
    public ESortSizeMode CurrentSortSizeMode
    {
        get => _currentSortSizeMode;
        set => SetProperty(ref _currentSortSizeMode, value);
    }

    public void CycleSortSizeMode()
    {
        CurrentSortSizeMode = CurrentSortSizeMode switch
        {
            ESortSizeMode.None => ESortSizeMode.Ascending,
            ESortSizeMode.Ascending => ESortSizeMode.Descending,
            ESortSizeMode.Descending => ESortSizeMode.None,
            _ => ESortSizeMode.None
        };
        
        RefreshFilter();
    }
    
    private RelayCommand? _sortSizeModeCommand;
    public ICommand SortSizeModeCommand => _sortSizeModeCommand ??= new RelayCommand(CycleSortSizeMode);

    public int ResultsCount => SearchResults?.Count ?? 0;
    public RangeObservableCollection<GameFile> SearchResults { get; }
    public ICollectionView SearchResultsView { get; }

    public SearchViewModel()
    {
        SearchResults = new RangeObservableCollection<GameFile>();
        SearchResultsView = new ListCollectionView(SearchResults);
    }

    public void RefreshFilter()
    {
        if (!string.IsNullOrEmpty(FilterText))
            SearchResultsView.Filter = e => ItemFilter(e, FilterText.Trim().Split(' '));
        else
            SearchResultsView.Refresh();

        SearchResultsView.SortDescriptions.Clear();

        if (CurrentSortSizeMode != ESortSizeMode.None)
            SearchResultsView.SortDescriptions.Add(new SortDescription(nameof(GameFile.Size),
                CurrentSortSizeMode == ESortSizeMode.Ascending
                    ? ListSortDirection.Ascending
                    : ListSortDirection.Descending));
    }

    private bool ItemFilter(object item, IEnumerable<string> filters)
    {
        if (item is not GameFile entry)
            return true;

        if (!HasRegexEnabled)
            return filters.All(x => entry.Path.Contains(x, HasMatchCaseEnabled ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase));

        var o = RegexOptions.None;
        if (!HasMatchCaseEnabled) o |= RegexOptions.IgnoreCase;
        return new Regex(FilterText, o).Match(entry.Path).Success;
    }
}