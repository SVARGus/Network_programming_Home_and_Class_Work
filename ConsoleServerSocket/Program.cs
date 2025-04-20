using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Console;

/*
Практическое задание (на уроке не успели реализовать), ушло как домашнее задание
Задание 1
	Разработайте два консольных приложения, использующих сокеты. Одно приложение – сервер, второе - клиент. 
    Клиентское приложение посылает приветствие серверу. Сервер отвечает. И клиент, и сервер отображают полученное сообщение. 
    Пример вывода:
	Сервер: В 10:25 от [IP-адрес] получена строка: Привет, сервер!
	Клиент: В 10:26 от [IP-адрес] получена строка: Привет, клиент! 
    Используйте механизм синхронных сокетов

Реализация серверной части
*/

namespace ConsoleServerSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            const int port = 1024;
            string serverIP = "127.0.0.1";
            IPAddress ip = IPAddress.Parse(serverIP);
            IPEndPoint ep = new IPEndPoint(ip, port);
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            listener.Bind(ep);
            listener.Listen(1);
            WriteLine("Ожидание подключения клиента");

            try
            {
                while (true)
                {
                    Socket handler = listener.Accept();

                    byte[] buffer = new byte[4096];
                    int bytesRec = handler.Receive(buffer);
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRec);

                    string clientIPAdress = ((IPEndPoint)handler.RemoteEndPoint).Address.ToString();
                    WriteLine($"Сервер: в {DateTime.Now:HH:mm} от {clientIPAdress} получена строка: {data}");

                    string reply = "Привет, клиент!";
                    byte[] message = Encoding.UTF8.GetBytes(reply);
                    handler.Send(message);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
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
    }
}
