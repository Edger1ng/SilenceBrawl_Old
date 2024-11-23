namespace Supercell.Laser.Server.Networking
{
    using Supercell.Laser.Server.Networking.Session;
    using System.Collections.Generic;
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Diagnostics;

    public static class TCPGateway
    {
        private static List<Connection> ActiveConnections;
        private static Dictionary<string, PacketCounter> PacketCounters; // Счетчики пакетов по IP
        private static Dictionary<string, ConnectionAttemptCounter> ConnectionAttempts; // Попытки подключения по IP

        private static Socket Socket;
        private static Thread Thread;
        private static Timer CleanupTimer;

        private static ManualResetEvent AcceptEvent;

        public static void Init(string host, int port)
        {
            ActiveConnections = new List<Connection>();
            PacketCounters = new Dictionary<string, PacketCounter>();
            ConnectionAttempts = new Dictionary<string, ConnectionAttemptCounter>();

            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
            Socket.Listen(99999999);

            AcceptEvent = new ManualResetEvent(false);

            Thread = new Thread(Update);
            Thread.Start();

            // Запускаем таймер для очистки неактивных соединений каждые 30 секунд
            CleanupTimer = new Timer(CleanupInactiveConnections, null, 30000, 30000);

            Logger.Print($"TCP сервер запущен на {host}:{port}");
        }

        private static void Update()
        {
            while (true)
            {
                AcceptEvent.Reset();
                Socket.BeginAccept(new AsyncCallback(OnAccept), null);
                AcceptEvent.WaitOne();
            }
        }

        private static void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket client = Socket.EndAccept(ar);
                string clientIp = ((IPEndPoint)client.RemoteEndPoint).Address.ToString();

                Logger.Print($"Попытка подключения от {clientIp}");

                // Проверка на частоту попыток подключения
                if (!ConnectionAttempts.ContainsKey(clientIp))
                {
                    ConnectionAttempts[clientIp] = new ConnectionAttemptCounter();
                }

                var attemptCounter = ConnectionAttempts[clientIp];
                attemptCounter.AttemptCount++;
                if ((DateTime.Now - attemptCounter.FirstAttemptTime).TotalSeconds > 10)
                {
                    // Сбрасываем счетчик после 10 секунд
                    attemptCounter.FirstAttemptTime = DateTime.Now;
                    attemptCounter.AttemptCount = 1;
                }

                if (attemptCounter.AttemptCount > 5)
                {
                    BanIp(clientIp);
                    Logger.Print($"IP {clientIp} заблокирован за слишком частые попытки подключения.");
                    return;
                }

                if (!PacketCounters.ContainsKey(clientIp))
                {
                    PacketCounters[clientIp] = new PacketCounter();
                }

                Connection connection = new Connection(client);
                ActiveConnections.Add(connection);
                Logger.Print($"Новое подключение от {clientIp}");

                Connections.AddConnection(connection);
                client.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            }
            catch (Exception ex)
            {
                Logger.Print($"Ошибка при приеме подключения: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                AcceptEvent.Set();
            }
        }

        private static void OnReceive(IAsyncResult ar)
        {
            Connection connection = (Connection)ar.AsyncState;
            if (connection == null || connection.Socket == null || !connection.Socket.Connected)
                return;

            string clientIp = null;

            try
            {
                clientIp = ((IPEndPoint)connection.Socket.RemoteEndPoint).Address.ToString(); // Получаем IP клиента

                int r = connection.Socket.EndReceive(ar);
                if (r <= 0)
                {
                    Logger.Print($"{clientIp} отключился.");
                    ActiveConnections.Remove(connection);
                    if (connection.MessageManager.HomeMode != null)
                    {
                        Sessions.Remove(connection.Avatar.AccountId);
                    }
                    connection.Close();
                    return;
                }

                // Обновляем счетчик пакетов
                if (PacketCounters.ContainsKey(clientIp))
                {
                    var counter = PacketCounters[clientIp];
                    counter.PacketCount++;
                    if ((DateTime.Now - counter.FirstPacketTime).TotalSeconds > 10)
                    {
                        // Сбрасываем счетчик после 10 секунд
                        counter.FirstPacketTime = DateTime.Now;
                        counter.PacketCount = 1;
                    }

                    // Если больше 50 пакетов за 10 секунд, блокируем IP
                    if (counter.PacketCount > 50)
                    {
                        BanIp(clientIp);
                        Logger.Print($"IP {clientIp} заблокирован за превышение лимита пакетов.");
                        ActiveConnections.Remove(connection);
                        connection.Close();
                        return;
                    }
                }

                connection.Memory.Write(connection.ReadBuffer, 0, r);
                connection.UpdateLastActiveTime(); // Обновляем время последней активности

                if (connection.Messaging.OnReceive() != 0)
                {
                    ActiveConnections.Remove(connection);
                    if (connection.MessageManager.HomeMode != null)
                    {
                        Sessions.Remove(connection.Avatar.AccountId);
                    }
                    connection.Close();
                    Logger.Print($"{clientIp} отключился.");
                    return;
                }
                connection.Socket.BeginReceive(connection.ReadBuffer, 0, 1024, SocketFlags.None, new AsyncCallback(OnReceive), connection);
            }
            catch (ObjectDisposedException)
            {
                // Сокет был закрыт, игнорируем это исключение
                Logger.Print($"Сокет клиента {clientIp} уже был закрыт.");
            }
            catch (SocketException)
            {
                ActiveConnections.Remove(connection);
                if (connection.MessageManager.HomeMode != null)
                {
                    Sessions.Remove(connection.Avatar.AccountId);
                }
                connection.Close();
                Logger.Print($"{clientIp} отключился из-за ошибки сокета.");
            }
            catch (Exception exception)
            {
                Logger.Print($"Неожиданная ошибка от {clientIp}: {exception.Message}");
                connection.Close();
            }
        }

        private static void CleanupInactiveConnections(object state)
        {
            DateTime now = DateTime.Now;
            for (int i = ActiveConnections.Count - 1; i >= 0; i--)
            {
                var connection = ActiveConnections[i];

                // Проверяем, что объект соединения и его сокет не равны null
                if (connection != null && connection.Socket != null)
                {
                    // Проверяем, что сокет соединения все еще открыт
                    if (connection.Socket.Connected)
                    {
                        try
                        {
                            if ((now - connection.LastActiveTime).TotalSeconds > 30) // Если соединение неактивно более 30 секунд
                            {
                                Logger.Print($"Закрытие неактивного соединения от {connection.Socket.RemoteEndPoint}.");
                                ActiveConnections.RemoveAt(i);
                                connection.Close();
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            // Игнорируем ObjectDisposedException, если сокет был уже закрыт
                            Logger.Print($"Сокет был закрыт до получения RemoteEndPoint, пропускаем.");
                            ActiveConnections.RemoveAt(i);
                            connection.Close();
                        }
                        catch (NullReferenceException)
                        {
                            // Обрабатываем случай, когда свойства соединения могут быть null
                            Logger.Print("NullReferenceException при попытке получить информацию о соединении.");
                            ActiveConnections.RemoveAt(i);
                        }
                    }
                    else
                    {
                        // Удаляем соединение, если сокет закрыт
                        Logger.Print($"Сокет для соединения уже закрыт и будет удален.");
                        ActiveConnections.RemoveAt(i);
                        connection.Close();
                    }
                }
                else
                {
                    // Удаляем соединение, если оно или его сокет равны null
                    Logger.Print("Соединение или его сокет равны null, удаляем.");
                    ActiveConnections.RemoveAt(i);
                }
            }
        }



        public static void OnSend(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndSend(ar);
            }
            catch (Exception)
            {
                // Обработка ошибок при отправке
            }
        }

        private static void BanIp(string ip)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"iptables -A INPUT -s {ip} -j DROP\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
    }

    // Класс для хранения информации о пакетах
    public class PacketCounter
    {
        public DateTime FirstPacketTime { get; set; }
        public int PacketCount { get; set; }

        public PacketCounter()
        {
            FirstPacketTime = DateTime.Now;
            PacketCount = 0;
        }
    }

    // Класс для хранения информации о попытках подключения
    public class ConnectionAttemptCounter
    {
        public DateTime FirstAttemptTime { get; set; }
        public int AttemptCount { get; set; }

        public ConnectionAttemptCounter()
        {
            FirstAttemptTime = DateTime.Now;
            AttemptCount = 0;
        }
    }
}
