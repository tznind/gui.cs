﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Syntax Highlighting", Description: "Text editor with keyword highlighting using the TextView control.")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("TextView")]
	public class SyntaxHighlighting : Scenario {

		TextView textView;
		MenuItem miWrap;
		private Attribute magenta;
		private Attribute white;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Application.Top.LayoutSubviews ();

			magenta = new Attribute(Color.Magenta, Color.Black);
			white = new Attribute (Color.White, Color.Black);

			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				miWrap =  new MenuItem ("_Word Wrap", "", () => WordWrap()){CheckType = MenuItemCheckStyle.Checked},
				new MenuItem ("_Quit", "", () => Quit()),
			})
			});
			Application.Top.Add (menu);

			textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			textView.Text = "SELECT TOP 100 * \nfrom\n MyDb.dbo.Biochemistry;";

			Win.Add (textView);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});

			ApplyHighlighting ();

			textView.KeyPress += (s,e)=> ApplyHighlighting ();

			Application.Top.Add (statusBar);
		}

		private void ApplyHighlighting ()
		{
			bool areInQuotes = false;
			var quoteRune = new Rune ('"');

			var textModel = textView.TextViewModel;
			for (int y=0;y< textModel.Count;y++) {
				
				var line = textView.TextViewModel.GetLine (y);

				for(int x=0;x<line.Count;x++) {
					if (line [x].Rune == quoteRune) {
						areInQuotes = !areInQuotes;
					}
					if(areInQuotes) {
						line [x].Attribute = magenta;
					} else {
						line [x].Attribute = white;
					}
				}
			}
			
		}

		private void WordWrap ()
		{
			miWrap.Checked = !miWrap.Checked;
			textView.WordWrap = (bool)miWrap.Checked;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
		/*
		private class SqlTextView : TextView {

			private HashSet<string> keywords = new HashSet<string> (StringComparer.CurrentCultureIgnoreCase);
			private Attribute blue;

			public void Init ()
			{
				keywords.Add ("select");
				keywords.Add ("distinct");
				keywords.Add ("top");
				keywords.Add ("from");
				keywords.Add ("create");
				keywords.Add ("CIPHER");
				keywords.Add ("CLASS_ORIGIN");
				keywords.Add ("CLIENT");
				keywords.Add ("CLOSE");
				keywords.Add ("COALESCE");
				keywords.Add ("CODE");
				keywords.Add ("COLUMNS");
				keywords.Add ("COLUMN_FORMAT");
				keywords.Add ("COLUMN_NAME");
				keywords.Add ("COMMENT");
				keywords.Add ("COMMIT");
				keywords.Add ("COMPACT");
				keywords.Add ("COMPLETION");
				keywords.Add ("COMPRESSED");
				keywords.Add ("COMPRESSION");
				keywords.Add ("CONCURRENT");
				keywords.Add ("CONNECT");
				keywords.Add ("CONNECTION");
				keywords.Add ("CONSISTENT");
				keywords.Add ("CONSTRAINT_CATALOG");
				keywords.Add ("CONSTRAINT_SCHEMA");
				keywords.Add ("CONSTRAINT_NAME");
				keywords.Add ("CONTAINS");
				keywords.Add ("CONTEXT");
				keywords.Add ("CONTRIBUTORS");
				keywords.Add ("COPY");
				keywords.Add ("CPU");
				keywords.Add ("CURSOR_NAME");
				keywords.Add ("primary");
				keywords.Add ("key");
				keywords.Add ("insert");
				keywords.Add ("alter");
				keywords.Add ("add");
				keywords.Add ("update");
				keywords.Add ("set");
				keywords.Add ("delete");
				keywords.Add ("truncate");
				keywords.Add ("as");
				keywords.Add ("order");
				keywords.Add ("by");
				keywords.Add ("asc");
				keywords.Add ("desc");
				keywords.Add ("between");
				keywords.Add ("where");
				keywords.Add ("and");
				keywords.Add ("or");
				keywords.Add ("not");
				keywords.Add ("limit");
				keywords.Add ("null");
				keywords.Add ("is");
				keywords.Add ("drop");
				keywords.Add ("database");
				keywords.Add ("table");
				keywords.Add ("having");
				keywords.Add ("in");
				keywords.Add ("join");
				keywords.Add ("on");
				keywords.Add ("union");
				keywords.Add ("exists");

				Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator () {
					AllSuggestions = keywords.ToList ()
				};

				blue = Driver.MakeAttribute (Color.Cyan, Color.Black);
			}


			protected override void SetNormalColor (List<RuneCell> line, int idx)
			{
				if (IsKeyword (line.Select(c => c.Rune).ToList(), idx)) {
					Driver.SetAttribute (blue);
				}
			}

			private bool IsInStringLiteral (List<Rune> line, int idx)
			{
				string strLine = new string (line.Select (r => (char)r.Value).ToArray ());

				foreach (Match m in Regex.Matches (strLine, "'[^']*'")) {
					if (idx >= m.Index && idx < m.Index + m.Length) {
						return true;
					}
				}

				return false;
			}

			private bool IsKeyword (List<Rune> line, int idx)
			{
				var word = IdxToWord (line, idx);

				if (string.IsNullOrWhiteSpace (word)) {
					return false;
				}

				return keywords.Contains (word, StringComparer.CurrentCultureIgnoreCase);
			}

			private string IdxToWord (List<Rune> line, int idx)
			{
				var words = Regex.Split (
					new string (line.Select (r => (char)r.Value).ToArray ()),
					"\\b");

				int count = 0;
				string current = null;

				foreach (var word in words) {
					current = word;
					count += word.Length;
					if (count > idx) {
						break;
					}
				}

				return current?.Trim ();
			}
		}*/
	}
}
