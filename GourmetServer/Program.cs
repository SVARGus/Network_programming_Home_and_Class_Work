using System;
using System.Collections.Concurrent;
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
        static ConcurrentDictionary<int, List<RequestData>> clientRequests = new ConcurrentDictionary<int, List<RequestData>>();
        static int lastUserId = 0;
        static object idLock = new object();
        static void Main(string[] args)
        {
            Log("Запуск сервера...");

            LoadRecipes();
            StartServer();

            Log("Нажмите любую клавишу для завершения...");
            ReadKey();
            SaveUserId();
        }

        private static void LoadRecipes(string fileName = "recipes.json")
        {
            try
            {
                if (!File.Exists("serveruserid.txt"))
                {
                    Log($"Файл \"serveruserid.txt\" не найден. Будет создан новый при первом сохранении.");
                }
                else
                {
                    string content = File.ReadAllText("serveruserid.txt");
                    if (int.TryParse(content, out int loadedId))
                    {
                        lastUserId = loadedId;
                        Log($"Загружен последний UserId: {lastUserId}");
                    }
                    else
                    {
                        Log($"Неверный формат в файле \"serveruserid.txt\": \"{content}\"");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка загрузки lastUserId: {ex.Message}");
            }

            try
            {
                if (!File.Exists(fileName))
                {
                    Log($"Файл {fileName} не найден");
                }
                else
                {
                    string json = File.ReadAllText(fileName);
                    allRecipes = JsonConvert.DeserializeObject<List<Recipe>>(json);

                    Log($"Загружено рецептов: {allRecipes.Count}");
                }
            }
            catch (Exception ex)
            {
                Log($"Ошибка загрузки рецептов: {ex.Message}");
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

                Log($"Сервер запущен на {localAdress}:{port}");

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Log("Клиент подключился.");

                    Task.Run(() => HandleClient(client));
                }
            }
            catch (Exception ex)
            {
                Log($"Error server: {ex.Message}");
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

                    string requestJson = Encoding.Unicode.GetString(buffer, 0, bytesRead);
                    var request = JsonConvert.DeserializeObject<ClientRequest>(requestJson);
                    Log($"От клиента #{request.IdUser} поступил запрос: {request.Quary}");

                    ServerResponse response = ProcessRequest(request);

                    string responseJson = JsonConvert.SerializeObject(response);

                    byte[] responseBytes = Encoding.Unicode.GetBytes(responseJson);
                    stream.Write(responseBytes, 0, responseBytes.Length);

                    Log($"Ответ отправлен клиенту #{response.IdUser}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error server: {ex.Message}");
            }
            finally
            {
                client?.Close();
                Log("Клиент отключился");
            }
        }

        private static ServerResponse ProcessRequest(ClientRequest request)
        {
            try
            {
                int userId = request.IdUser;
                bool isNewUser = false;

                if (userId == 0)
                {
                    lock (idLock)
                    {
                        userId = ++lastUserId;
                        isNewUser = true;
                    }
                }

                if (!clientRequests.TryGetValue(userId, out var requests))
                {
                    requests = new List<RequestData>();
                    clientRequests[userId] = requests;
                }

                requests.RemoveAll(r => DateTime.Now - r.RequestTime > TimeSpan.FromHours(1));

                if (requests.Count >= 10)
                {
                    return new ServerResponse { IdUser = userId, Error = "Достигнут лимит запросов (10 в 1 час)" };
                }

                requests.Add(new RequestData { RequestTime = DateTime.Now, Query = request.Quary });

                List<Recipe> recipes = FindRecipes(request.Quary);
                if (isNewUser)
                {
                    Log($"Зарегестрирован новый пользователь: {userId}");
                }

                return new ServerResponse { IdUser = userId, Recipes = recipes };

            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return new ServerResponse { Error = ex.Message };
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

        private static void Log(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            File.AppendAllText("server.log", logEntry + Environment.NewLine);
            WriteLine(logEntry);
        }

        private static void SaveUserId()
        {
            File.WriteAllText("serveruserid.txt", lastUserId.ToString());
        }
    }
}
