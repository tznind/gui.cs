using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.ConsoleDrivers.V2;


public class ApplicationV2 : IApplication
{
    /// <inheritdoc />
    public void Init (IConsoleDriver driver = null, string driverName = null)
    {

    }

    /// <inheritdoc />
    public Toplevel Run (Func<Exception, bool> errorHandler = null, IConsoleDriver driver = null) { throw new NotImplementedException (); }

    /// <inheritdoc />
    public T Run<T> (Func<Exception, bool> errorHandler = null, IConsoleDriver driver = null) where T : Toplevel, new () { throw new NotImplementedException (); }

    /// <inheritdoc />
    public void Run (Toplevel view, Func<Exception, bool> errorHandler = null) { throw new NotImplementedException (); }

    /// <inheritdoc />
    public void Shutdown () { throw new NotImplementedException (); }
}
