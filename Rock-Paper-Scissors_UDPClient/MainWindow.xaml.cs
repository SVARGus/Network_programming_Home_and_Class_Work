using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/*
Задание 1
	Создайте клиент-серверное приложение для игры в «Камень-Ножницы-Бумага».
	Можно реализовать любую версию игры. Например, «Камень-Ножницы-Бумага-Ящерица-Спок».
	Клиентское приложение подключается к серверному. Если подключение успешно, можно начинать игру. 
	Каждая из игр состоит из пяти раундов. В каждом из раундов игроки одновременно выбирают какую-то фигуру. 
	Возможные результаты игры: ничья, победа любого из пользователей. 
	У каждого из игроков должно отображаться текущее состояние дел в игре. Итог партии показывается у каждого игрока. 
	После завершения игры происходит отсоединение клиента
Задание 2
	Добавьте к первому заданию возможность игры в следующих форматах:
	■ Человек-компьютер;
	■ Компьютер-компьютер;
	■ Человек-человек.
Режим должен отображаться на экране во время игры
Задание 3
	Добавьте к первому заданию возможность преждевременного завершения игры. Игрок во время своего хода может:
	■ Совершить ход;
	■ Предложить ничью;
	■ Признать поражение
Задание 4*
	Добавьте к первому заданию возможность матча. Матч состоит из определенного количества игр. 
	Например, в матче может быть три игры. Каждая из игр состоит из пяти раундов. 
	Возможные результаты матча: ничья, победа любого из пользователей. По итогам каждой игры должна отображаться статистика:
	■ Фигуры, которые были выбраны пользователями в раунде;
	■ Результат конкретного тура;
	■ Результат игры;
	■ Длительность игры.
По итогам матча должна отображаться статистика:
	■ Результат каждой игры;
	■ Результат матча;
	■ Самая популярная фигура;
	■ Самая непопулярная фигура
*/

namespace Rock_Paper_Scissors_UDPClient
{
    public partial class MainWindow : Window
    {
        private IPAddress RemoteIpAddress;
        private int RemotePort;
        private int LocalPort;
        private bool IsHuman;
        private bool settingsProposalSent;
        private bool settingsProposalReceived;
        private bool settingsAckReceived;

        private int matchCount;
        private int gameCount;

        private TotalResult totalResult;
        

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxRole.SelectedIndex == 0)
            {
                IsHuman = false;
            }
            else if (ComboBoxRole.SelectedIndex == 1)
            {
                IsHuman = true;
            }
            else
            {
                MessageBox.Show("Error",
                    "Выберите роль: Компьютер/Человек",
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(TextBoxLocalPort.Text, out LocalPort) || !int.TryParse(TextBoxRemotePort.Text, out RemotePort))
            {
                MessageBox.Show("Некорректно указаны порты.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                RemoteIpAddress = IPAddress.Parse(TextBoxServerIP.Text);

                Thread threadReceive = new Thread(new ThreadStart(ReceiveDate));
                threadReceive.IsBackground = true;
                threadReceive.Start();
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"Conresion is not possible: {ex.Message}");
            }
            catch (Exception ex) 
            { 
                MessageBox.Show($"Error: {ex.Message}"); 
            }

            ButtonStart.IsEnabled = false;
            TextBoxServerIP.IsEnabled = false;
            TextBoxRemotePort.IsEnabled = false;
            TextBoxLocalPort.IsEnabled = false;
            ComboBoxRole.IsEnabled = false;

            TextBoxMatch.IsEnabled = true;
            TextBoxGame.IsEnabled = true;
            ButtonGameSettings.IsEnabled = true;

            if (!IsHuman)
            {
                matchCount = 5;
                gameCount = 3;
                settingsProposalSent = true;
                settingsProposalReceived = true;
                settingsAckReceived = true;
                OnSettingsFinalized();
            }
        }

        private void OnSettingsFinalized()
        {
            TextBoxMatch.IsEnabled = false;
            TextBoxGame.IsEnabled = false;
            ButtonGameSettings.IsEnabled = false;

            ButtonRock.IsEnabled = true;
            ButtonPaper.IsEnabled = true;
            ButtonScissors.IsEnabled = true;
            ButtonDrawnGame.IsEnabled = true;
            ButtonDefeatGame.IsEnabled = true;

            TextBlockMode.Text = $"Модель: {(IsHuman ? "Человек" : "Компьютер")} – {(IsHuman ? "Компьютер" : "Человек")}";
        }

        private void ButtonRock_Click(object sender, RoutedEventArgs e)
        {
            SendMove(MoveType.Rock);
        }

        private void ButtonPaper_Click(object sender, RoutedEventArgs e)
        {
            SendMove(MoveType.Paper);
        }

        private void ButtonScissors_Click(object sender, RoutedEventArgs e)
        {
            SendMove(MoveType.Scissors);
        }

        private void SendMove(MoveType move)
        {
            try
            {
                var movePayload = new MovePayload()
                {
                    Move = move,
                    OfferDraw = false,
                    Resing = false
                };

                var packet = new UdpPacket<MovePayload>()
                {
                    Payload = movePayload,
                    Type = PacketType.MoveSubmission,
                    GameNamber = 1, // Надо продумать метод просчета порядок игр
                    RoundNumber = 1 // Надо продумать метод просчета порядок раунда
                };

                SendDate(packet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке хода: {ex.Message}");
            }
        }

        private void ButtonDrawnGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var movePayload = new MovePayload()
                {
                    Move = null,
                    OfferDraw = true,
                    Resing = false
                };

                var packet = new UdpPacket<MovePayload>()
                {
                    Payload = movePayload,
                    Type = PacketType.MoveSubmission,
                    GameNamber = 1, // Надо продумать метод просчета порядок игр
                    RoundNumber = 1 // Надо продумать метод просчета порядок раунда
                };

                SendDate(packet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке хода: {ex.Message}");
            }
        }

