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

        // Required to set up colors etc?
        Application.Init ();

        var top = CreateTestWindow ();

        IMainLoopCoordinator coordinator;
        if (win)
        {
            // TODO: We will need a nice factory for this constructor, it's getting a bit epic

            var inputBuffer = new ConcurrentQueue<InputRecord> ();
            var loop = new MainLoop<InputRecord> ();
            coordinator = new MainLoopCoordinator<InputRecord> (
                                                                ()=>new WindowsInput (),
                                                                inputBuffer,
                                                                new WindowsInputProcessor (inputBuffer),
                                                                ()=>new WindowsOutput (),
                                                                loop);
        }
        else
        {
            var inputBuffer = new ConcurrentQueue<ConsoleKeyInfo> ();
            var loop = new MainLoop<ConsoleKeyInfo> ();
            coordinator = new MainLoopCoordinator<ConsoleKeyInfo> (()=>new NetInput (),
                                                                   inputBuffer,
                                                                   new NetInputProcessor (inputBuffer),
                                                                   ()=>new NetOutput (),
                                                                   loop);
        }

        // Register the event handler for Ctrl+C
        Console.CancelKeyPress += (s,e)=>
                                  {
                                      e.Cancel = true;
                                      coordinator.Stop ();
                                  };

        BeginTestWindow (top);



        coordinator.StartBlocking ();
    }

    private static void BeginTestWindow (Toplevel top)
    {
        Application.Top = top;
    }

    private static Toplevel CreateTestWindow ()
    {
        var w = new Window
        {
            Title = "Hello World",
            Width = 30,
            Height = 5
        };

        var tf = new TextField { X = 5, Y = 0, Width = 10 };
        w.AdvanceFocus (NavigationDirection.Forward, null);
        w.Add (tf);

        var tf2 = new TextField { X = 5, Y = 2, Width = 10 };
        w.AdvanceFocus (NavigationDirection.Forward, null);
        w.Add (tf2);

        return w;
    }
}
