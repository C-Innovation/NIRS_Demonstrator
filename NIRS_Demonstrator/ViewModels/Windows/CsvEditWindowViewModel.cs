using Avalonia.Controls;
using NIRS_Demonstrator.Core;
using System;
using System.Collections.ObjectModel;

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
            RefreshData(); 
        }






        #endregion

        #region Private Callbacks

        private void CsvDataGrid_CellPointerPressed(object? sender, Avalonia.Controls.DataGridCellPointerPressedEventArgs e)
        {
            var index = e.Row.Index;
            if (index > _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2)
            {
                _ViewerPage.Nirs1Chart740.HorizontalScroll.Value = index - _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2;
                _ViewerPage.Nirs1Chart850.HorizontalScroll.Value = index - _ViewerPage.Nirs1Chart740.AxisX.AxisSize / 2;
            }
            var items = _CsvEditWindow.CsvDataGrid.SelectedItems;
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

                var items = _CsvEditWindow.CsvDataGrid.SelectedItems;

                // Example if you are mapping data dynamically (like a list/dictionary of cells):
                // string newValue = editedRow.Cells[columnIndex];

                //System.Diagnostics.Debug.WriteLine($"Cell edited at column '{columnName}' (Idx: {columnIndex}). New Value: {newValue}");
            }
        }

        #endregion

        #region Command Methods

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