        private void ButtonDefeatGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var movePayload = new MovePayload()
                {
                    Move = null,
                    OfferDraw = false,
                    Resing = true
                };

                var packet = new UdpPacket<MovePayload>()
                {
                    Payload = movePayload,
                    Type = PacketType.MoveSubmission,
                    GameNamber = 1, // Надо продумать метод просчета порядок игр
                    RoundNumber = 1 // Надо продумать метод просчета порядок раунда
                };

                SendDate(packet);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке хода: {ex.Message}");
            }
        }

        private void ButtonGameSettings_Click(object sender, RoutedEventArgs e)
        {
            bool isValid = true;

            if (string.IsNullOrWhiteSpace(TextBoxMatch.Text))
            {
                MessageBox.Show("Поле \"Количество матчей\" не может быть пустым!",
                    "Ошибка",
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                isValid = false;
            }
            else if (!int.TryParse(TextBoxMatch.Text, out matchCount) || matchCount <= 0)
            {
                MessageBox.Show("Введите корректное число матчей (целое число больше 0)!", 
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(TextBoxGame.Text))
            {
                MessageBox.Show("Поле \"Количество игр\" не может быть пустым!", 
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                isValid = false;
            }
            else if (!int.TryParse(TextBoxGame.Text, out gameCount) || gameCount <= 0)
            {
                MessageBox.Show("Введите корректное число игр (целое число больше 0)!", 
                    "Ошибка", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                isValid = false;
            }

            if (!isValid)
            {
                return;
            }

            try
            {
                SettingsPayload settingsPayload = new SettingsPayload()
                {
                    TotalGame = gameCount,
                    MatchPerGame = matchCount
                };

                UdpPacket<SettingsPayload> udpPacket = new UdpPacket<SettingsPayload>()
                {
                    Payload = settingsPayload,
                    Type = PacketType.SettingsProposal,
                    //MarchId = Guid.NewGuid(),
                    GameNamber = 1,
                    RoundNumber = 1
                };

                SendDate(udpPacket);
                settingsProposalSent = true;
                TryFinalizeSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке настроек: {ex.Message}",
                      "Ошибка",
                      MessageBoxButton.OK,
                      MessageBoxImage.Error);
            }
        }

        private void ButtonResults_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ReceiveDate()
        {
            try
            {
                using (UdpClient udpClient = new UdpClient(LocalPort))
                {
                    while (true)
                    {
                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        var date = udpClient.Receive(ref remoteEP);
                        var json = Encoding.UTF8.GetString(date);

                        var wrapper = JsonConvert.DeserializeObject<UdpPacket<JObject>>(json);
                        Dispatcher.Invoke(() => ProcessPacket(wrapper));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void ProcessPacket(UdpPacket<JObject> wrapper)
        {
            switch (wrapper.Type)
            {
                case PacketType.SettingsProposal:
                    var s = wrapper.Payload.ToObject<SettingsPayload>();
                    matchCount = s.MatchPerGame;
                    gameCount = s.TotalGame;
                    settingsProposalReceived = true;

                    if (IsHuman)
                    {
                        var res = MessageBox.Show(
                            $"Оппонент предлагает:\n" +
                            $"- Матчей: {matchCount}\n" +
                            $"- Игр:   {gameCount}\n\n" +
                            "Принять?",
                            "Настройки",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        var ack = new SettingsAckPayload { Accepted = (res == MessageBoxResult.Yes) };
                        SendDate(new UdpPacket<SettingsAckPayload>
                        {
                            Type = PacketType.SettingsAck,
                            Payload = ack,
                            GameNamber = 1,
                            RoundNumber = 1
                        });
                        if (res == MessageBoxResult.No)
                        {
                            settingsProposalSent = settingsProposalReceived = settingsAckReceived = false;
                        }
                    }
                    else
                    {
                        SendDate(new UdpPacket<SettingsAckPayload>
                        {
                            Type = PacketType.SettingsAck,
                            Payload = new SettingsAckPayload { Accepted = true },
                            GameNamber = 1,
                            RoundNumber = 1
                        });
                    }
                    TryFinalizeSettings();
                    break;

                case PacketType.SettingsAck:
                    var ackPayload = wrapper.Payload.ToObject<SettingsAckPayload>();
                    if (ackPayload.Accepted)
                        settingsAckReceived = true;
                    else
                    {
                        settingsProposalSent = settingsProposalReceived = settingsAckReceived = false;
                    }
                    TryFinalizeSettings();
                    break;

                //здесь будут кейсы для MoveSubmission, MoveAck, GameResult и т.д.

                default:
                    break;
            }
        }

        private void SendDate<T>(T udpPacket)
        {
            try
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    IPEndPoint remoteEndPoint = new IPEndPoint(RemoteIpAddress, RemotePort);
                    var json = JsonConvert.SerializeObject(udpPacket);
                    var date = Encoding.UTF8.GetBytes(json);
                    udpClient.Send(date, date.Length, remoteEndPoint);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке данных: {ex.Message}");
            }
        }

        private void TryFinalizeSettings()
        {
            if (settingsProposalSent && settingsProposalReceived && settingsAckReceived)
            {
                OnSettingsFinalized();
            }
        }


    }
}
