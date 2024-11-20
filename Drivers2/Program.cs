using System.Collections.Concurrent;
using Terminal.Gui;
using static Terminal.Gui.WindowsConsole;

namespace Drivers2;

class Program
{
    static void Main (string [] args)
    {
        bool win = false;

        if (args.Length > 0)
        {
            if (args [0] == "net")
            {
                // default
            }
            else if(args [0] == "win")
            {
                win = true;
            }
            else
            {
                Console.WriteLine("Arg must be 'win' or 'net' or blank to use default");
            }
        }

        IMainLoopCoordinator coordinator;
        if (win)
        {
            var loop = new MainLoop<InputRecord> ();
            coordinator = new MainLoopCoordinator<InputRecord> (
                                                                ()=>new WindowsInput (),
                                                                ()=>new WindowsOutput (),
                                                                loop);
        }
        else
        {
            var loop = new MainLoop<ConsoleKeyInfo> ();
            coordinator = new MainLoopCoordinator<ConsoleKeyInfo> (()=>new NetInput (),
                                                                   ()=>new NetOutput (),
                                                                   loop);
        }

        // Register the event handler for Ctrl+C
        Console.CancelKeyPress += (s,e)=>
                                  {
                                      e.Cancel = true;
                                      coordinator.Stop ();
                                  };

        coordinator.StartBlocking ();
    }
}
