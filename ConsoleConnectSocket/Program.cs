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

Реализация клиентской части
*/

namespace ConsoleConnectSocket
{
    class Program
    {
        static void Main(string[] args)
        {
            const int port = 1024;
            string serverIP = "127.0.0.1";
            IPAddress ip = IPAddress.Parse(serverIP);
            IPEndPoint ep = new IPEndPoint(ip, port);
            Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                sender.Connect(ep);
                if (sender.Connected)
                {
                    string message = "Привет, Сервер!";
                    sender.Send(Encoding.UTF8.GetBytes(message));

                    byte[] buffer = new byte[4096];
                    int bytesRec = sender.Receive(buffer);
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRec);

                    string serverIPAddress = ((IPEndPoint)sender.RemoteEndPoint).Address.ToString();
                    WriteLine($"Клиент: в {DateTime.Now:HH:mm} от {serverIPAddress} получена строка: {data}");
                }
                else
                {
                    WriteLine("Error");
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
            finally
            {
                if (sender != null )
                {
                    if (sender.Connected)
                    {
                        sender.Shutdown(SocketShutdown.Both);
                    }
                    sender.Close();
                    sender.Dispose();
                }                
            }
            Read();
        }
    }
}
