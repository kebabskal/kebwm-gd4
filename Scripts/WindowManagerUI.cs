using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vanara.PInvoke;

public partial class WindowManagerUI : Control {
	WindowManager wm;
	WeatherManager weatherManager;

	int barHeight = 28;
	double updateInterval = 0.25f;
	double updateIconInterval = 0.25f;
	List<Window> windowsWaitingForIcons = new();
	List<ScreenArea> screenAreas = new();
	int[] areaSizes = new int[] {
		1,2,1
	};

	public override void _Ready()
    {
        wm = new WindowManager();
        wm.WindowCreated += OnWindowCreated;
        wm.WindowDestroyed += OnWindowDestroyed;
        iconThread = new Thread(IconThread);
        iconThread.Start();
        Engine.MaxFps = 30;

        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, true);

        weatherManager = new WeatherManager();
        HideFromAltTab();
    }

    public static void HideFromAltTab() {
        // Hide window from alt-tab switcher
        HWND selfHWND = (HWND)(IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, 0);
        User32.SetWindowLong(selfHWND, User32.WindowLongFlags.GWL_EXSTYLE, (int)User32.WindowStylesEx.WS_EX_TOOLWINDOW);
    }

    double time = 0;
	double lastUpdate = 0;
	double lastIconUpdate = 0;
	Vector2I lastScreenSize = new Vector2I(0, 0);

	public override void _Process(double delta) {
		time += delta;

		if (time - lastIconUpdate > updateIconInterval) {
			foreach (var window in wm.Windows) {
				if (
					!window.Value.IconDenied &&
					window.Value.Icon == null &&
					!windowsWaitingForIcons.Contains(window.Value)
				) {
					windowsWaitingForIcons.Add(window.Value);
				}
			}

			lastIconUpdate = time;
		}
		if (time - lastUpdate > updateInterval) {
			wm.Update();
			lastUpdate = time;
		}

		// Setup screen area widths
		var screenSize = DisplayServer.ScreenGetSize();
		var screenRect = DisplayServer.ScreenGetUsableRect();
		var startX = screenRect.Position.X;
		var w4 = screenSize.X / 4;
		var x = 0;

		if (screenSize != lastScreenSize) {
			DisplayServer.WindowSetPosition(new Vector2I(startX, 0));
			DisplayServer.WindowSetSize(new Vector2I(screenRect.Size.X, barHeight));

			// Make sure we have the right number of ScreenAreas instantiated
			while (screenAreas.Count != areaSizes.Length) {
				// Add
				if (screenAreas.Count < areaSizes.Length) {
					var area = new ScreenArea();
					area.Initialize(wm);
					AddChild(area);
					screenAreas.Add(area);
				}

				// Destroy
				else if (screenAreas.Count > areaSizes.Length) {
					var area = screenAreas.Last();
					RemoveChild(area);
					area.QueueFree();
					screenAreas.Remove(area);
				}
			}

			for (int i = 0; i < screenAreas.Count; i++) {
				var width = areaSizes[i] * w4;
				if (i == 0)
					width -= startX;

				if (screenAreas[i].Bounds.Size.X != width)
					screenAreas[i].SetBounds(x, 0, width, screenSize.Y);

				x += width;
			}

			lastScreenSize = screenSize;
		}
	}

	Thread iconThread;
	void IconThread() {
		while (true) {
			if (windowsWaitingForIcons.Count == 0) {
				Thread.Sleep(100);
				continue;
			}

			var window = windowsWaitingForIcons[0];
			windowsWaitingForIcons.RemoveAt(0);
			try {
				window.GetIcon();
			} catch (Exception ex) {
				// Console.WriteLine($"Error getting icon for: {window.Title}\n{ex}");
				window.IconDenied = true;
			}
			Thread.Sleep(10);
		}
	}

	void OnWindowCreated(Window window) {
		if (!window.IsManageable)
			return;

		windowsWaitingForIcons.Add(window);
	}

	void OnWindowDestroyed(Window window) {

	}
}
