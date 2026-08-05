using Avalonia;
using NIRS_Demonstrator.Core;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace NIRS_Demonstrator.ViewModels
{
    /// <summary>
    ///
    /// </summary>
    public class ViewerPageViewModel : ViewModelBase
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members

        private readonly List<ChartSelectionArea> _SelectionAreas = new List<ChartSelectionArea>();
        private readonly List<List<List<Point>>> _SelectedPoints = new List<List<List<Point>>>();

        #endregion

        #region Public Properties

        /// <summary>
        /// Выделенные области в порядке их создания.
        /// </summary>
        public IReadOnlyList<ChartSelectionArea> SelectionAreas => _SelectionAreas;

        /// <summary>
        /// Данные выделенных областей в реальных значениях:
        /// область -> колонка csv -> точки, где X — индекс строки NirsData,
        /// а Y — значение. Порядок колонок совпадает с порядком колонок в файле;
        /// серии Ai сюда не попадают, они не сохраняются в csv.
        /// </summary>
        public IReadOnlyList<List<List<Point>>> SelectedPoints => _SelectedPoints;

        /// <summary>
        /// Есть ли хотя бы одна выделенная область.
        /// </summary>
        public bool HasSelection => _SelectionAreas.Count > 0;

        #endregion

        #region Public Commands
        public ICommand ReturnToMainCommand { get; set; }

        /// <summary>
        /// Снять все выделения сразу на обоих графиках.
        /// </summary>
        public ICommand CancelSelectionCommand { get; set; }

        /// <summary>
        /// Снять одну выделенную область на обоих графиках. Параметр — её индекс.
        /// </summary>
        public ICommand RemoveSelectionAreaCommand { get; set; }
        #endregion

        #region Public Events

        /// <summary>
        /// Все выделения сброшены — представление должно убрать их с графиков.
        /// </summary>
        public event EventHandler SelectionCleared;

        /// <summary>
        /// Снята одна выделенная область — представление должно убрать её
        /// с обоих графиков. Передаётся индекс области.
        /// </summary>
        public event EventHandler<int> SelectionAreaRemoved;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public ViewerPageViewModel()
        {
            ReturnToMainCommand = new RelayCommand(ReturnToMainCommandAction);
            CancelSelectionCommand = new RelayCommand(CancelSelectionCommandAction);
            RemoveSelectionAreaCommand = new RelayParameterizedCommand(RemoveSelectionAreaCommandAction);

            AppConfig.GetInstance().RegisterDisposableObject(this);
        }

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        private void ReturnToMainCommandAction()
        {
            IoC.Application.GoToPage(ApplicationPage.Main);
        }

        private void CancelSelectionCommandAction()
        {
            ClearSelectionAreas();
        }

        private void RemoveSelectionAreaCommandAction(object parameter)
        {
            if (parameter is int index)
                RemoveSelectionArea(index);
            else if (parameter is ChartSelectionArea area)
                RemoveSelectionArea(_SelectionAreas.IndexOf(area));
        }

        #endregion



        #region Public Methods

        /// <summary>
        /// Добавляет выделенную область.
        /// </summary>
        public void AddSelectionArea(ChartSelectionArea area)
        {
            if (area == null)
                return;

            _SelectionAreas.Add(area);
            _SelectedPoints.Add(area.ColumnPoints);

            NotifySelectionChanged();
        }

        /// <summary>
        /// Снимает одну выделенную область по её индексу.
        /// </summary>
        public void RemoveSelectionArea(int index)
        {
            if ((uint)index >= (uint)_SelectionAreas.Count)
                return;

            _SelectionAreas.RemoveAt(index);
            _SelectedPoints.RemoveAt(index);

            NotifySelectionChanged();

            SelectionAreaRemoved?.Invoke(this, index);
        }

        /// <summary>
        /// Сбрасывает все выделенные области и сообщает об этом представлению.
        /// </summary>
        public void ClearSelectionAreas()
        {
            _SelectionAreas.Clear();
            _SelectedPoints.Clear();

            NotifySelectionChanged();

            SelectionCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Пересобирает значения всех выделенных областей из NirsData.
        /// Вызывается после редактирования данных, чтобы выделения не остались
        /// со старыми значениями.
        /// </summary>
        public void RefreshSelectionData(IReadOnlyList<List<double>> nirsData)
        {
            if (_SelectionAreas.Count == 0)
                return;

            foreach (ChartSelectionArea area in _SelectionAreas)
                area.Rebuild(nirsData);

            OnPropertyChanged(nameof(SelectedPoints));
        }

        /// <summary>
        /// Диапазоны строк NirsData, попавшие в выделения: First — первая строка,
        /// Second — последняя, обе включительно.
        /// </summary>
        public List<Pair<int, int>> GetSelectedRowRanges()
        {
            List<Pair<int, int>> ranges = new List<Pair<int, int>>(_SelectionAreas.Count);

            foreach (ChartSelectionArea area in _SelectionAreas)
            {
                if (!area.IsEmpty)
                    ranges.Add(new Pair<int, int>(area.StartIndex, area.StopIndex));
            }

            return ranges;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        #endregion

        #region Private Methods

        private void NotifySelectionChanged()
        {
            OnPropertyChanged(nameof(SelectedPoints));
            OnPropertyChanged(nameof(SelectionAreas));
            OnPropertyChanged(nameof(HasSelection));
        }

        #endregion
    }
}
