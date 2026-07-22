using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PoPUnturnedLauncher
{
    public class ServerInfo
    {
        public bool IsOnline { get; set; } = false;
        public string Name { get; set; } = "Servidor Desconectado";
        public string Map { get; set; } = "";
        public int Players { get; set; } = 0;
        public int MaxPlayers { get; set; } = 0;
    }

    public static class ServerQuery
    {
        private static readonly byte[] RequestHeader = { 0xFF, 0xFF, 0xFF, 0xFF };
        private static readonly byte A2S_INFO = 0x54; // 'T'
        private static readonly byte S2C_CHALLENGE = 0x41; // 'A'
        private static readonly byte A2S_INFO_RESPONSE = 0x49; // 'I'

        public static async Task<ServerInfo> QueryServerAsync(string ipAddress, int queryPort)
        {
            var info = new ServerInfo();
            
            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.Client.SendTimeout = 2000;
                    udpClient.Client.ReceiveTimeout = 2000;

                    IPAddress? ip;
                    if (!IPAddress.TryParse(ipAddress, out ip))
                    {
                        var addresses = await Dns.GetHostAddressesAsync(ipAddress);
                        if (addresses.Length > 0)
                        {
                            ip = addresses[0];
                        }
                        else
                        {
                            return info;
                        }
                    }

                    var endPoint = new IPEndPoint(ip, queryPort);
                    
                    // Construir petición inicial: 0xFFFFFFFF + 'T' + "Source Engine Query\0"
                    byte[] payload = BuildRequest(null);
                    
                    await udpClient.SendAsync(payload, payload.Length, endPoint);
                    
                    var receiveTask = udpClient.ReceiveAsync();
                    var timeoutTask = Task.Delay(2000);

                    var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        return info; // Timeout
                    }

                    var result = receiveTask.Result;
                    byte[] response = result.Buffer;

                    if (response.Length < 5) return info;

                    // Comprobar si el servidor nos responde con un reto (Challenge)
                    if (response[4] == S2C_CHALLENGE && response.Length >= 9)
                    {
                        // Extraer token de desafío (4 bytes desde la posición 5)
                        byte[] challengeToken = new byte[4];
                        Buffer.BlockCopy(response, 5, challengeToken, 0, 4);

                        // Reconstruir petición con el token de desafío
                        byte[] finalPayload = BuildRequest(challengeToken);
                        
                        await udpClient.SendAsync(finalPayload, finalPayload.Length, endPoint);

                        receiveTask = udpClient.ReceiveAsync();
                        completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                        if (completedTask == timeoutTask)
                        {
                            return info; // Timeout en el segundo intento
                        }

                        response = receiveTask.Result.Buffer;
                    }

                    // Parsear respuesta del servidor (A2S_INFO_RESPONSE)
                    if (response.Length >= 5 && response[4] == A2S_INFO_RESPONSE)
                    {
                        ParseInfoResponse(response, info);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al consultar el servidor: {ex.Message}");
            }

            return info;
        }

        private static byte[] BuildRequest(byte[]? challengeToken)
        {
            string queryStr = "Source Engine Query\0";
            byte[] queryBytes = Encoding.UTF8.GetBytes(queryStr);
            
            int size = 4 + 1 + queryBytes.Length;
            if (challengeToken != null)
            {
                size += challengeToken.Length;
            }

            byte[] packet = new byte[size];
            
            // Header: 0xFFFFFFFF
            Buffer.BlockCopy(RequestHeader, 0, packet, 0, 4);
            
            // Tipo de Query: A2S_INFO ('T')
            packet[4] = A2S_INFO;
            
            // Payload del Query
            Buffer.BlockCopy(queryBytes, 0, packet, 5, queryBytes.Length);

            // Añadir Challenge Token si existe
            if (challengeToken != null)
            {
                Buffer.BlockCopy(challengeToken, 0, packet, 5 + queryBytes.Length, challengeToken.Length);
            }

            return packet;
        }

        private static void ParseInfoResponse(byte[] response, ServerInfo info)
        {
            try
            {
                using (var ms = new MemoryStream(response))
                using (var reader = new BinaryReader(ms, Encoding.UTF8))
                {
                    // Saltar header (4 bytes) y tipo de respuesta (1 byte)
                    reader.ReadBytes(5);

                    byte protocol = reader.ReadByte();
                    string serverName = ReadNullTerminatedString(reader);
                    string mapName = ReadNullTerminatedString(reader);
                    string folderName = ReadNullTerminatedString(reader);
                    string gameName = ReadNullTerminatedString(reader);
                    
                    short appId = reader.ReadInt16();
                    byte players = reader.ReadByte();
                    byte maxPlayers = reader.ReadByte();
                    
                    info.IsOnline = true;
                    info.Name = serverName;
                    info.Map = mapName;
                    info.Players = players;
                    info.MaxPlayers = maxPlayers;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parseando datos A2S: {ex.Message}");
            }
        }

        public static async Task<System.Collections.Generic.List<string>> QueryPlayersAsync(string ipAddress, int queryPort)
        {
            var players = new System.Collections.Generic.List<string>();
            try
            {
                using (var udpClient = new UdpClient())
                {
                    udpClient.Client.SendTimeout = 2000;
                    udpClient.Client.ReceiveTimeout = 2000;

                    IPAddress? ip;
                    if (!IPAddress.TryParse(ipAddress, out ip))
                    {
                        var addresses = await Dns.GetHostAddressesAsync(ipAddress);
                        if (addresses.Length > 0) ip = addresses[0];
                        else return players;
                    }

                    var endPoint = new IPEndPoint(ip, queryPort);

                    // Petición inicial para obtener el Challenge Token de jugadores
                    // Header: 0xFFFFFFFF, Tipo: 0x55 ('U'), Challenge: 0xFFFFFFFF
                    byte[] challengeRequest = { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
                    await udpClient.SendAsync(challengeRequest, challengeRequest.Length, endPoint);

                    var receiveTask = udpClient.ReceiveAsync();
                    var timeoutTask = Task.Delay(2000);
                    var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                    if (completedTask == timeoutTask) return players;

                    var result = receiveTask.Result;
                    byte[] response = result.Buffer;

                    // Si responde con un reto (0x41), extraer token y reenviar
                    if (response.Length >= 9 && response[4] == S2C_CHALLENGE)
                    {
                        byte[] challengeToken = new byte[4];
                        Buffer.BlockCopy(response, 5, challengeToken, 0, 4);

                        // Reenviar petición con token: 0xFFFFFFFF + 'U' + challengeToken
                        byte[] finalRequest = new byte[9];
                        Buffer.BlockCopy(RequestHeader, 0, finalRequest, 0, 4);
                        finalRequest[4] = 0x55; // A2S_PLAYER ('U')
                        Buffer.BlockCopy(challengeToken, 0, finalRequest, 5, 4);

                        await udpClient.SendAsync(finalRequest, finalRequest.Length, endPoint);

                        receiveTask = udpClient.ReceiveAsync();
                        completedTask = await Task.WhenAny(receiveTask, timeoutTask);
                        if (completedTask == timeoutTask) return players;

                        response = receiveTask.Result.Buffer;
                    }

                    // Parsear respuesta A2S_PLAYER (comienza con 0xFFFFFFFF y 0x44 'D')
                    if (response.Length >= 6 && response[4] == 0x44)
                    {
                        using (var ms = new MemoryStream(response))
                        using (var reader = new BinaryReader(ms, Encoding.UTF8))
                        {
                            reader.ReadBytes(5); // Saltar header y 'D'
                            byte playerCount = reader.ReadByte();

                            for (int i = 0; i < playerCount; i++)
                            {
                                if (ms.Position >= ms.Length) break;
                                
                                byte index = reader.ReadByte();
                                string name = ReadNullTerminatedString(reader);
                                int score = reader.ReadInt32();
                                float duration = reader.ReadSingle(); // float

                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    players.Add(name);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al consultar lista de jugadores: {ex.Message}");
            }
            return players;
        }

        private static string ReadNullTerminatedString(BinaryReader reader)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}
