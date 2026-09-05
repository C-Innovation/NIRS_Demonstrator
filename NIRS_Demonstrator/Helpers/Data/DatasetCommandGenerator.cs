using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class DatasetCommandGenerator
    {
        /// <summary>
        /// Генерирует команду для сборки датасета и сохраняет её в текстовый файл.
        /// </summary>
        /// <param name="folderPath">Путь к папке с CSV файлами.</param>
        /// <param name="outputFileName">Имя выходного текстового файла (по умолчанию build_command.txt).</param>
        public static void GenerateBuildCommand(string folderPath, string outputFileName = "build_command.txt")
        {
            // Проверяем, существует ли папка
            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException($"Указанная папка не найдена: {folderPath}");
            }

            // Получаем все CSV файлы, берем только их имена и сортируем по убыванию 
            // (чтобы новые файлы, у которых время в имени больше, шли первыми, как в примере)
            var csvFiles = Directory.GetFiles(folderPath, "*.csv")
                                    .Select(Path.GetFileName)
                                    .OrderByDescending(f => f)
                                    .ToList();

            if (!csvFiles.Any())
            {
                Console.WriteLine("Внимание: в указанной папке не найдено ни одного CSV файла.");
                return;
            }

            var commandBuilder = new StringBuilder();

            // Начало команды
            commandBuilder.Append("python 01_build_dataset.py --target-vmax 5.0 ");

            // Добавляем каждый CSV файл с префиксом _dataset/ (как в вашем примере)
            foreach (var file in csvFiles)
            {
                commandBuilder.Append($"--csv _dataset/{file} ");
            }

            // Завершение команды
            // Обратите внимание на экранирование кавычек \" для пути с пробелом
            commandBuilder.Append("--core-features \"../bin/Release/net8.0/NirsTool.exe run\" ");
            commandBuilder.Append("--out _dataset/dataset.npz");

            // Формируем путь для сохранения текстового файла
            string outputPath = Path.Combine(folderPath, outputFileName);

            // Записываем результат в файл
            File.WriteAllText(outputPath, commandBuilder.ToString());

            Console.WriteLine($"Команда успешно сгенерирована и сохранена в файл:\n{outputPath}");
        }
    
    }
}
