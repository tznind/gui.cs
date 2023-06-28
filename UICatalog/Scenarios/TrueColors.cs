﻿using System;
using Terminal.Gui;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "True Colors", Description: "Demonstration of true color support.")]
	[ScenarioCategory ("Colors")]
	public class TrueColors : Scenario {

		public override void Setup ()
		{
			var x = 2;
			var y = 1;

			var canTrueColor = Application.Driver.SupportsTrueColorOutput;

			var lblDriverName = new Label($"Current driver is {Application.Driver.GetType().Name}") {
				X = x,
				Y = y++
			};
			Win.Add(lblDriverName);
			y++;

			var cbSupportsTrueColor = new CheckBox ("Driver supports true color ") {
				X = x,
				Y = y++,
				Checked = canTrueColor,
				CanFocus = false
			};
			Win.Add (cbSupportsTrueColor);

			var cbUseTrueColor = new CheckBox ("Use true color") {
				X = x,
				Y = y++,
				Checked = Application.Driver.UseTrueColor,
				Enabled = canTrueColor,
			};
			cbUseTrueColor.Toggled += (_, _) => Application.Driver.UseTrueColor = (bool)cbUseTrueColor.Checked;
			Win.Add (cbUseTrueColor);

			y += 2;
			SetupGradient ("Red gradient", x, ref y, (i) => new TrueColor (i, 0, 0));
			SetupGradient ("Green gradient", x, ref y, (i) => new TrueColor (0, i, 0));
			SetupGradient ("Blue gradient", x, ref y, (i) => new TrueColor (0, 0, i));
			SetupGradient ("Yellow gradient", x, ref y, (i) => new TrueColor (i, i, 0));
			SetupGradient ("Magenta gradient", x, ref y, (i) => new TrueColor (i, 0, i));
			SetupGradient ("Cyan gradient", x, ref y, (i) => new TrueColor (0, i, i));
			SetupGradient ("Gray gradient", x, ref y, (i) => new TrueColor (i, i, i));

			Win.Add (new Label ("Mouse over to get the gradient view color:") {
				X = Pos.AnchorEnd (44),
				Y = 2
			});
			Win.Add (new Label ("Red:") {
				X = Pos.AnchorEnd (44),
				Y = 4
			});
			Win.Add (new Label ("Green:") {
				X = Pos.AnchorEnd (44),
				Y = 5
			});
			Win.Add (new Label ("Blue:") {
				X = Pos.AnchorEnd (44),
				Y = 6
			});

			var lblRed = new Label ("na") {
				X = Pos.AnchorEnd (32),
				Y = 4
			};
			Win.Add (lblRed);
			var lblGreen = new Label ("na") {
				X = Pos.AnchorEnd (32),
				Y = 5
			};
			Win.Add (lblGreen);
			var lblBlue = new Label ("na") {
				X = Pos.AnchorEnd (32),
				Y = 6
			};
			Win.Add (lblBlue);

			Application.RootMouseEvent = (e) => {
				var normal = e.View.GetNormalColor ();
				if (e.View != null) {
					lblRed.Text = normal.TrueColorForeground.Red.ToString();
					lblGreen.Text = normal.TrueColorForeground.Green.ToString ();
					lblBlue.Text = normal.TrueColorForeground.Blue.ToString ();
				}
			};
		}

		private void SetupGradient (string name, int x, ref int y, Func<int, TrueColor> colorFunc)
		{
			var gradient = new Label (name) {
				X = x,
				Y = y++,
			};
			Win.Add (gradient);
			for (int dx = x, i = 0; i <= 256; i += 4) {
				var l = new Label (" ") {
					X = dx++,
					Y = y,
					ColorScheme = new ColorScheme () { Normal = new Terminal.Gui.Attribute (
						colorFunc (i > 255 ? 255 : i).ToConsoleColor(),
						colorFunc (i > 255 ? 255 : i).ToConsoleColor()
						) }
				};
				Win.Add (l);
			}
			y+=2;
		}
	}
}