// TextView.cs: multi-line text editing
using System.Collections.Generic;

namespace Terminal.Gui {
	public interface ITextModel {
		int Count { get; }
		List<RuneCell> GetLine (int line);
	}
}