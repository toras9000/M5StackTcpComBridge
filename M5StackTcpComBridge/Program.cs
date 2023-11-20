using System;
using System.Device.Wifi;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using nanoFramework.Networking;

namespace M5StackTcpComBridge;

public class Program
{
    public static void Main()
    {
        nanoFramework.M5Stack.M5Core2.InitializeScreen();
        var console = new ScrollConsole();

        // MACアドレスを表示
        var wifi = default(WifiAdapter);
        try
        {
            Console.WriteLine($"Wi-Fi Adapter detection");
            wifi = WifiAdapter.FindAllAdapters()[0];
            var nwIf = NetworkInterface.GetAllNetworkInterfaces()[wifi.NetworkInterface];
            var macAddr = new StringBuilder();
            foreach (var part in nwIf.PhysicalAddress)
            {
                if (macAddr.Length != 0) macAddr.Append(":");
                macAddr.Append($"{part:X2}");
            }
            console.WriteLine($"  MAC Address: {macAddr}");
        }
        catch (Exception ex)
        {
            console.WriteLine($"  Wi-Fi adapter fail: {ex.Message}");
            Thread.Sleep(Timeout.Infinite);
        }

        // Wi-Fiアクセスポイントへの接続
        try
        {
            console.WriteLine($"Connect access point");
            var ssid = "access-point";
            var password = "*****";
            var result = WifiNetworkHelper.ConnectDhcp(ssid, password, wifiAdapterId: wifi!.NetworkInterface);
            if (!result) throw new Exception("not connected");
            var nwIf = NetworkInterface.GetAllNetworkInterfaces()[wifi.NetworkInterface];
            console.WriteLine($"  Connected to {ssid}");
            console.WriteLine($"  IP address: {nwIf.IPv4Address}");
        }
        catch (Exception ex)
        {
            console.WriteLine($"  Failed to connect: {ex.Message}");
            Thread.Sleep(Timeout.Infinite);
        }

        // TCPとCOMのブリッジ
        while (true)
        {
            try
            {
                console.WriteLine($"Waiting TCP connection ..");
                var endpoint = new IPEndPoint(IPAddress.Any, 5000);
                var listener = new TcpListener(endpoint);
                listener.Start(1);

                using var client = listener.AcceptTcpClient();
                console.WriteLine($"Accepted from {client.Client.RemoteEndPoint}");

                console.WriteLine($"Start bridge");
                using var bridge = new TcpComBridge(client);
                bridge.Run();
                console.WriteLine($"End bridge");
            }
            catch (Exception ex)
            {
                console.WriteLine($"Bridge error: {ex.Message}");
                Thread.Sleep(10 * 1000);
            }
        }
    }
}
