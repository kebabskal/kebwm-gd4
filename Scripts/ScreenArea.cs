using System;
using System.Collections.Generic;
using Godot;

public partial class ScreenArea : HBoxContainer {
	WindowManager wm;
	List<WindowButton> buttons = new();
	Rect2I rect = new Rect2I(0, 0, 0, 0);

	public Rect2I Bounds => rect;

	HBoxContainer iconBox;
	HBoxContainer infoBox;
	Label timeLabel;
	Label weatherLabel;
	VSeparator separator;


	WindowButton FindButton(Window window) {
		return buttons.Find(button => button.Window == window);
	}

	public void Initialize(WindowManager wm) {
		this.wm = wm;

		wm.WindowCreated += OnWindowCreated;
		wm.WindowDestroyed += OnWindowDestroyed;
		wm.WindowRectangleChanged += OnWindowRectangleChanged;
		wm.ForegroundWindowChanged += OnForegroundWindowChanged;
		wm.WindowTitleChanged += OnWindowTitleChanged;

		RefreshButtons();

		iconBox = new HBoxContainer();
		iconBox.SizeFlagsHorizontal = SizeFlags.Expand;
		AddChild(iconBox);

		infoBox = new HBoxContainer();
		AddChild(infoBox);

		AddSeparator(infoBox);

		var onTopButton = new Button();
		onTopButton.Text = "T";
		onTopButton.Pressed += () => {
			var onTop = !DisplayServer.WindowGetFlag(DisplayServer.WindowFlags.AlwaysOnTop);
			onTopButton.Text = onTop ? "T" : "B";

			DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.AlwaysOnTop, onTop);
			WindowManagerUI.HideFromAltTab();
		};
		infoBox.AddChild(onTopButton);

		AddSeparator(infoBox);

		var quitButton = new Button();
		quitButton.Text = "R";
		quitButton.Pressed += () => {
			WindowManager.Instance.Reenumerate();
			WindowManagerUI.HideFromAltTab();
		};
		infoBox.AddChild(quitButton);


		AddSeparator(infoBox);

		weatherLabel = new Label();
		weatherLabel.Text = "loading...";
		weatherLabel.VerticalAlignment = VerticalAlignment.Center;
		weatherLabel.CustomMinimumSize = new Vector2(0, 24);
		infoBox.AddChild(weatherLabel);

		AddSeparator(infoBox);

		timeLabel = new Label();
		timeLabel.Text = "Clock";
		timeLabel.VerticalAlignment = VerticalAlignment.Center;
		timeLabel.CustomMinimumSize = new Vector2(0, 24);
		infoBox.AddChild(timeLabel);

		AddSeparator(infoBox);

		var groupButton = new Button();
		groupButton.Text = "G";
		groupButton.Pressed += OnGroupButtonPressed;
		infoBox.AddChild(groupButton);

		AddSeparator(infoBox);
	}

    void OnWindowTitleChanged(Window window) {
		CreateButtonIfNotExists(window);
    }

	void CreateButtonIfNotExists(Window window) {
		var isManageable = window.IsManageable;
		var isWindowInside = IsWindowInside(window);

		if (!isManageable || !isWindowInside)
			return;

		
		var button = FindButton(window);
		if (button != null)
			return;

		button = new WindowButton();
		button.Initialize(window, this);
		iconBox.AddChild(button);
		buttons.Add(button);
	}

    void OnGroupButtonPressed() {
		foreach (var button in buttons) {
			MaximizeInArea(button.Window);
		}
	}

	public void MaximizeInArea(Window window) {
		var windowSize = DisplayServer.WindowGetSize();
		var windowPosition = DisplayServer.WindowGetPosition();
		var screenSize = DisplayServer.ScreenGetSize();
		window.SetSize(
			new Rect2I(
				(int)Position.X + windowPosition.X,
				windowSize.Y,
				Bounds.Size.X,
				screenSize.Y - windowSize.Y
			)
		);
	}

	public void ToggleCompact(Window window) {
		window.CompactHack = !window.CompactHack;
		MaximizeInArea(window);
	}

	public void ActivateWindow(Window window) {
		window.SetActive();
		foreach (var button in buttons)
			button.IsFrontmost = button.Window == window;
	}

	void AddSeparator(Container container) {
		var separator = new VSeparator();
		container.AddChild(separator);
	}

	void OnWindowCreated(Window window) {
		CreateButtonIfNotExists(window);
	}

	void OnWindowDestroyed(Window window) {
		var button = FindButton(window);
		if (button == null)
			return;

		buttons.Remove(button);
		iconBox.RemoveChild(button);
		button.QueueFree();
	}

	void OnWindowRectangleChanged(Window window) {
		
		if (!IsWindowInside(window)) {
			var button = FindButton(window);
			if (button == null)
				return;

			buttons.Remove(button);
			iconBox.RemoveChild(button);
			button.QueueFree();
		} else {
			CreateButtonIfNotExists(window);
		}
	}

	public void SetBounds(int x, int y, int width, int height) {
		rect = new Rect2I(x, y, width, height);
		Position = new Vector2(x + 8, y);
		Size = new Vector2(width, 0);
		RefreshButtons();
	}

	bool IsWindowInside(Window window) {
		var center = window.Rectangle.GetCenter();
		return rect.HasPoint(center);
	}

	void RefreshButtons() {
		foreach (var button in buttons) {
			if (!IsWindowInside(button.Window)) {
				RemoveChild(button);
				button.QueueFree();
				buttons.Remove(button);
			}
		}

		foreach (var window in wm.Windows.Values) {
			OnWindowCreated(window);
		}
	}

	void OnForegroundWindowChanged(Window window) {
		var foregroundButton = FindButton(window);
		if (foregroundButton != null) {
			foreach (var button in buttons) {
				button.IsFrontmost = button == foregroundButton;
			}
		}
	}

	double time = 0;
	double lastUpdate = -99f;
	public override void _Process(double delta) {
		time += delta;
		base._Process(delta);

		// Update Clock every second
		if (time - lastUpdate > 1f) {
			var dateString = System.DateTime.Now.ToString("ddd - yyyy-MM-dd - HH:mm:ss").ToUpper();
			var week = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(System.DateTime.Now, System.Globalization.CalendarWeekRule.FirstFourDayWeek, System.DayOfWeek.Monday);
			dateString = $"W{week} " + dateString;

			timeLabel.Text = dateString;
			lastUpdate = time;

			weatherLabel.Text = WeatherManager.Instance?.CurrentTemperature;
		}
	}

}