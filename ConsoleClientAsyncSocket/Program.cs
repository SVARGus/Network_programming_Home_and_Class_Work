using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Console;

/*
Задание 2
	Разработайте набор консольных приложений. Первое приложение: серверное приложение, 
    которое на запросы клиента возвращает текущее время или дату на сервере. 
    Второе приложение: клиентское приложение, запрашивающее дату или время. 
    Пользователь с клавиатуры определяет, что нужно запросить. 
    После отсылки даты или времени сервер разрывает соединение. 
    Клиентское приложение отображает полученные данные. 

В данном задании было принято решение использовать асинхронные сокеты, так как синхронные уже были реализованы в предыдущем задании 

Клиентская часть
*/

namespace ConsoleClientAsyncSocket
{
    class ClientAsync
    {
        private byte[] buffer = new byte[1024];
        private Socket client;
        private string serverIP;
        private int port;
        private IPEndPoint endP;

        public ClientAsync(string serverIP, int port)
        {
            this.serverIP = serverIP;
            this.port = port;
            endP = new IPEndPoint(IPAddress.Parse(serverIP), port);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }
        
        public void StartClient(string cmd)
        {
            try
            {
                client.BeginConnect(endP, ConnectCallback, cmd);
            }
            catch (SocketException ex)
            {
                WriteLine($"Ошибка сокета: {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteLine($"Общая ошибка: {ex.Message}");
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                client.EndConnect(ar);
                string cmd = (string)ar.AsyncState;
                WriteLine($"Подключено к {client.RemoteEndPoint}");

                byte[] date = Encoding.UTF8.GetBytes(cmd);
                client.BeginSend(date, 0, date.Length, 0, SendCallback, null);
            }
            catch (SocketException ex)
            {
                WriteLine($"Ошибка сокета: {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteLine($"Общая ошибка: {ex.Message}");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                int byteSent = client.EndSend(ar);
                WriteLine($"Отправлено {byteSent} байт. Локальное время: {DateTime.Now::HH:mm:ss}, ожидаю ответ...");

                client.BeginReceive(buffer, 0, buffer.Length, 0, ReceiveCallback, null);
            }
            catch (SocketException ex)
            {
                WriteLine($"Ошибка сокета: {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteLine($"Общая ошибка: {ex.Message}");
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRec = client.EndReceive(ar);
                string resp = Encoding.UTF8.GetString(buffer, 0, bytesRec);
                WriteLine($"Получено от сервера: {resp}. Локальное время: {DateTime.Now::HH:mm:ss}");
            }
            catch (SocketException ex)
            {
                WriteLine($"Ошибка сокета: {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteLine($"Общая ошибка: {ex.Message}");
            }
            
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            WriteLine("Введите \"date\" или \"time\": ");
            string cmd = Console.ReadLine().Trim().ToLower();

            ClientAsync client = new ClientAsync("127.0.0.1", 1024);
            client.StartClient(cmd);

            ReadLine();
        }
    }
}
