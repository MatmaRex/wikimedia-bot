using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System.Net;

namespace wmib
{
    public class NetCat : Module
    {
        public readonly int Port = 64834;

        public override bool Construct()
        {
            Name = "NetCat";
            start = true;
            Version = "1.0.0.0";
            return true;
        }

        public void SendMessage(ref StreamWriter writer, string text)
        {
            writer.WriteLine(text);
            writer.Flush();
        }

        public void Client(object data)
        {
            try
            {
                // #channel token message goes here and needs a newline on end
                DebugLog("Accepted connection");
                System.Net.Sockets.TcpClient client = (System.Net.Sockets.TcpClient)data;
                string IP = client.Client.RemoteEndPoint.ToString();
                DebugLog("Incoming connection from: " + IP);
                System.Net.Sockets.NetworkStream ns = client.GetStream();
                StreamWriter _StreamWriter = new StreamWriter(ns);
                StreamReader _StreamReader = new StreamReader(ns, Encoding.UTF8);
                while (!_StreamReader.EndOfStream)
                {
                    string text = _StreamReader.ReadLine();
                    if (!text.Contains(" "))
                    {
                        DebugLog("Invalid text from " + IP + ": " + text, 2);
                        SendMessage(ref _StreamWriter, "ERROR1 (invalid text): " + text);
                        continue;
                    }
                    string channel = text.Substring(0, text.IndexOf(" "));
                    string value = text.Substring(text.IndexOf(" ") + 1);
                    DebugLog("Request to send text to channel " + channel + " text: " + value, 4);
                    config.channel ch = core.getChannel(channel);
                    if (ch == null)
                    {
                        DebugLog("Nonexistent channel " + channel + " message was rejected");
                        SendMessage(ref _StreamWriter, "ERROR2 (invalid channel " + channel + "): " + value);
                        continue;
                    }
                    if (!GetConfig(ch, "NetCat.Enabled", false))
                    {
                        DebugLog("Channel doesn't allow relay " + channel + " message was rejected");
                        SendMessage(ref _StreamWriter, "ERROR3 (disallowed): " + channel + " :" + value);
                        continue;
                    }
                    if (GetConfig(ch, "NetCat.Token", false))
                    {
                        DebugLog("Channel requires the token for relay " + channel, 6);
                        if (!value.Contains(" "))
                        {
                            DebugLog("Invalid token from " + IP + " to " + channel);
                            SendMessage(ref _StreamWriter, "ERROR4 (invalid token): " + channel + " :" + value);
                            continue;
                        }
                        string token = value.Substring(0, value.IndexOf(" "));
                        value = value.Substring(value.IndexOf(" ") + 1);
                        if (token != GetConfig(ch, "NetCat.TokenData", "<invalid>"))
                        {
                            DebugLog("Channel requires the token for relay " + channel, 6);
                            SendMessage(ref _StreamWriter, "ERROR4 (invalid token): " + channel + " :" + value);
                            continue;
                        }
                    }
                    DebugLog("Relaying message from " + IP + " to " + channel + ":" + value, 2);
                    core.irc._SlowQueue.DeliverMessage(value, ch, IRC.priority.low);
                }
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public override void Load()
        {
            try
            {
                Log("NetCat listening on port " + Port.ToString());
                System.Net.Sockets.TcpListener server = new System.Net.Sockets.TcpListener(IPAddress.Any, Port);
                server.Start();
                while (true)
                {
                    try
                    {
                        System.Net.Sockets.TcpClient connection = server.AcceptTcpClient();
                        Thread _client = new Thread(Client);
                        //threads.Add(_client);
                        _client.Start(connection);
                        System.Threading.Thread.Sleep(200);
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception fail)
                    {
                        handleException(fail);
                    }
                }
            }
            catch (Exception fail)
            {
                handleException(fail);
            }
        }

        public static string GenerateToken()
        {
            Random random = new Random((int)DateTime.Now.Ticks);
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < 40; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message == config.CommandPrefix + "relay-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "NetCat.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Relay is already disabled", channel.Name);
                        return;
                    }
                    SetConfig(channel, "NetCat.Enabled", false);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage("Relay was disabled", channel.Name);
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
                return;
            }

            if (message == config.CommandPrefix + "token-on")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    string token = GenerateToken();
                    SetConfig(channel, "NetCat.Token", true);
                    SetConfig(channel, "NetCat.TokenData", token);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage("New token was generated for this channel, and it was sent to you in a private message", channel.Name);
                    core.irc._SlowQueue.DeliverMessage("Token for " + channel.Name + " is: " + token, invoker.Nick, IRC.priority.normal);
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
                return;
            }

            if (message == config.CommandPrefix + "token-off")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    SetConfig(channel, "NetCat.Token", false);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage("This channel will no longer require a token in order to relay messages into it", channel.Name);
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
                return;
            }

            if (message == config.CommandPrefix + "token-remind")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (!GetConfig(channel, "NetCat.Token", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("This channel doesn't require a token", channel.Name);
                        return;
                    }
                    string token = GetConfig(channel, "NetCat.TokenData", "<invalid>");
                    core.irc._SlowQueue.DeliverMessage("Token for " + channel.Name + " is: " + token, invoker.Nick, IRC.priority.normal);
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
                return;
            }

            if (message == config.CommandPrefix + "relay-on")
            {
                if (channel.Users.isApproved(invoker.Nick, invoker.Host, "admin"))
                {
                    if (GetConfig(channel, "NetCat.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage("Relay is already enabled", channel.Name);
                        return;
                    }
                    SetConfig(channel, "NetCat.Enabled", true);
                    channel.SaveConfig();
                    core.irc._SlowQueue.DeliverMessage("Relay was enabled", channel.Name);
                    return;
                }
                else
                {
                    if (!channel.suppress_warnings)
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                    }
                }
                return;
            }
        }
    }
}
