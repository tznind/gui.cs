using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui;

public interface IApplication
{
    public IConsoleDriver Driver { get; }


    /// <summary>
    ///     Gets whether the application has been initialized with <see cref="Init"/> and not yet shutdown with <see cref="Shutdown"/>.
    /// </summary>
    public bool Initialized { get; set; }


    public void Shutdown ();
    public void RequestStop ();
    public void Init ();

}
