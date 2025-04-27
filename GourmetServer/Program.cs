using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using GourmetRecipe;
using Newtonsoft.Json;
using static System.Console;

namespace GourmetServer
{
    class Program
    {
        static List<Recipe> allRecipes = new List<Recipe>();
        static void Main(string[] args)
        {
            WriteLine("Запуск сервера...");

            LoadRecipes();
            StartServer();

            WriteLine("Нажмите любую клавишу для завершения...");
            ReadKey();
        }

        private static void LoadRecipes(string fileName = "recipes.json")
        {
            try
            {
                if(!File.Exists(fileName))
                {
                    WriteLine($"Файл {fileName} не найден");
                    return;
                }

                string json = File.ReadAllText(fileName);
                allRecipes = JsonConvert.DeserializeObject<List<Recipe>>(json);

                WriteLine($"Загружено рецептов: {allRecipes.Count}");
            }
            catch (Exception ex)
            {
                WriteLine($"Ошибка загрузки рецептов: {ex.Message}");
            }
        }

        private static void StartServer()
        {
            TcpListener listener = null;

            try
            {
                int port = 1025;
                IPAddress localAdress = IPAddress.Parse("127.0.0.1");

                listener = new TcpListener(localAdress, port);
                listener.Start();

                WriteLine($"Сервер запущен на {localAdress}:{port}");

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    WriteLine("Клиент подключился.");

                    Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Error server: {ex.Message}");
            }
            finally
            {
                listener?.Stop();
            }
        }

        private static void HandleClient(TcpClient client)
        {
            try
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    string request = Encoding.Unicode.GetString(buffer, 0, bytesRead);
                    WriteLine($"Запрос от клиента: {request}");

                    List<Recipe> matchedRecipes = FindRecipes(request);

                    string responseJson = JsonConvert.SerializeObject(matchedRecipes);

                    byte[] responseBytes = Encoding.Unicode.GetBytes(responseJson);
                    stream.Write(responseBytes, 0, responseBytes.Length);

                    WriteLine("Ответ отправлен клиенту");
                }
            }
            catch (Exception ex)
            {
                WriteLine($"Error server: {ex.Message}");
            }
            finally
            {
                client?.Close();
                WriteLine("Клиент отключился");
            }
        }

        private static List<Recipe> FindRecipes(string request)
        {
            List<string> searchTherms = request.Split(',')
                .Select(term => term.Trim().ToLower())
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .ToList();

            var result = allRecipes
                .Where(recipe => searchTherms
                .Any(term => recipe.Name.ToLower().Contains(term) ||
                recipe.Ingredients.Any(ingredient => ingredient.ToLower().Contains(term))))
                .ToList();

            return result;
        }
    }
}
