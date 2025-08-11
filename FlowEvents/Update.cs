using Newtonsoft.Json;
using Squirrel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

// Класс обеспечивающий процесс обновления приложения

namespace FlowEvents
{
    internal class Update
    {
    }

    public class RepositoryData
    {
        public string Repository { get; set; }
    }

    // Класс получения пути из файла json (конфигурация пути к источнику обновления)
    internal class Repository
    {
        //private readonly string _repositoryFilePath;

        ////Конструктор принимает путь к JSON-файлу
        //public Repository(string repositoryFilePath)
        //{
        //    _repositoryFilePath = repositoryFilePath;
        //}

        //Метод для чтения информации о конфигурации из JSON-файла
        //public RepositoryData GetConfig()
        //{
        //    try
        //    {
        //        //Проверяем, существует ли файл
        //        if (File.Exists(_repositoryFilePath))
        //        {
        //            string jsonContent = File.ReadAllText(_repositoryFilePath); //Читаем содержимое JSON-файла
        //            jsonContent = jsonContent.Replace(@"\", @"\\");
        //            RepositoryData repository = JsonConvert.DeserializeObject<RepositoryData>(jsonContent); //Десериализация JSON
        //            string repositoryPath = repository?.Repository;
        //            return repository;
        //        }
        //        else
        //        {
        //            throw new FileNotFoundException("Файл не найден: " + _repositoryFilePath);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Ошибка при чтении версии: " + ex.Message);
        //    }
        //}
    }



    //-----------------------------------------------
    //Класс обновления 
    //-----------------------------------------------


    public class Updater
    {
        public static async Task CheckForUpdateAsync(string updatePath)
        {
            try
            {
                using (var mgr = new Squirrel.UpdateManager(updatePath))
                {
                    var updateInfo = await mgr.CheckForUpdate();

                    if (updateInfo.ReleasesToApply.Count > 0)
                    {
                        var result = MessageBox.Show(
                            "Найдены обновления для приложения. Хотите обновить сейчас?\n\nВерсия: " +
                            updateInfo.FutureReleaseEntry.Version,
                            "Доступно обновление",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                // Асинхронное обновление с прогрессом
                                await mgr.UpdateApp();

                                MessageBox.Show(
                                    "Приложение успешно обновлено до версии " +
                                    updateInfo.FutureReleaseEntry.Version +
                                    "\n\nПриложение будет перезапущено автоматически.",
                                    "Обновление завершено",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);

                                // Правильный перезапуск приложения
                                RestartApplication();
                            }
                            finally
                            {

                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "У вас установлена последняя версия приложения.",
                            "Обновлений нет",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при проверке обновлений:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void RestartApplication()
        {
            // Получаем путь к текущему исполняемому файлу
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            // Запускаем новую копию приложения
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Arguments = "/updated" // Можно передать флаг обновления
            });

            // Закрываем текущее приложение
            Application.Current.Shutdown();
        }
    }
}
