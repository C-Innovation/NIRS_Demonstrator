
using System.Threading.Tasks;
using System.Windows;

namespace NIRS_Demonstrator.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        //Task ShowMessage(MessageBoxDialogViewModel viewModel);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        //Task<MessageBoxResult> ShowMessageForResult(MessageBoxDialogViewModel viewModel);

        //Task<MessageBoxResult> ShowCreateGroupDialog(CreateGroupDialogViewModel viewModel);

        //Task<MessageBoxResult> ShowAddEditOrWordDialog(AddWordDialogViewModel viewModel);
        //Task<MessageBoxResult> ShowExamResultsDialog(ExamResultsDialogViewModel viewModel);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        //Task ShowDialogStimulParameters(StimulParametersDialogViewModel viewModel);
    }

    public enum MessageBoxResult
    {
        //
        // Сводка:
        //     Окно сообщения не возвращает никаких результатов.
        None = 0,
        //
        // Сводка:
        //     Полученное значение окна сообщения — ОК.
        OK = 1,
        //
        // Сводка:
        //     Полученное значение окна сообщения — Отменить.
        Cancel = 2,
        //
        // Сводка:
        //     Полученное значение окна сообщения — Да.
        Yes = 6,
        //
        // Сводка:
        //     Полученное значение окна сообщения — Нет.
        No = 7
    }
}
