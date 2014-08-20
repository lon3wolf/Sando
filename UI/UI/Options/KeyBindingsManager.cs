using EnvDTE;
using EnvDTE80;
using Sando.DependencyInjection;
using Sando.UI.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sando.UI.Options
{
    public class KeyBindingsManager
    {
        private const string KEY_BINDINGS_MAPPED_FLAG = "keysMapped.txt";
        private static FlagManager keyBindingsMapped = new FlagManager(KEY_BINDINGS_MAPPED_FLAG);

        public static void RebindOnceOnly()
        {
            HijackFindInFilesKeyBinding();
        }

        private static void HijackFindInFilesKeyBinding()
        {
            if (keyBindingsMapped.DoesFlagExist())
            {
                var dte = ServiceLocator.Resolve<DTE2>();
                string[] toChange = { "Edit.FindinFiles", "View.Sando" };
                foreach (var commandString in toChange)
                    foreach (var command in dte.Commands)
                        if (((Command)command).Name.Contains(commandString))
                        {
                            SetKeyBindings((Command)command, new List<string>());
                            break;
                        }
                string[] toChangeCommand = { "Global::Alt+Shift+S", "Global::Ctrl+Shift+F" };
                int index = 0;
                foreach (var commandString in toChange)
                    foreach (var command in dte.Commands)
                        if (((Command)command).Name.Contains(commandString))
                        {
                            List<string> bindings = new List<string>();
                            bindings.Add(toChangeCommand[index++]);
                            SetKeyBindings((Command)command, bindings);
                            break;
                        }
                keyBindingsMapped.CreateFlag();
            }
        }

        private static void SetKeyBindings(Command command, IEnumerable<string> commandBindings)
        {
            try
            {
                var bindings = commandBindings.Cast<object>().ToArray();
                command.Bindings = bindings;
            }
            catch (COMException)
            {
                //don't care if this fails, as it is not crucial bro
            }
        }


       
    }
}
