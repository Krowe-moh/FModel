using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;
using CUE4Parse.FileProvider.Objects;
using FModel.Framework;

namespace FModel.ViewModels;

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

    public int ResultsCount => SearchResults?.Count ?? 0;
    public RangeObservableCollection<GameFile> SearchResults { get; }
    public ICollectionView SearchResultsView { get; }

    public SearchViewModel()
    {
        SearchResults = new RangeObservableCollection<GameFile>();
        SearchResultsView = new ListCollectionView(SearchResults)
        {
            Filter = e => ItemFilter(e, FilterText?.Trim().Split(' ') ?? []),
        };
    }

    public void RefreshFilter()
    {
        SearchResultsView.Refresh();
    }

    public void CycleSortSizeMode()
    {
        CurrentSortSizeMode = CurrentSortSizeMode switch
        {
            ESortSizeMode.None => ESortSizeMode.Descending,
            ESortSizeMode.Descending => ESortSizeMode.Ascending,
            _ => ESortSizeMode.None
        };

        using (SearchResultsView.DeferRefresh())
        {
            SearchResultsView.SortDescriptions.Clear();
            if (CurrentSortSizeMode != ESortSizeMode.None)
            {
                var sort = CurrentSortSizeMode == ESortSizeMode.Ascending
                    ? ListSortDirection.Ascending
                    : ListSortDirection.Descending;

                SearchResultsView.SortDescriptions.Add(new SortDescription(nameof(GameFile.Size), sort));
            }
        }
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
