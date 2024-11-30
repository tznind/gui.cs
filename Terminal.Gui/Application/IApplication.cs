using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui;

public interface IApplication
{
    public IConsoleDriver Driver { get; }

    public void Shutdown ();
    public void RequestStop ();
    public void Init ();

}
