using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace wmib
{
    class ChannelTools : Module
    {
        public override void Load()
        {
            try
            {
                while (working)
                {
                    System.Threading.Thread.Sleep(1000000);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }

        private static User getUser(string name, config.channel c)
        {
            lock (c.UserList)
            {
                foreach (User user in c.UserList)
                {
                    if (name.ToLower() == user.Nick.ToLower())
                    {
                        return user;
                    }
                }
            }
            return null;
        }

        public void GetOp(config.channel chan)
        {
            if (!GetConfig(chan, "OP.Permanent", false))
            {
                chan.instance.irc._SlowQueue.Send("CS op " + chan.Name, IRC.priority.high);
                return;
            }
            // get our user
            User user = chan.RetrieveUser(chan.instance.Nick);
            if (user == null)
            {
                chan.instance.irc._SlowQueue.Send("CS op " + chan.Name, IRC.priority.high);
                return;
            }
            if (!user.IsOperator)
            {
                chan.instance.irc._SlowQueue.Send("CS op " + chan.Name, IRC.priority.high);
            }
        }

        public override void Hook_PRIV(config.channel channel, User invoker, string message)
        {
            if (message.StartsWith(config.CommandPrefix + "optools-on"))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpE1", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpM1", channel.Language), channel.Name);
                        SetConfig(channel, "OP.Enabled", true);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "optools-permanent-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Permanent", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpE2", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpM2", channel.Language), channel);
                        SetConfig(channel, "OP.Permanent", false);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "optools-permanent-on")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Permanent", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpE3", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpM3", channel.Language), channel);
                        SetConfig(channel, "OP.Permanent", true);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message == config.CommandPrefix + "optools-off")
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (!GetConfig(channel, "OP.Enabled", false))
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpE4", channel.Language), channel);
                        return;
                    }
                    else
                    {
                        core.irc._SlowQueue.DeliverMessage(messages.get("OpM4", channel.Language), channel);
                        SetConfig(channel, "OP.Enabled", false);
                        channel.SaveConfig();
                        return;
                    }
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "kick "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(6);
                        string reason = "Removed from the channel";
                        if (nick.Contains(" "))
                        {
                            reason = nick.Substring(nick.IndexOf(" ") + 1);
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        channel.instance.irc._SlowQueue.Send("KICK " + channel.Name + " " + user.Nick + " :" + reason, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + channel.instance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "kb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(4);
                        string reason = "Removed from the channel";
                        if (nick.Contains(" "))
                        {
                            reason = nick.Substring(nick.IndexOf(" ") + 1);
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("OpE6", channel.Language), channel, IRC.priority.high);
                        }
                        else
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " +b *!*@" + user.Host, IRC.priority.high);
                        }
                        channel.instance.irc._SlowQueue.Send("KICK " + channel.Name + " " + user.Nick + " :" + reason, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + channel.instance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "unkb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(6);
                        GetOp(channel);
                        channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -b *!*@" + nick, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + channel.instance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "unq "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(5);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }

                        if (string.IsNullOrEmpty(user.Host))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("OpE6", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        // op self
                        GetOp(channel);
                        channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -q *!*@" + user.Host, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + channel.instance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "q "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(3);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user == null)
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("OpE5", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        
                        if (string.IsNullOrEmpty(user.Host))
                        {
                            core.irc._SlowQueue.DeliverMessage(messages.get("OpE6", channel.Language), channel, IRC.priority.high);
                            return;
                        }
                        GetOp(channel);
                        channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " +q *!*@" + user.Host, IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + channel.instance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "jb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(4);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user != null)
                        {
                            nick = user.Nick;
                        }
                        // op self
                        GetOp(channel);
                        channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " +b " + nick + "!*@*$##fix_your_connection", IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + channel.instance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }

            if (message.StartsWith(config.CommandPrefix + "unjb "))
            {
                if (channel.Users.IsApproved(invoker, "admin"))
                {
                    if (GetConfig(channel, "OP.Enabled", false))
                    {
                        string nick = message.Substring(6);
                        if (nick.Contains(" "))
                        {
                            nick = nick.Substring(0, nick.IndexOf(" "));
                        }
                        User user = getUser(nick, channel);
                        if (user != null)
                        {
                            nick = user.Nick;
                        }
                        // op self
                        GetOp(channel);
                        channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -b " + nick + "!*@*$##fix_your_connection", IRC.priority.high);
                        if (!GetConfig(channel, "OP.Permanent", false))
                        {
                            channel.instance.irc._SlowQueue.Send("MODE " + channel.Name + " -o " + channel.instance.Nick, IRC.priority.low);
                        }
                        return;
                    }
                    return;
                }
                if (!channel.suppress_warnings)
                {
                    core.irc._SlowQueue.DeliverMessage(messages.get("PermissionDenied", channel.Language), channel.Name, IRC.priority.low);
                }
                return;
            }
        }

        public override bool Construct()
        {
            Version = "1.0.20";
            start = true;
            Name = "Operator tools";
            return true;
        }
    }
}
