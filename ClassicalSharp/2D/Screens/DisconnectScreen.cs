﻿// Copyright 2014-2017 ClassicalSharp | Licensed under BSD-3
using System;
using System.Drawing;
using ClassicalSharp.Gui.Widgets;
using OpenTK.Input;

namespace ClassicalSharp.Gui.Screens {
	public class DisconnectScreen : ClickableScreen {
		
		string title, message;
		readonly Font titleFont, messageFont;
		Widget[] widgets;
		DateTime initTime, clearTime;
		bool canReconnect;
		
		public DisconnectScreen(Game game, string title, string message) : base(game) {
			this.title = title;
			this.message = message;
			
			string reason = Utils.StripColours(message);
			canReconnect = !(reason.StartsWith("Kicked ") || reason.StartsWith("Banned "));
			
			titleFont = new Font(game.FontName, 16, FontStyle.Bold);
			messageFont = new Font(game.FontName, 16);
			BlocksWorld = true;
			HidesHud = true;
			HandlesAllInput = true;
		}
		
		public override void Render(double delta) {
			if (canReconnect) UpdateDelayLeft(delta);
			
			// NOTE: We need to make sure that both the front and back buffers have
			// definitely been drawn over, so we redraw the background multiple times.
			if (DateTime.UtcNow < clearTime)
				Redraw(delta);
		}
		
		public override void Init() {
			game.SkipClear = true;
			game.Graphics.ContextLost += ContextLost;
			game.Graphics.ContextRecreated += ContextRecreated;
			
			ContextRecreated();
			initTime = DateTime.UtcNow;
			lastSecsLeft = delay;
		}

		public override void Dispose() {
			game.SkipClear = false;
			game.Graphics.ContextLost -= ContextLost;
			game.Graphics.ContextRecreated -= ContextRecreated;
			
			ContextLost();
			titleFont.Dispose();
			messageFont.Dispose();
		}
		
		public override void OnResize(int width, int height) {
			RepositionWidgets(widgets);
			clearTime = DateTime.UtcNow.AddSeconds(0.5);
		}

		
		public override bool HandlesKeyDown(Key key) { return key < Key.F1 || key > Key.F35; }
		
		public override bool HandlesKeyPress(char key) { return true; }
		
		public override bool HandlesKeyUp(Key key) { return true; }
		
		public override bool HandlesMouseClick(int mouseX, int mouseY, MouseButton button) {
			return HandleMouseClick(widgets, mouseX, mouseY, button);
		}
		
		public override bool HandlesMouseMove(int mouseX, int mouseY) {
			return HandleMouseMove(widgets, mouseX, mouseY);
		}
		
		public override bool HandlesMouseScroll(float delta) { return true; }
		
		public override bool HandlesMouseUp(int mouseX, int mouseY, MouseButton button) { return true; }
		
		
		int lastSecsLeft;
		const int delay = 5;
		bool lastActive = false;
		void UpdateDelayLeft(double delta) {
			ButtonWidget btn = (ButtonWidget)widgets[2];
			double elapsed = (DateTime.UtcNow - initTime).TotalSeconds;
			int secsLeft = Math.Max(0, (int)(delay - elapsed));
			if (lastSecsLeft == secsLeft && btn.Active == lastActive) return;
			
			btn.SetText(ReconnectMessage());
			btn.Disabled = secsLeft != 0;
			
			Redraw(delta);
			lastSecsLeft = secsLeft;
			lastActive = btn.Active;
			clearTime = DateTime.UtcNow.AddSeconds(0.5);
		}
		
		readonly FastColour top = new FastColour(64, 32, 32), bottom = new FastColour(80, 16, 16);
		void Redraw(double delta) {
			game.Graphics.Draw2DQuad(0, 0, game.Width, game.Height, top, bottom);
			game.Graphics.Texturing = true;
			RenderWidgets(widgets, delta);
			game.Graphics.Texturing = false;
		}
		
		void ReconnectClick(Game g, Widget w, MouseButton btn, int x, int y) {
			if (btn != MouseButton.Left) return;
			string connectString = "Connecting to " + game.IPAddress + ":" + game.Port +  "..";
			for (int i = 0; i < game.Components.Count; i++)
				game.Components[i].Reset(game);
			BlockInfo.Reset();
			
			game.Gui.SetNewScreen(new LoadingMapScreen(game, connectString, ""));
			game.Server.Connect(game.IPAddress, game.Port);
		}
		
		string ReconnectMessage() {
			if (!canReconnect) return "Reconnect";
			
			double elapsed = (DateTime.UtcNow - initTime).TotalSeconds;
			int secsLeft = (int)(delay - elapsed);
			return secsLeft > 0 ? "Reconnect in " + secsLeft : "Reconnect";
		}
		
		protected override void ContextLost() { DisposeWidgets(widgets); }
		
		protected override void ContextRecreated() {
			if (game.Graphics.LostContext) return;
			clearTime = DateTime.UtcNow.AddSeconds(0.5);
			widgets = new Widget[canReconnect ? 3 : 2];
			
			widgets[0] = TextWidget.Create(game, title, titleFont)
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, -30);
			widgets[1] = TextWidget.Create(game, message, messageFont)
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, 10);
			
			string msg = ReconnectMessage();
			if (!canReconnect) return;
			widgets[2] = ButtonWidget.Create(game, 300, msg, titleFont, ReconnectClick)
				.SetLocation(Anchor.Centre, Anchor.Centre, 0, 80);
		}
	}
}
