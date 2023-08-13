using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using GtkNetPanel.Interop.X11;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;
using Microsoft.Extensions.Hosting;

namespace GtkNetPanel.Services.X11;

public class XLibAdaptorService : IDisposable
{
	private XWindowRef _rootWindowRef;
	private readonly IHostApplicationLifetime _applicationLifetime;
	private readonly Subject<XWindowRef> _windowCreated = new();
	private readonly Subject<XWindowRef> _windowRemoved = new();
	private List<XWindowRef> _knownWindows = new();

	public XLibAdaptorService(IHostApplicationLifetime applicationLifetime)
	{
		_applicationLifetime = applicationLifetime;
	}

	public IObservable<XWindowRef> WindowCreated => _windowCreated;
	public IObservable<XWindowRef> WindowRemoved => _windowRemoved;

	public void Initialize()
	{
		XLib.XInitThreads();
		var display = XLib.XOpenDisplay(0);
		var window = XLib.XDefaultRootWindow(display);
		XLib.XSelectInput(display, window, EventMask.SubstructureNotifyMask | EventMask.PropertyChangeMask);
		_rootWindowRef = new XWindowRef() { Window = window, Display = display };
		Task.Run(() => WatchEvents(_applicationLifetime.ApplicationStopping));
	}

	private void WatchEvents(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			var eventPointer = Marshal.AllocHGlobal(24 * sizeof(long));
			XLib.XNextEvent(_rootWindowRef.Display, eventPointer);
			var someEvent = Marshal.PtrToStructure<XAnyEvent>(eventPointer);

			if (someEvent.type == (int) Event.PropertyNotify)
			{
				var e = Marshal.PtrToStructure<XPropertyEvent>(eventPointer);

				if (e.atom != XAtoms.NetClientList)
				{
					HandleWindowListUpdate();
				}
			}

			XLib.XFree(eventPointer);
		}
	}

	private void HandleWindowListUpdate()
	{
		var currentWindowList = GetNormalWindows();

		foreach (var w in currentWindowList)
		{
			if (_knownWindows.All(x => x.Window != w.Window))
			{
				_windowCreated.OnNext(w);
			}
		}

		foreach (var w in _knownWindows)
		{
			if (currentWindowList.All(x => x.Window != w.Window))
			{
				_windowRemoved.OnNext(w);
			}
		}

		_knownWindows = currentWindowList;
	}

	public void Dispose()
	{
		XLib.XCloseDisplay(_rootWindowRef.Display);
	}

	private List<XWindowRef> GetNormalWindows()
	{
		var clientList = GetULongArray(_rootWindowRef, XAtoms.NetClientList);
		var results = new List<XWindowRef>();

		foreach (var c in clientList)
		{
			var childWindow = new XWindowRef() { Display = _rootWindowRef.Display, Window = c };
			var windowType = GetAtomArray(childWindow, XAtoms.NetWmWindowType);

			if (windowType.Any(s => s == "_NET_WM_WINDOW_TYPE_NORMAL"))
			{
				results.Add(childWindow);
			}
		}

		return results;
	}

	public string GetStringProperty(XWindowRef windowRef, ulong property)
	{
		var result = XLib.XGetWindowProperty(windowRef.Display, windowRef.Window, property, 0, 1024, false, 0, out var actualTypeReturn, out var actualFormatReturn, out var actualLength, out _, out var dataPointer);

		if (result != 0 || actualTypeReturn == 0)
		{
			return null;
		}

		var atomName = XLib.XGetAtomName(windowRef.Display, actualTypeReturn);

		if (atomName == Atoms.STRING.Name || atomName == Atoms.UTF8_STRING.Name || atomName == Atoms.COMPOUND_TEXT.Name)
		{
			var dataSize = (int)(actualLength * (ulong)actualFormatReturn / 8);
			var propBytes = new byte[dataSize];
			Marshal.Copy(dataPointer, propBytes, 0, dataSize);
			return Encoding.UTF8.GetString(propBytes);
		}

		return null;
	}

	public ulong[] GetULongArray(XWindowRef windowRef, ulong property)
	{
		var result = XLib.XGetWindowProperty(windowRef.Display, windowRef.Window, property, 0, 1024, false, 0, out var actualTypeReturn, out var actualFormatReturn, out var actualLength, out _, out var dataPointer);
		if (result != 0) return Array.Empty<ulong>();
		var dataSize = (int)(actualLength * sizeof(ulong));
		if (dataSize == 0) return Array.Empty<ulong>();

		var data = new byte[dataSize];
		Marshal.Copy(dataPointer, data, 0, dataSize);
		using var reader = new BinaryReader(new MemoryStream(data));
		var atomNames = new LinkedList<ulong>();

		for (var i = 0; i < (int) actualLength; i++)
		{
			atomNames.AddLast((ulong) reader.ReadInt64());
		}

		return atomNames.ToArray();
	}

	public string[] GetAtomArray(XWindowRef windowRef, ulong property)
	{
		var result = XLib.XGetWindowProperty(windowRef.Display, windowRef.Window, property, 0, 1024, false, 0, out var actualTypeReturn, out var actualFormatReturn, out var actualLength, out _, out var dataPointer);
		if (result != 0) return Array.Empty<string>();
		var dataSize = (int)(actualLength * (ulong)actualFormatReturn / 8);
		if (dataSize == 0) return Array.Empty<string>();

		var data = new byte[dataSize];
		Marshal.Copy(dataPointer, data, 0, dataSize);
		using var reader = new BinaryReader(new MemoryStream(data));
		var atomNames = new LinkedList<string>();

		for (var i = 0; i < (int) actualLength; i++)
		{
			var atom = (ulong)reader.ReadInt32();
			if (atom == 0) continue;
			atomNames.AddLast(XLib.XGetAtomName(windowRef.Display, atom));
		}

		return atomNames.ToArray();
	}

	public List<WindowIcon> GetIcons(XWindowRef windowRef)
	{
		var success = XLib.XGetWindowProperty(windowRef.Display, windowRef.Window, XAtoms.NetWmIcon, 0, 1024 * 1024 * 10, false, 0, out var actualTypeReturn, out var actualFormatReturn, out var actualLength, out var bytesLeft, out var dataPointer);

		if (success != 0 || actualTypeReturn == 0) return null;

		var data = new byte[actualLength * 8];
		Marshal.Copy(dataPointer, data, 0, data.Length);
		XLib.XFree(dataPointer);
		using var binaryReader = new BinaryReader(new MemoryStream(data));
		var icons = new List<WindowIcon>();

		while (binaryReader.PeekChar() != -1)
		{
			var width = binaryReader.ReadInt64();
			var height = binaryReader.ReadInt64();
			var numPixels = width * height;
			var imageData = new byte[numPixels * sizeof(int)];

			for (var i = 0; i < numPixels * sizeof(int); i += sizeof(int))
			{
				var intBytes = BitConverter.GetBytes(binaryReader.ReadInt32());
				binaryReader.ReadInt32();
				imageData[i] = intBytes[3];
				imageData[i+1] = intBytes[2];
				imageData[i+2] = intBytes[1];
				imageData[i+3] = intBytes[0];
			}

			icons.Add(new WindowIcon()
			{
				Width = (int) width,
				Height = (int) height,
				Data = ImageHelper.ConvertArgbToRgba(imageData, (int) width, (int) height)
			});
		}

		return icons;
	}

	private LinkedList<ulong> GetParents(XWindowRef windowRef)
	{
		var results = new LinkedList<ulong>();
		var current = windowRef.Window;

		while (current != 0)
		{
			results.AddLast(current);
			XLib.XQueryTree(windowRef.Display, current, out _, out var currentParent, out _, out _);
			current = currentParent;
		}

		return results;
	}

	public void ToggleWindowVisibility(XWindowRef windowRef)
	{
		XLib.XGetInputFocus(windowRef.Display, out var focusedWindow, out _);
		var focusedWindowRef = new XWindowRef() { Display = windowRef.Display, Window = focusedWindow };
		var windowHasFocus = GetParents(focusedWindowRef).Any(w => w == windowRef.Window);

		if (windowHasFocus)
		{
			XLib.XIconifyWindow(windowRef.Display, windowRef.Window, 0);
		}
		else
		{
			XLib.XMapWindow(windowRef.Display, windowRef.Window);
			XLib.XRaiseWindow(windowRef.Display, windowRef.Window);
		}

		XLib.XFlush(windowRef.Display);
	}

	public void MakeWindowVisible(XWindowRef windowRef)
	{
		XLib.XMapWindow(windowRef.Display, windowRef.Window);
		XLib.XRaiseWindow(windowRef.Display, windowRef.Window);
		XLib.XFlush(windowRef.Display);
	}
}
