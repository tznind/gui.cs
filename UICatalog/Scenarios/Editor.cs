﻿using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Editor", Description: "A Terminal.Gui Text Editor via TextView")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("Text")]
	[ScenarioCategory ("Dialogs")]
	[ScenarioCategory ("TopLevel")]
	class Editor : Scenario {
		private string _fileName = "demo.txt";
		private TextView _textView;
		private bool _saved = true;
		private ScrollBarView _vertical;

		public override void Init (Toplevel top, ColorScheme colorScheme)
		{
			Application.Init ();
			Top = top;
			if (Top == null) {
				Top = Application.Top;
			}

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => New()),
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					null,
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => Copy()),
					new MenuItem ("C_ut", "", () => Cut()),
					new MenuItem ("_Paste", "", () => Paste())
				}),
				new MenuBarItem ("_ScrollBarView", CreateKeepChecked ())
			});
			Top.Add (menu);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.F2, "~F2~ Open", () => Open()),
				new StatusItem(Key.F3, "~F3~ Save", () => Save()),
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);

			CreateDemoFile (_fileName);

			Win = new Window (_fileName ?? "Untitled") {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				ColorScheme = colorScheme,
			};
			Top.Add (Win);

			_textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),

			};

			LoadFile ();

			Win.Add (_textView);

			_vertical = new ScrollBarView (_textView, true);
			var horizontal = new ScrollBarView (_textView, false);
			_vertical.OtherScrollBarView = horizontal;
			horizontal.OtherScrollBarView = _vertical;

			_vertical.ChangedPosition += () => {
				_textView.TopRow = _vertical.Position;
				if (_textView.TopRow != _vertical.Position) {
					_vertical.Position = _textView.TopRow;
				}
				_textView.SetNeedsDisplay ();
			};

			horizontal.ChangedPosition += () => {
				_textView.LeftColumn = horizontal.Position;
				if (_textView.LeftColumn != horizontal.Position) {
					horizontal.Position = _textView.LeftColumn;
				}
				_textView.SetNeedsDisplay ();
			};

			_textView.DrawContent += (e) => {
				_vertical.Size = _textView.Lines - 1;
				_vertical.ContentOffset = _textView.TopRow;
				horizontal.Size = _textView.Maxlength + 1;
				horizontal.ContentOffset = _textView.LeftColumn;
				_vertical.ColorScheme = horizontal.ColorScheme = _textView.ColorScheme;
				if (_vertical.ShowScrollIndicator) {
					_vertical.Redraw (e);
				}
				if (horizontal.ShowScrollIndicator) {
					horizontal.Redraw (e);
				}
			};
		}

		public override void Setup ()
		{
		}

		private void New ()
		{
			Win.Title = _fileName = "Untitled";
			throw new NotImplementedException ();
		}

		private void LoadFile ()
		{
			if (!_saved) {
				MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok");
			}

			if (_fileName != null) {
				// BUGBUG: #452 TextView.LoadFile keeps file open and provides no way of closing it
				//_textView.LoadFile(_fileName);
				_textView.Text = System.IO.File.ReadAllText (_fileName);
				Win.Title = _fileName;
				_saved = true;
			}
		}

		private void Paste ()
		{
			MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Cut ()
		{
			MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok");
		}

		private void Copy ()
		{
			MessageBox.ErrorQuery ("Not Implemented", "Functionality not yet implemented.", "Ok");
			//if (_textView != null && _textView.SelectedLength != 0) {
			//	_textView.Copy ();
			//}
		}

		private void Open ()
		{
			var d = new OpenDialog ("Open", "Open a file") { AllowsMultipleSelection = false };
			Application.Run (d);

			if (!d.Canceled) {
				_fileName = d.FilePaths [0];
				LoadFile ();
			}
		}

		private void Save ()
		{
			if (_fileName != null) {
				// BUGBUG: #279 TextView does not know how to deal with \r\n, only \r 
				// As a result files saved on Windows and then read back will show invalid chars.
				System.IO.File.WriteAllText (_fileName, _textView.Text.ToString());
				_saved = true;
			}
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private void CreateDemoFile(string fileName)
		{
			var sb = new StringBuilder ();
			// BUGBUG: #279 TextView does not know how to deal with \r\n, only \r
			sb.Append ("Hello world.\n");
			sb.Append ("This is a test of the Emergency Broadcast System.\n");

			for (int i = 0; i < 30; i++) {
				sb.Append ($"{i} - This is a test with a very long line and many lines to test the ScrollViewBar against the TextView. - {i}\n");
			}
			var sw = System.IO.File.CreateText (fileName);
			sw.Write (sb.ToString ());
			sw.Close ();
		}

		private MenuItem [] CreateKeepChecked ()
		{
			var item = new MenuItem ();
			item.Title = "Keep Content Always In Viewport";
			item.CheckType |= MenuItemCheckStyle.Checked;
			item.Checked = true;
			item.Action += () => _vertical.KeepContentAlwaysInViewport = item.Checked = !item.Checked;

			return new MenuItem [] { item };
		}

		public override void Run ()
		{
			base.Run ();
		}
	}
}
