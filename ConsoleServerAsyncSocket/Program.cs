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

Серверная часть
*/

namespace ConsoleServerAsyncSocket
{
    class StateObject
    {
        public Socket workSocket;
        public byte[] buffer = new byte[1024];
        public StringBuilder Sb = new StringBuilder();
    }

    class ServerAsync
    {
        private IPEndPoint endP;
        private Socket listener;


        public ServerAsync(string strAdress, int port)
        {
            endP = new IPEndPoint(IPAddress.Parse(strAdress), port);
        }

        public void Start()
        {
            if (listener != null)
            {
                return;
            }

            try
            {
                listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                listener.Bind(endP);
                listener.Listen(10);

                WriteLine($"Слушаю {endP}...");

                listener.BeginAccept(AcceptCallback, null);
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

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = listener.EndAccept(ar);
                WriteLine($"Подключился {client.RemoteEndPoint}");

                listener.BeginAccept(AcceptCallback, null);

                StateObject state = new StateObject() { workSocket = client };
                client.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, ReceiveCallback, state);
            }
            catch (SocketException ex)
            {
                WriteLine($"Ошибка сокета: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                WriteLine("Сокет был закрыт");
            }
            catch (Exception ex)
            {
                WriteLine($"Общая ошибка: {ex.Message}");
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            var state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;

            try
            {
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.Sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    string cmd = state.Sb.ToString().Trim().ToLower();
                    WriteLine($"Получена команда \"{cmd}\" от {client.RemoteEndPoint}");

                    string reply;
                    if (cmd == "time")
                        reply = DateTime.Now.ToString("HH:mm:ss.fff");
                    else if (cmd == "date")
                        reply = DateTime.Now.ToShortDateString();
                    else
                        reply = "Unknown command";

                    byte[] data = Encoding.UTF8.GetBytes(reply);

                    client.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, client);
                }
                else
                {
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
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

        private void SendCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;

            try
            {
                int sent = client.EndSend(ar);
                WriteLine($"Отправлено {sent} байт клиенту {client.RemoteEndPoint}");

                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (SocketException ex)
            {
                WriteLine($"Ошибка сокета при отправке: {ex.Message}");
            }
            catch (Exception ex)
            {
                WriteLine($"Общая ошибка при отправке: {ex.Message}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var server = new ServerAsync("127.0.0.1", 1024);
            server.Start();
            WriteLine("Нажмите ENTER для завершения...");
            ReadLine();
        }
    }
}
