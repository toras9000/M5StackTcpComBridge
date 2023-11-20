using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;

namespace M5StackTcpComBridge;

public class TcpComBridge : IDisposable
{
    public TcpComBridge(TcpClient client)
    {
        // M5Stack Core2 for AWS のボトムモジュール PORT.B 用のピン設定
        nanoFramework.Hardware.Esp32.Configuration.SetPinFunction(36, nanoFramework.Hardware.Esp32.DeviceFunction.COM2_RX);
        nanoFramework.Hardware.Esp32.Configuration.SetPinFunction(26, nanoFramework.Hardware.Esp32.DeviceFunction.COM2_TX);

        // COMポートを開く
        this.comPort = new SerialPort("COM2");
        this.comPort.BaudRate = 115_200;
        this.comPort.Handshake = Handshake.None;
        this.comPort.DataBits = 8;
        this.comPort.Parity = Parity.None;
        this.comPort.StopBits = StopBits.One;
        this.comPort.Open();

        // クライアントを保持
        this.remoteClient = client;
    }

    public void Run()
    {
        using var canceller = new CancellationTokenSource();
        using var remote = this.remoteClient.GetStream();

        // TCPリモートからのデータを受信してCOMに送信するスレッド
        var receiver = new Thread(() =>
        {
            var buffer = new byte[1024];
            while (true)
            {
                var recvLen = remote.Read(buffer, 0, buffer.Length);
                if (recvLen <= 0) break;

                this.comPort.Write(buffer, 0, recvLen);
            }
        });

        // COMからの受信データをTCPリモートへ送信するスレッド
        var sender = new Thread(() =>
        {
            var buffer = new byte[1024];
            while (!canceller.IsCancellationRequested)
            {
                var readableBytes = this.comPort.BytesToRead;
                if (0 < readableBytes)
                {
                    var recvLen = this.comPort.Read(buffer, 0, Math.Min(readableBytes, buffer.Length));
                    if (0 < recvLen)
                    {
                        try
                        {
                            remote.Write(buffer, 0, recvLen);
                        }
                        catch (System.IO.IOException)
                        {
                            break;
                        }
                    }
                }
                Thread.Sleep(1);
            }
        });

        // 仲介スレッドを開始
        receiver.Start();
        sender.Start();

        // 受信スレッドが完了(リモートがコネクション終了)するまで待機。
        // その後逆方向の仲介を閉じる
        receiver.Join();
        canceller.Cancel();
        sender.Join();

    }

    public void Dispose()
    {
        this.comPort.Dispose();
    }

    private SerialPort comPort;
    private TcpClient remoteClient;
}
