using uMod;
using System.Reflection;
using uMod.Plugins;
using uMod.SevenDaysToDie.Libraries;

namespace uMod.Plugins
{
    public abstract class SevenDaysPlugin : CSharpPlugin
    {
        protected Command cmd = Interface.uMod.GetLibrary<Command>();
        
        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attributes = method.GetCustomAttributes(typeof(ConsoleCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    ConsoleCommandAttribute attribute = attributes[0] as ConsoleCommandAttribute;
                    cmd.AddConsoleCommand(attribute?.Command, this, method.Name);
                    continue;
                }

                attributes = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    ChatCommandAttribute attribute = attributes[0] as ChatCommandAttribute;
                    cmd.AddChatCommand(attribute?.Command, this, method.Name);
                }
            }

            base.HandleAddedToManager(manager);
        }
    }
}

