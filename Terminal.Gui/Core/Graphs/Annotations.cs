﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui.Graphs {
	/// <summary>
	/// <para>Describes an overlay element that is rendered either before or
	/// after a series.</para>
	/// 
	/// <para>Annotations can be positioned either in screen space (e.g.
	/// a legend) or in graph space (e.g. a line showing high point)
	/// </para>
	/// <para>Unlike <see cref="ISeries"/>, annotations are allowed to
	/// draw into graph margins
	/// </para>
	/// </summary>
	public interface IAnnotation {
		/// <summary>
		/// True if annotation should be drawn before <see cref="ISeries"/>.  This
		/// allowes Series and later annotations to potentially draw over the top
		/// of this annotation.
		/// </summary>
		bool BeforeSeries { get; }

		/// <summary>
		/// Called once after series have been rendered (or before if <see cref="BeforeSeries"/> is true).
		/// Use <see cref="View.Driver"/> to draw and <see cref="View.Bounds"/> to avoid drawing outside of
		/// graph
		/// </summary>
		/// <param name="graph"></param>
		void Render (GraphView graph);
	}


	/// <summary>
	/// Displays text at a given position (in screen space or graph space)
	/// </summary>
	public class TextAnnotation : IAnnotation {

		/// <summary>
		/// The location on screen to draw the <see cref="Text"/> regardless
		/// of scroll/zoom settings.  This overrides <see cref="GraphPosition"/>
		/// if specified.
		/// </summary>
		public Point? ScreenPosition { get; set; }

		/// <summary>
		/// The location in graph space to draw the <see cref="Text"/>.  This
		/// annotation will only show if the point is in the current viewable
		/// area of the graph presented in the <see cref="GraphView"/>
		/// </summary>
		public PointF GraphPosition { get; set; }

		/// <summary>
		/// Text to display on the graph
		/// </summary>
		public string Text { get; set; }

		/// <summary>
		/// True to add text before plotting series.  Defaults to false
		/// </summary>
		public bool BeforeSeries { get; set; }

		/// <summary>
		/// Draws the annotation
		/// </summary>
		/// <param name="graph"></param>
		public void Render (GraphView graph)
		{
			if (ScreenPosition.HasValue) {
				DrawText (graph, ScreenPosition.Value.X, ScreenPosition.Value.Y);
				return;
			}

			var screenPos = graph.GraphSpaceToScreen (GraphPosition);
			DrawText (graph, screenPos.X, screenPos.Y);
		}

		/// <summary>
		/// Draws the <see cref="Text"/> at the given coordinates with truncation to avoid
		/// spilling over <see name="View.Bounds"/> of the <paramref name="graph"/>
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="x">Screen x position to start drawing string</param>
		/// <param name="y">Screen y position to start drawing string</param>
		protected void DrawText (GraphView graph, int x, int y)
		{
			// the draw point is out of control bounds
			if (!graph.Bounds.Contains (new Point (x, y))) {
				return;
			}

			// There is no text to draw
			if (string.IsNullOrWhiteSpace (Text)) {
				return;
			}

			graph.Move (x, y);

			int availableWidth = graph.Bounds.Width - x;

			if (availableWidth <= 0) {
				return;
			}

			if (Text.Length < availableWidth) {
				View.Driver.AddStr (Text);
			} else {
				View.Driver.AddStr (Text.Substring (0, availableWidth));
			}
		}
	}

	/// <summary>
	/// A box containing symbol definitions e.g. meanings for colors in a graph.
	/// The 'Key' to the graph
	/// </summary>
	public class LegendAnnotation : IAnnotation {

		/// <summary>
		/// True to draw a solid border around the legend.
		/// Defaults to true.  This border will be within the
		/// <see cref="Bounds"/> and so reduces the width/height
		/// available for text by 2
		/// </summary>
		public bool Border { get; set; } = true;

		/// <summary>
		/// Defines the screen area available for the legend to render in
		/// </summary>
		public Rect Bounds { get; set; }

		/// <summary>
		/// Returns false i.e. Lengends render after series
		/// </summary>
		public bool BeforeSeries => false;

		/// <summary>
		/// Ordered collection of entries that are rendered in the legend.
		/// </summary>
		List<Tuple<GraphCellToRender, string>> entries = new List<Tuple<GraphCellToRender, string>> ();

		/// <summary>
		/// Creates a new empty legend at the given screen coordinates
		/// </summary>
		/// <param name="legendBounds">Defines the area available for the legend to render in
		/// (within the graph).  This is in screen units (i.e. not graph space)</param>
		public LegendAnnotation (Rect legendBounds)
		{
			Bounds = legendBounds;
		}

		/// <summary>
		/// Draws the Legend and all entries into the area within <see cref="Bounds"/>
		/// </summary>
		/// <param name="graph"></param>
		public void Render (GraphView graph)
		{
			if (Border) {
				graph.DrawFrame (Bounds, 0, true);
			}

			// start the legend at
			int y = Bounds.Top + (Border ? 1 : 0);
			int x = Bounds.Left + (Border ? 1 : 0);

			// how much horizontal space is available for writing legend entries?
			int availableWidth = Bounds.Width - (Border ? 2 : 0);
			int availableHeight = Bounds.Height - (Border ? 2 : 0);

			int linesDrawn = 0;

			foreach (var entry in entries) {

				if (entry.Item1.Color.HasValue) {
					Application.Driver.SetAttribute (entry.Item1.Color.Value);
				} else {
					graph.SetDriverColorToGraphColor ();
				}

				// add the symbol
				graph.AddRune (x, y + linesDrawn, entry.Item1.Rune);

				// switch to normal coloring (for the text)
				graph.SetDriverColorToGraphColor ();

				// add the text
				graph.Move (x + 1, y + linesDrawn);

				string str = TextFormatter.ClipOrPad (entry.Item2, availableWidth - 1);
				Application.Driver.AddStr (str);

				linesDrawn++;

				// Legend has run out of space
				if (linesDrawn >= availableHeight) {
					break;
				}
			}
		}


		/// <summary>
		/// Adds an entry into the legend.  Duplicate entries are permissable
		/// </summary>
		/// <param name="graphCellToRender">The symbol appearing on the graph that should appear in the legend</param>
		/// <param name="text">Text to render on this line of the legend.  Will be truncated
		/// if outside of Legend <see cref="Bounds"/></param>
		public void AddEntry (GraphCellToRender graphCellToRender, string text)
		{
			entries.Add (Tuple.Create (graphCellToRender, text));
		}
	}

	/// <summary>
	/// Sequence of lines to connect points e.g. of a <see cref="ScatterSeries"/>
	/// </summary>
	public class PathAnnotation : IAnnotation {

		/// <summary>
		/// Points that should be connected.  Lines will be drawn between points in the order
		/// they appear in the list
		/// </summary>
		public List<PointF> Points { get; set; } = new List<PointF> ();

		/// <summary>
		/// Color for the line that connects points
		/// </summary>
		public Attribute? LineColor { get; set; }

		/// <summary>
		/// The symbol that gets drawn along the line, defaults to '.'
		/// </summary>
		public Rune LineRune { get; set; } = new Rune ('.');

		/// <summary>
		/// True to add line before plotting series.  Defaults to false
		/// </summary>
		public bool BeforeSeries { get; set; }

		public bool UseBraille {get;set;}


		/// <summary>
		/// Draws lines connecting each of the <see cref="Points"/>
		/// </summary>
		/// <param name="graph"></param>
		public void Render (GraphView graph)
		{
			View.Driver.SetAttribute (LineColor ?? graph.ColorScheme.Normal);

			if(UseBraille) {
				RenderAsBraille (graph);
			}
			else {
				RenderAsLineRune (graph);
			}
		}

		private void RenderAsLineRune (GraphView graph)
		{
			foreach (var line in DiscardIfOffScreen(graph, PointsToLines ()))
			{
				var start = graph.GraphSpaceToScreen (line.Start);
				var end = graph.GraphSpaceToScreen (line.End);

				graph.DrawLine (start, end, LineRune);
			}
		}


		private void RenderAsBraille (GraphView graph)
		{
			// with Braille we can render 8 'pixels' (i.e 4x2)
			// per Rune instead of only 1. So we need to create
			// a coordinate space that is ScreenSpace upscaled 4x2

			// within the upscaled coordinate space what cells
			// are have line pass through them
			List<Point> upscaledLitPoints = new List<Point> ();
			int minScreenX = int.MaxValue;
			int maxScreenX = int.MinValue;
			int minScreenY = int.MaxValue;
			int maxScreenY = int.MinValue;

			// 'draw' all the lines at once
			foreach (var line in DiscardIfOffScreen(graph, PointsToLines ())) {


				var start = graph.GraphSpaceToScreen (line.Start);
				var end = graph.GraphSpaceToScreen (line.End);

				foreach(int x in new int []{ start.X,end.X }) {
					if(minScreenX > x) {
						minScreenX = x;
					}
					if(x > maxScreenX) {
						maxScreenX = x;
					}
				}
				foreach (int y in new int [] { start.Y, end.Y }) {
					if (minScreenX > y) {
						minScreenY = y;
					}
					if (y > maxScreenY) {
						maxScreenY = y;
					}
				}

				var upScaleStart = new Point (
				start.X * BitmapToBraille.CHAR_WIDTH,
				start.Y * BitmapToBraille.CHAR_HEIGHT);

				var upScaleEnd = new Point (
					end.X * BitmapToBraille.CHAR_WIDTH,
					end.Y * BitmapToBraille.CHAR_HEIGHT);
				
				// 'Draw' all the line points to the collection so we can render
				// a single Braille 'bitmap' that represents all the points smoothly
				graph.DrawLine (
					upScaleStart,
					upScaleEnd,
					(x, y) => {
						var p = new Point (x, y);
						if (!upscaledLitPoints.Contains (p)) {
							upscaledLitPoints.Add (p);
						}
					});
			}

			if(upscaledLitPoints.Count == 0) {
				return;
			}

			var upScaledMinX = upscaledLitPoints.Min (p => p.X);
			var upScaledMaxX = upscaledLitPoints.Max (p => p.X);
			var upScaledWidth = (upScaledMaxX - upScaledMinX) + 1;

			var upScaledMinY = upscaledLitPoints.Min (p => p.Y);
			var upScaledMaxY = upscaledLitPoints.Max (p => p.Y);
			var upScaledHeight = (upScaledMaxY - upScaledMinY) + 1;

			var builder = new BitmapToBraille (
				upScaledWidth,
				upScaledHeight,
				(x, y) => upscaledLitPoints.Contains (
					new Point (
						upScaledMinX + x,
						upScaledMinY + y))
					);

			var runes = builder.GenerateImage ().Split ('\n');
			
			for (int y = 0; y < runes.Length; y++) {
				var line = runes [y];

				for (int x = 0; x < line.Length; x++) {

					var rune = line [x];
					if (rune != ' ') {
						graph.AddRune (
							x + minScreenX,
							y + minScreenY, rune);
					}
				}
			}

		}

		private IEnumerable<LineF> DiscardIfOffScreen (GraphView graph, IEnumerable<LineF> lines)
		{
			// Note that margin and title etc will mean this calculation is inaccurate but
			// it is over conservative so that's ok
			var ul = graph.ScreenToGraphSpace (0, 0);
			var lr = graph.ScreenToGraphSpace (graph.Bounds.Width, graph.Bounds.Height);


			foreach (var line in lines) {
				if(line.Start.X < ul.X && line.End.X < ul.X) {
					continue;
				}
				if (line.Start.X > lr.X && line.End.X > lr.X) {
					continue;
				}

				if (line.Start.Y > ul.Y && line.End.Y > ul.Y) {
					continue;
				}
				if (line.Start.Y < lr.Y && line.End.Y < lr.Y) {
					continue;
				}

				yield return line;
			}
		}


		/// <summary>
		/// Generates lines joining <see cref="Points"/> 
		/// </summary>
		/// <returns></returns>
		private IEnumerable<LineF> PointsToLines ()
		{
			for (int i = 0; i < Points.Count - 1; i++) {
				yield return new LineF (Points [i], Points [i + 1]);
			}
		}

		/// <summary>
		/// Describes two points in graph space and a line between them
		/// </summary>
		public class LineF {
			/// <summary>
			/// The start of the line
			/// </summary>
			public PointF Start { get; }

			/// <summary>
			/// The end point of the line
			/// </summary>
			public PointF End { get; }

			/// <summary>
			/// Creates a new line between the points
			/// </summary>
			public LineF (PointF start, PointF end)
			{
				this.Start = start;
				this.End = end;
			}
		}
	}
}