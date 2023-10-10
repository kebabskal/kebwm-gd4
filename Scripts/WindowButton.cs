using System;
using System.Linq;
using Godot;
using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

public partial class WindowButton : TextureButton {
	public Window Window => window;
	public bool IsFrontmost = false;

	WindowManager wm;
	Window window;
	ColorRect colorRect;

	bool isHovered = false;

	Color accentColor = Color.FromHtml("31a8ff");
	MouseButton lastMouseButton;
	ScreenArea screenArea;

	public void Initialize(Window window, ScreenArea screenArea) {
		this.window = window;
		this.screenArea = screenArea;
		window.IconChanged += () => {
			TextureNormal = window.Icon;
		};
		if (window.Icon != null)
			TextureNormal = window.Icon;

		IgnoreTextureSize = true;
		StretchMode = StretchModeEnum.KeepAspectCentered;
		CustomMinimumSize = new Vector2(40, 20);
		colorRect = new ColorRect();
		colorRect.Color = Colors.Transparent;
		colorRect.Size = new Vector2(40, 2);
		colorRect.Position = new Vector2(0, 27);
		AddChild(colorRect);

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;
	}

	HWND previewHWND;
	Godot.Window currentPopup;
	void OnMouseEntered() {
		isHovered = true;

		GetViewport().GuiEmbedSubwindows = false;
		var windowPosition = DisplayServer.WindowGetPosition(0);
		currentPopup = new Godot.Window();
		currentPopup.Borderless = true;
		currentPopup.Size = new Vector2I(200, 100);
		currentPopup.Position = new Vector2I((int)(GlobalPosition.X + windowPosition.X), 30);
		currentPopup.Unfocusable = true;
		AddChild(currentPopup);

		var label = new Label();
		label.Text = window.Title.Substring(0, Mathf.Min(35, window.Title.Length));
		label.Position = new Vector2(10, 5);
		currentPopup.AddChild(label);

		var id = DisplayServer.GetWindowAtScreenPosition(currentPopup.Position);
		HWND hwnd = (HWND)(IntPtr)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, id);

		var result = DwmApi.DwmRegisterThumbnail(hwnd, window.Hwnd, out var thumbnailId);
		if (result.Succeeded) {
			DwmApi.DwmQueryThumbnailSourceSize(thumbnailId, out var size);
			int height = size.Height / 10;
			int width = size.Width / 10;
			User32.SetWindowRgn(hwnd, Gdi32.CreateRoundRectRgn(0, 0, width, height, 10, 10), true);

			currentPopup.Size = new Vector2I(width, height + 160);
			currentPopup.Position = new Vector2I(
				Mathf.Max(windowPosition.X + (int)GetParent<Control>().GlobalPosition.X, currentPopup.Position.X - Mathf.RoundToInt((float)width / 2 - Size.X / 2)),
				currentPopup.Position.Y
			);

			DwmApi.DWM_THUMBNAIL_PROPERTIES thumbProps = new DwmApi.DWM_THUMBNAIL_PROPERTIES();
			thumbProps.dwFlags = DwmApi.DWM_TNP.DWM_TNP_RECTDESTINATION | DwmApi.DWM_TNP.DWM_TNP_VISIBLE;
			thumbProps.rcDestination = new RECT(0, 30, width, height + 30);
			thumbProps.fVisible = true;

			DwmApi.DwmUpdateThumbnailProperties(thumbnailId, thumbProps);
		}
	}

	void OnMouseExited() {
		isHovered = false;
		// User32.DestroyWindow(previewHWND);
		if (currentPopup != null)
			currentPopup.QueueFree();
	}

	public override void _GuiInput(InputEvent @event) {
		base._GuiInput(@event);
		var inputEvent = @event as InputEventMouseButton;
		if (inputEvent != null && inputEvent.Pressed) {
			if (inputEvent.ButtonIndex == MouseButton.Left) {
				screenArea.ActivateWindow(window);

			} else if (inputEvent.ButtonIndex == MouseButton.Right) {
				screenArea.MaximizeInArea(window);
			} else if (inputEvent.ButtonIndex == MouseButton.Middle) {
				screenArea.ToggleCompact(window);
			}
		}
	}

	public override void _Process(double delta) {
		base._Process(delta);

		CustomMinimumSize = new Vector2(Mathf.Lerp(CustomMinimumSize.X, IsFrontmost ? 60 : 40, (float)delta * 10f), 20);
		colorRect.Size = new Vector2(CustomMinimumSize.X, 2);
		SelfModulate = IsFrontmost || isHovered ? Colors.White : Colors.DimGray;
		colorRect.Color = IsFrontmost ? accentColor : Colors.Transparent;
	}
}