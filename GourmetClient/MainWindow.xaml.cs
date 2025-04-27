using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GourmetRecipe;
using Newtonsoft.Json;

/*
Задание 1
	Создайте серверное приложение, с помощью которого можно узнавать кулинарные рецепты. Типичный пример работы
	■ клиентское приложение подключается к серверу;
	■ клиентское приложение посылает запрос с указанием списка продуктов;
	■ сервер возвращает рецепты, содержащие указанные продукты;
	■ клиент может послать новый запрос или отключиться.
Задание 2
	Добавьте к первому заданию ограничение по количеству запросов для конкретного клиента за определенный промежуток времени. 
    Например, клиент не может послать больше, чем 10 запросов за час
Задание 3
	Добавьте механизм логгирования в сервер. Этот механизм должны сохранять информацию о клиентах, их запросах, времени соединения и т.д. 
*/

namespace GourmetClient
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        NetworkStream stream;
        IPAddress adressIP = IPAddress.Parse("127.0.0.1");
        int port = 1025;

        public List<Recipe> Recipes {  get; set; } = new List<Recipe>();
        public Recipe SelectedRecipe { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(SearchTextBox.Text))
            {
                MessageBox.Show("Укажите данные для поиска",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return;
            }

            await SendRequestAsync(SearchTextBox.Text.Trim());
        }

        private async Task SendRequestAsync(string query)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(adressIP, port);

                stream = client.GetStream();

                byte[] buffer = Encoding.Unicode.GetBytes(query);
                await stream.WriteAsync(buffer, 0, buffer.Length);

                await ReceiveResponseAsync();
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                client?.Close();
            }
        }

        private async Task ReceiveResponseAsync()
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;

                    do
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                        }
                    }
                    while (stream.DataAvailable);

                    string response = Encoding.Unicode.GetString(ms.ToArray());

                    ProcessServerResponse(response);
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void ProcessServerResponse(string response)
        {
            try
            {
                Recipes.Clear();

                if (!string.IsNullOrWhiteSpace(response))
                {
                    var recipesFromServer = JsonConvert.DeserializeObject<List<Recipe>>(response);

                    if (recipesFromServer != null)
                    {
                        Recipes.AddRange(recipesFromServer);
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    RecipesListBox.ItemsSource = null;
                    RecipesListBox.ItemsSource = Recipes;
                });
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void RecipeListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selectedRecipe = RecipesListBox.SelectedItem as Recipe;

            if (selectedRecipe != null)
            {
                RecipePanel.DataContext = selectedRecipe;
            }
            else
            {
                RecipePanel.DataContext = null;
            }
        }
    }
}
