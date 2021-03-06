using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace tcp_io
{
    public struct BufferItem
    {
        public string _text;
        public DateTime _datetime;
        public bool important;
    }
    public static class Buffer
    {
        public static List<BufferItem> data_ou = new List<BufferItem>();
        public static List<BufferItem> data_in = new List<BufferItem>();

        public static bool Out(string message)
        {
            try
            {
                BufferItem item = new BufferItem();
                item._datetime = DateTime.Now;
                item._text = message;
                lock (data_ou)
                {
                    data_ou.Add(item);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool In(string message, bool control = false)
        {
            try
            {
                BufferItem item = new BufferItem();
                item._datetime = DateTime.Now;
                item.important = control;
                item._text = message;
                lock (data_in)
                {
                    data_in.Add(item);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    class Server
    {
        public static string network = "irc.freenode.net";
        public static int port = 6667;

        public static bool connection_remote = false;

        public static bool connection_host = false;

        public static System.IO.StreamReader local_reader;
        public static System.IO.StreamWriter local_writer;

        public static System.IO.StreamWriter _w;
        public static System.IO.StreamReader _r;
        public static System.Net.Sockets.NetworkStream stream;
        public static TcpClient client;

        public static System.Threading.Thread listener;
        public static System.Threading.Thread irc;

        public static void Listen()
        {
            TcpListener cache = new TcpListener(IPAddress.Parse("127.0.0.1"), port);

            cache.Start();
            Console.WriteLine("Cache is ok");
            
            while (true)
            {
                client = cache.AcceptTcpClient();
                NetworkStream temp = client.GetStream();
                local_writer = new System.IO.StreamWriter(temp);
                local_reader = new System.IO.StreamReader(temp, System.Text.Encoding.UTF8);
                //_socket = cache.AcceptSocket();
                
                connection_host = true;
                
                //Console.WriteLine("");
                try
                {
                    while (!local_reader.EndOfStream)
                    {
                        
                        //byte[] text = new byte[8000];
                        //int i = _socket.Receive(text);
                        //text = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, text);
                        string data = local_reader.ReadLine();
                       
                            if (data == "")
                            {
                                continue;
                            }
                            if (!data.StartsWith("CONTROL: "))
                            {
                                Buffer.Out(data);
                            }
                            else
                            {
                                string code = data.Replace("\r", "").Substring("CONTROLxx".Length);
                                switch (code)
                                {
                                    case "STATUS":
                                        if (connection_remote)
                                        {
                                            Buffer.In("CONTROL: TRUE", true);
                                        } else
                                        {
                                            Buffer.In("CONTROL: FALSE", true);
                                        }
                                        break;
                                    case "CREATE":
                                        StartIRC();
                                        Console.WriteLine("Connecting wait");
                                        break;
                                }
                            }
                        System.Threading.Thread.Sleep(20);
                    }
                }
                catch (System.IO.IOException)
                {
                    connection_host = false;
                    Console.WriteLine("Remote dced");
                }

                connection_host = false;
                System.Threading.Thread.Sleep(20);
            }
        }

        public static bool StartIRC()
        {
            try
            {
                if (connection_remote)
                {
                    return false;
                }
                stream = new System.Net.Sockets.TcpClient(network, 6667).GetStream();
                _r = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
                _w = new System.IO.StreamWriter(stream);
                connection_remote = true;
            }
            catch (Exception x)
            {
                connection_remote = false;
            }
            return false;
        }

        public static void Init()
        {
            while (true)
            {
                try
                {
                    if (connection_remote)
                    {
                        while (!_r.EndOfStream)
                        {
                            string text = _r.ReadLine();
                            Buffer.In(text);
                            System.Threading.Thread.Sleep(20);
                        }
                        Buffer.In("CONTROL: DC");
                        connection_remote = false;
                    }
                }
                catch (System.IO.IOException)
                {
                    connection_remote = false;
                    Buffer.In("CONTROL: DC");
                }
                System.Threading.Thread.Sleep(10);
            }
        }

        public static void Connect()
        {
            listener = new System.Threading.Thread(Listen);
            listener.Start();
            irc = new System.Threading.Thread(Init);
            irc.Start();
            int ping = 0;
            while (true)
            {
                try
                {
                    if (client != null)
                    {
                        if (client.Connected)
                        {
                            if (Buffer.data_in.Count > 0)
                            {
                                BufferItem lastitem;
                                lock (Buffer.data_in)
                                {
                                    lastitem = Buffer.data_in[0];
                                    foreach (BufferItem Item in Buffer.data_in)
                                    {
                                        if (Item.important)
                                        {
                                            lastitem = Item;
                                            break;
                                        }
                                    }
                                    Buffer.data_in.Remove(lastitem);
                                }
                                //UTF8Encoding dc = new UTF8Encoding();
                                local_writer.WriteLine(lastitem._text);
                                local_writer.Flush();
                                //_socket.Send(dc.GetBytes(lastitem._text + "\n"));
                            }
                        }
                    }

                    if (connection_remote)
                    {
                        if (Buffer.data_ou.Count > 0)
                        {
                            BufferItem lastitem;
                            lock (Buffer.data_ou)
                            {
                                lastitem = Buffer.data_ou[0];
                                Buffer.data_ou.Remove(lastitem);
                            }
                            _w.WriteLine(lastitem._text);
                            _w.Flush();
                        }
                    }
                    ping++;
                    if (ping > 2000)
                    {
                        ping = 0;
                        if (connection_remote)
                        {
                            _w.WriteLine("PING :" + "irc.freenode.net");
                            _w.Flush();
                        }
                    }
                }
                catch (Exception x)
                { 
                    
                }
                System.Threading.Thread.Sleep(10);
            }

        }
    }
}
