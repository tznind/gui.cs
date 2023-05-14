using System.IO.Abstractions;

namespace Terminal.Gui {

	/// <summary>
	/// Arguments for the <see cref="FileDialogStyle.IconGetter"/> delegate
	/// </summary>
	public class FileIconGetterArgs {

		/// <summary>
		/// Creates a new instance of the class
		/// </summary>
		public FileIconGetterArgs (string currentDirectory, IFileSystemInfo file, FileDialogIconGetterContext context)
		{
			CurrentDirectory = currentDirectory;
			Context = context;
		}

		/// <summary>
		/// Gets the currently open directory
		/// </summary>
		public string CurrentDirectory { get; }

		/// <summary>
		/// Gets the file/folder for which the icon is required.
		/// </summary>
		public IFileSystemInfo File { get; }

		/// <summary>
		/// Gets the context in which the icon will be used in.
		/// </summary>
		public FileDialogIconGetterContext Context { get; }
	}
}