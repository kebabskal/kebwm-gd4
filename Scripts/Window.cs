using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Godot;
using Vanara.PInvoke;

public class Window {
	public static string[] BorderList = new String[] {
		"chrome",
		"spotify",
		"applicationframehost",
		"editor\\unity.exe",
		"explorer.exe",
		"godot",
	};

	public static string[] TitleBlacklist = new String[] {
		"Microsoft Text Input Application",
		"Windows indataupplevelse",
		"Program Manager",
		"Flow.Launcher",
	};

	public WindowManager WindowManager;
	public HWND Hwnd;
	public string Title = "";
	public Process Process;
	public Rect2I Rectangle;
	public bool BorderHack = false;
	public bool CompactHack = false;

	public bool IconDenied = false;
	public Texture2D Icon;
	public event System.Action IconChanged;

	public bool IsMinimized => User32.IsIconic(Hwnd);
	public bool IsValid => User32.IsWindow(Hwnd);
	public bool IsVisible => User32.IsWindowVisible(Hwnd);
	public bool IsTopLevel => User32.GetParent(Hwnd) == HWND.NULL;

	public bool IsManageable =>
		IsValid &&
		IsVisible &&
		IsTopLevel &&
		Rectangle.Size.X > 1 &&
		Rectangle.Size.Y > 1 &&
		Title != "" &&
		!TitleBlacklist.Contains(Title) &&
		!IsMinimized
	;

	public Window(WindowManager windowManager, HWND hwnd, Process process) {
		WindowManager = windowManager;
		Hwnd = hwnd;
		Process = process;
		try {
			var filename = Process.MainModule.FileName.ToLower();
			BorderHack = BorderList.Any(bl => filename.Contains(bl));
		} catch {
		}
	}

	public void GetIcon() {
		if (Process == null) {
			Console.WriteLine($"GetIcon {this} Null Process");
			return;
		}

		var icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.MainModule.FileName);
		if (icon != null) {
			var iconBitmap = icon.ToBitmap();
			// Console.WriteLine($"GetIcon {this}");
			Icon = GetTexture(iconBitmap);
			IconChanged?.Invoke();
		} else {
			// Console.WriteLine($"GetIcon {this} Null Icon");
		}
	}

	public override string ToString() {
		return $"{Title} ({((IntPtr)Hwnd).ToString()}) ({Rectangle})";
	}

	void SetActiveThread() {
		User32.SetActiveWindow(Hwnd);
		User32.SetForegroundWindow(Hwnd);
	}

	public void SetActive() {
		Console.WriteLine($"Activating: {Title}");
		var thread = new Thread(SetActiveThread);
		thread.Start();
	}

	Texture2D GetTexture(System.Drawing.Bitmap bmp) {
		var graphics = Graphics.FromImage(bmp);
		Godot.Image testImage = new Godot.Image();

		using (MemoryStream ms = new MemoryStream()) {
			bmp.Save(ms, ImageFormat.Png);
			ms.Position = 0;
			testImage.LoadPngFromBuffer(ms.ToArray());
		}

		ImageTexture imageTex = new ImageTexture();
		return ImageTexture.CreateFromImage(testImage);
	}

	public void SetSize(Rect2I rectangle) {
		var b = 8;
		var left = rectangle.Position.X - 8;
		var top = rectangle.Position.Y;
		var width = rectangle.Size.X;
		var height = rectangle.Size.Y;

		if (BorderHack) {
			left -= b;
			width += b * 2;
		}

		if (CompactHack) {
			top -= 53;
			height += 53;
		}

		User32.MoveWindow(Hwnd, left, top, width, height, true);
	}

	internal void ToggleCompact() {
		CompactHack = !CompactHack;
	}
}


