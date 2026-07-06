using Avalonia.Controls;
using NIRS_Demonstrator.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NIRS_Demonstrator.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    public class CsvEditWindowViewModel : ViewModelBase
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members
        private ViewerPage _ViewerPage;
        private CsvEditWindow _CsvEditWindow;
        private ObservableCollection<NirsCsvData> _nirsCsvRawData;

        public ObservableCollection<NirsCsvData> NirsCsvRawData
        {
            get => _nirsCsvRawData;
            private set
            {
                _nirsCsvRawData = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Public Properties

        #endregion

        #region Public Commands
        public ICommand SetToOneCommand { get; set; }
        public ICommand SetToZeroCommand { get; set; }
        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public CsvEditWindowViewModel(ViewerPage viewerPage, CsvEditWindow csvEditWindow)
        {
            _ViewerPage = viewerPage;
            _CsvEditWindow = csvEditWindow;
            _CsvEditWindow.CsvDataGrid.CellPointerPressed += CsvDataGrid_CellPointerPressed;
            _CsvEditWindow.CsvDataGrid.CellEditEnded += CsvDataGrid_CellEditEnded;
            _CsvEditWindow.CsvDataGrid.SelectionChanged += CsvDataGrid_SelectionChanged;

            RefreshData();

            SetToOneCommand = new RelayCommand(SetToOneCommandAction);
            SetToZeroCommand = new RelayCommand(SetToZeroCommandAction);

            _ViewerPage.Nirs1Chart740.OnChartAreaDoubleClick += NirsChart_OnChartAreaDoubleClick;
            _ViewerPage.Nirs1Chart850.OnChartAreaDoubleClick += NirsChart_OnChartAreaDoubleClick;
        }

        


        #endregion

        #region Private Callbacks

        private void CsvDataGrid_CellPointerPressed(object? sender, Avalonia.Controls.DataGridCellPointerPressedEventArgs e)
        {
            //var index = e.Row.Index;
            //if (index > _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2)
            //{
            //    _ViewerPage.Nirs1Chart740.HorizontalScroll.Value = index - _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2;
            //    _ViewerPage.Nirs1Chart850.HorizontalScroll.Value = index - _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2;
            //}
            //var items = _CsvEditWindow.CsvDataGrid.SelectedItems;
        }

        private async void CsvDataGrid_CellEditEnded(object? sender, Avalonia.Controls.DataGridCellEditEndedEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
            {
                return;
            }

            if (e.Row.DataContext is NirsCsvData editedRow)
            {
                // 3. Identify which column was edited using its index or header text
                var row = e.Row.Index;
                var col = e.Column.DisplayIndex;
                string? columnName = e.Column.Header?.ToString();

                string newValue = editedRow.ChTotal;
                
                double new_val;
                if (double.TryParse(newValue, out new_val))
                    _ViewerPage.NirsData[col][row] = new_val;

                await _ViewerPage.UpdateSeries(8);

                //var items = _CsvEditWindow.CsvDataGrid.SelectedItems;

                // Example if you are mapping data dynamically (like a list/dictionary of cells):
                // string newValue = editedRow.Cells[columnIndex];

                //System.Diagnostics.Debug.WriteLine($"Cell edited at column '{columnName}' (Idx: {columnIndex}). New Value: {newValue}");
            }
        }

        private void CsvDataGrid_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            //List<NirsCsvData> items = (List<NirsCsvData>)_CsvEditWindow.CsvDataGrid.SelectedItems;
            //if (items == null)
            //    return;
            //if (items.Count == 0)
            //    return;
            //_ViewerPage.Nirs1Series740_TotalVal.SelectionArea.LevelStart = _CsvEditWindow.CsvDataGrid.Inde items[0].


            if (sender is DataGrid dataGrid && dataGrid.ItemsSource is IList<NirsCsvData> sourceCollection)
            {
                var selectedIndexes = dataGrid.SelectedItems
                    .Cast<NirsCsvData>()
                    .Select(item => sourceCollection.IndexOf(item))
                    .Where(index => index >= 0)
                    .ToList();
                if (selectedIndexes.Count == 0)
                    return;
                _ViewerPage.Nirs1Series740_TotalVal.SelectionArea.LevelStart = selectedIndexes[0];
                _ViewerPage.Nirs1Series740_TotalVal.SelectionArea.LevelStop = selectedIndexes[selectedIndexes.Count - 1];

                _ViewerPage.Nirs1Series850_TotalVal.SelectionArea.LevelStart = selectedIndexes[0];
                _ViewerPage.Nirs1Series850_TotalVal.SelectionArea.LevelStop = selectedIndexes[selectedIndexes.Count - 1];

                if (selectedIndexes[0] > _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2)
                {
                    _ViewerPage.Nirs1Chart740.HorizontalScroll.Value = selectedIndexes[0] - _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2;
                    _ViewerPage.Nirs1Chart850.HorizontalScroll.Value = selectedIndexes[0] - _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2;
                }
            }
            
        }

        private void NirsChart_OnChartAreaDoubleClick(object? sender, Avalonia.Point e)
        {
            int index = (int) e.X;
            _CsvEditWindow.CsvDataGrid.SelectedItems.Clear();
            if (_CsvEditWindow.CsvDataGrid.ItemsSource is System.Collections.IList sourceList)
            {
                var targetItem = sourceList[index];
                var targetColumn = _CsvEditWindow.CsvDataGrid.Columns[8];
                _CsvEditWindow.CsvDataGrid.ScrollIntoView(targetItem, targetColumn);
                _CsvEditWindow.CsvDataGrid.SelectedIndex = index;
            }
        }

        #endregion

        #region Command Methods

        private async void SetToOneCommandAction()
        {

            if(_CsvEditWindow.CsvDataGrid.ItemsSource is IList<NirsCsvData> sourceCollection)
            {
                var selectedIndexes = _CsvEditWindow.CsvDataGrid.SelectedItems
                    .Cast<NirsCsvData>()
                    .Select(item => sourceCollection.IndexOf(item))
                    .Where(index => index >= 0)
                    .ToList();

                foreach (var selectedIndex in selectedIndexes)
                {
                    _ViewerPage.NirsData[8][selectedIndex] = 5.0;
                    NirsCsvRawData[selectedIndex].ChTotal = "5.000";
                    
                }
                await _ViewerPage.UpdateSeries(8);
                _CsvEditWindow.CsvDataGrid.SelectedItems.Clear();
                RefreshData();
                await Task.Delay(250);
                if (_CsvEditWindow.CsvDataGrid.ItemsSource is System.Collections.IList sourceList)
                {
                    var targetItem = sourceList[selectedIndexes[selectedIndexes.Count - 1]];
                    var targetColumn = _CsvEditWindow.CsvDataGrid.Columns[8];
                    _CsvEditWindow.CsvDataGrid.ScrollIntoView(targetItem, targetColumn);

                }
            }
            
        }

        private async void SetToZeroCommandAction()
        {

            if (_CsvEditWindow.CsvDataGrid.ItemsSource is IList<NirsCsvData> sourceCollection)
            {
                var selectedIndexes = _CsvEditWindow.CsvDataGrid.SelectedItems
                    .Cast<NirsCsvData>()
                    .Select(item => sourceCollection.IndexOf(item))
                    .Where(index => index >= 0)
                    .ToList();

                foreach (var selectedIndex in selectedIndexes)
                {
                    _ViewerPage.NirsData[8][selectedIndex] = 0.0;
                    NirsCsvRawData[selectedIndex].ChTotal = "0.000";
                    
                }
                await _ViewerPage.UpdateSeries(8);
                _CsvEditWindow.CsvDataGrid.SelectedItems.Clear();
                RefreshData();
                await Task.Delay(250);
                if (_CsvEditWindow.CsvDataGrid.ItemsSource is System.Collections.IList sourceList) {
                    var targetItem = sourceList[selectedIndexes[selectedIndexes.Count - 1]];
                    var targetColumn = _CsvEditWindow.CsvDataGrid.Columns[8];
                    _CsvEditWindow.CsvDataGrid.ScrollIntoView(targetItem, targetColumn);
                    
                }
            }
        }

        private void EditCellByIndex(DataGrid dataGrid, int rowIndex, int columnIndex)
        {
            // 1. Validate indices against the source data bounds
            if (dataGrid.ItemsSource is System.Collections.IList sourceList &&
                rowIndex >= 0 && rowIndex < sourceList.Count &&
                columnIndex >= 0 && columnIndex < dataGrid.Columns.Count)
            {
                // 2. Get the specific row item and column object
                var targetItem = sourceList[rowIndex];
                var targetColumn = dataGrid.Columns[columnIndex];

                // 3. Move selection and focus to the target cell
                dataGrid.SelectedItem = targetItem;
                dataGrid.CurrentColumn = targetColumn;

                // 4. Force the DataGrid to scroll the item into view 
                // (Crucial because virtualization prevents editing off-screen cells)
                dataGrid.ScrollIntoView(targetItem, targetColumn);

                // 5. Trigger the edit mode
                dataGrid.BeginEdit();
            }
        }
        #endregion

        #region Public Methods
        public void RefreshData()
        {
            var newData = GetCsvData();
            NirsCsvRawData = newData; // Это вызовет PropertyChanged и UI перерисуется
        }

        #endregion

        #region Private Methods

        private ObservableCollection<NirsCsvData> GetCsvData()
        {
            ObservableCollection<NirsCsvData> _dat = new ObservableCollection<NirsCsvData>();
            for(int i = 0; i < _ViewerPage.NirsData[0].Count; i++)
            {
                _dat.Add(new NirsCsvData(
                    $"{_ViewerPage.NirsData[0][i]:0.000}",
                    $"{_ViewerPage.NirsData[1][i]:0.000}",
                    $"{_ViewerPage.NirsData[2][i]:0.000}",
                    $"{_ViewerPage.NirsData[3][i]:0.000}",
                    $"{_ViewerPage.NirsData[4][i]:0.000}",
                    $"{_ViewerPage.NirsData[5][i]:0.000}",
                    $"{_ViewerPage.NirsData[6][i]:0.000}",
                    $"{_ViewerPage.NirsData[7][i]:0.000}",
                    $"{_ViewerPage.NirsData[8][i]:0.000}"));
            }
            return _dat;
        }

        #endregion
    }
}
