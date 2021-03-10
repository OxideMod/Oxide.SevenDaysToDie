using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Game.SevenDays
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class SevenDaysCommandSystem : ICommandSystem
    {
        #region Initialization

        // The covalence provider
        private readonly SevenDaysCovalenceProvider provider = SevenDaysCovalenceProvider.Instance;

        // The console player
        private readonly SevenDaysConsolePlayer consolePlayer;

        // Command handler
        private readonly CommandHandler commandHandler;

        // All registered commands
        internal IDictionary<string, RegisteredCommand> registeredCommands;

        internal class NativeCommand : ConsoleCmdAbstract
        {
            /// <summary>
            /// The name of the command
            /// </summary>
            public string Command;

            /// <summary>
            /// The player that sent the command
            /// </summary>
            public IPlayer Sender;

            /// <summary>
            /// The callback
            /// </summary>
            public CommandCallback Callback;

            /// <summary>
            /// Executes the command
            /// </summary>
            /// <param name="args"></param>
            /// <param name="sender"></param>
            public override void Execute(List<string> args, CommandSenderInfo sender)
            {
                IPlayer player = sender.RemoteClientInfo?.IPlayer ?? Sender;
                Callback?.Invoke(player, Command, args.ToArray());
            }

            /// <summary>
            /// Gets the variant commands for the command
            /// </summary>
            /// <returns></returns>
            public override string[] GetCommands() => new[] { Command };

            /// <summary>
            /// Gets the description for the command
            /// </summary>
            /// <returns></returns>
            public override string GetDescription()
            {
                return "See plugin documentation for command description"; // TODO: Implement when possible and localize
            }

            /// <summary>
            /// Gets the help for the command
            /// </summary>
            /// <returns></returns>
            public override string GetHelp()
            {
                return "See plugin documentation for command help"; // TODO: Implement when possible and localize
            }
        }

        // Registered commands
        internal class RegisteredCommand
        {
            /// <summary>
            /// The plugin that handles the command
            /// </summary>
            public readonly Plugin Source;

            /// <summary>
            /// The name of the command
            /// </summary>
            public readonly string Command;

            /// <summary>
            /// The callback
            /// </summary>
            public readonly CommandCallback Callback;

            /// <summary>
            /// The callback
            /// </summary>
            public IConsoleCommand OriginalCallback;

            /// <summary>
            /// The 7 Days to Die command
            /// </summary>
            public NativeCommand SevenDaysToDieCommand;

            /// <summary>
            /// The original command when overridden
            /// </summary>
            public IConsoleCommand OriginalSevenDaysToDieCommand;

            /// <summary>
            /// Initializes a new instance of the RegisteredCommand class
            /// </summary>
            /// <param name="source"></param>
            /// <param name="command"></param>
            /// <param name="callback"></param>
            public RegisteredCommand(Plugin source, string command, CommandCallback callback)
            {
                Source = source;
                Command = command;
                Callback = callback;
            }
        }

        /// <summary>
        /// Initializes the command system
        /// </summary>
        public SevenDaysCommandSystem()
        {
            registeredCommands = new Dictionary<string, RegisteredCommand>();
            commandHandler = new CommandHandler(CommandCallback, registeredCommands.ContainsKey);
            consolePlayer = new SevenDaysConsolePlayer();
        }

        private bool CommandCallback(IPlayer caller, string cmd, string[] args)
        {
            return registeredCommands.TryGetValue(cmd, out RegisteredCommand command) && command.Callback(caller, cmd, args);
        }

        #endregion Initialization

        #region Command Registration

        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            // Convert command to lowercase and remove whitespace
            command = command.ToLowerInvariant().Trim();

            // Split the command
            string[] split = command.Split('.');
            string parent = split.Length >= 2 ? split[0].Trim() : "global";
            string name = split.Length >= 2 ? string.Join(".", split.Skip(1).ToArray()) : split[0].Trim();
            if (parent == "global")
            {
                command = name;
            }

            // Check if the command can be overridden
            if (!CanOverrideCommand(command))
            {
                throw new CommandAlreadyExistsException(command);
            }

            // Set up a new command
            RegisteredCommand newCommand = new RegisteredCommand(plugin, command, callback)
            {
                // Create a new native command
                SevenDaysToDieCommand = new NativeCommand
                {
                    Command = command,
                    Callback = callback,
                    Sender = consolePlayer
                }
            };

            // Check if the command already exists in another plugin
            if (registeredCommands.TryGetValue(command, out RegisteredCommand cmd))
            {
                if (newCommand.SevenDaysToDieCommand == cmd.SevenDaysToDieCommand)
                {
                    newCommand.OriginalSevenDaysToDieCommand = cmd.SevenDaysToDieCommand;
                }

                string newPluginName = plugin?.Name ?? "An unknown plugin"; // TODO: Localization
                string previousPluginName = cmd.Source?.Name ?? "an unknown plugin"; // TODO: Localization
                Interface.Oxide.LogWarning($"{newPluginName} has replaced the '{command}' command previously registered by {previousPluginName}"); // TODO: Localization
            }

            // Check if the command already exists as a native command
            IConsoleCommand nativeCommand = SdtdConsole.Instance.GetCommand(command);
            if (nativeCommand != null)
            {
                if (newCommand.OriginalCallback == null)
                {
                    newCommand.OriginalCallback = nativeCommand;
                }

                if (SdtdConsole.Instance.m_Commands.Contains(nativeCommand))
                {
                    SdtdConsole.Instance.m_Commands.Remove(nativeCommand);
                    foreach (string nCommand in newCommand.SevenDaysToDieCommand.GetCommands())
                    {
                        SdtdConsole.Instance.m_CommandsAllVariants.Remove(nCommand);
                    }
                }

                string newPluginName = plugin?.Name ?? "An unknown plugin"; // TODO: Localization
                Interface.Oxide.LogWarning($"{newPluginName} has replaced the '{command}' command previously registered by {provider.GameName}"); // TODO: Localization
            }

            // Register the command
            registeredCommands.Add(command, newCommand);
            if (!SdtdConsole.Instance.m_Commands.Contains(newCommand.SevenDaysToDieCommand))
            {
                SdtdConsole.Instance.m_Commands.Add(newCommand.SevenDaysToDieCommand);
            }
            foreach (string nCommand in newCommand.SevenDaysToDieCommand.GetCommands())
            {
                if (!string.IsNullOrEmpty(nCommand))
                {
                    SdtdConsole.Instance.m_CommandsAllVariants.Add(nCommand, newCommand.SevenDaysToDieCommand);
                }
            }

            // Register the command permission
            string[] variantCommands = newCommand.SevenDaysToDieCommand.GetCommands();
            if (!GameManager.Instance.adminTools.IsPermissionDefinedForCommand(variantCommands) && newCommand.SevenDaysToDieCommand.DefaultPermissionLevel != 0)
            {
                GameManager.Instance.adminTools.AddCommandPermission(command, newCommand.SevenDaysToDieCommand.DefaultPermissionLevel, false);
            }
        }

        #endregion Command Registration

        #region Command Unregistration

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin)
        {
            IConsoleCommand cmd = SdtdConsole.Instance.GetCommand(command);
            if (SdtdConsole.Instance.m_Commands.Contains(cmd))
            {
                SdtdConsole.Instance.m_Commands.Remove(cmd);
                foreach (string nCommand in cmd.GetCommands())
                {
                    SdtdConsole.Instance.m_CommandsAllVariants.Remove(nCommand);
                }
            }

            // Remove the command
            registeredCommands.Remove(command);
        }

        #endregion Command Unregistration

        #region Message Handling

        /// <summary>
        /// Handles a chat message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleChatMessage(IPlayer player, string message) => commandHandler.HandleChatMessage(player, message);

        /// <summary>
        /// Handles a console message
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool HandleConsoleMessage(IPlayer player, string message) => commandHandler.HandleConsoleMessage(player ?? consolePlayer, message);

        /// <summary>
        /// Parses the specified command
        /// </summary>
        /// <param name="argstr"></param>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        public void ParseCommand(string argstr, out string cmd, out string[] args)
        {
            List<string> arglist = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool inlongarg = false;
            foreach (char c in argstr)
            {
                if (c == '"')
                {
                    if (inlongarg)
                    {
                        string arg = sb.ToString().Trim();
                        if (!string.IsNullOrEmpty(arg))
                        {
                            arglist.Add(arg);
                        }

                        sb = new StringBuilder();
                        inlongarg = false;
                    }
                    else
                    {
                        inlongarg = true;
                    }
                }
                else if (char.IsWhiteSpace(c) && !inlongarg)
                {
                    string arg = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(arg))
                    {
                        arglist.Add(arg);
                    }

                    sb = new StringBuilder();
                }
                else
                {
                    sb.Append(c);
                }
            }
            if (sb.Length > 0)
            {
                string arg = sb.ToString().Trim();
                if (!string.IsNullOrEmpty(arg))
                {
                    arglist.Add(arg);
                }
            }
            if (arglist.Count == 0)
            {
                cmd = null;
                args = null;
                return;
            }

            cmd = arglist[0];
            arglist.RemoveAt(0);
            args = arglist.ToArray();
        }

        #endregion Message Handling

        #region Command Overriding

        /// <summary>
        /// Checks if a command can be overridden
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private bool CanOverrideCommand(string command)
        {
            if (!registeredCommands.TryGetValue(command, out RegisteredCommand cmd) || !cmd.Source.IsCorePlugin)
            {
                string[] split = command.Split('.');
                string parent = split.Length >= 2 ? split[0].Trim() : "global";
                string name = split.Length >= 2 ? string.Join(".", split.Skip(1).ToArray()) : split[0].Trim();
                return !SevenDaysExtension.RestrictedCommands.Contains(command) && !SevenDaysExtension.RestrictedCommands.Contains($"{parent}.{name}");
            }

            return true;
        }

        #endregion Command Overriding
    }
}
