using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using GtkNetPanel.Interop.X11;
using GtkNetPanel.Services.GtkSharp;
using GtkNetPanel.State;
using Microsoft.Extensions.Hosting;
using Task = System.Threading.Tasks.Task;

namespace GtkNetPanel.Services.X11;

public class XLibAdaptorService : IDisposable
{
	private XWindowRef _rootWindowRef;
	private readonly IHostApplicationLifetime _applicationLifetime;
	private readonly Subject<XWindowRef> _windowCreated = new();
	private readonly Subject<XWindowRef> _windowRemoved = new();
	private readonly Subject<XWindowRef> _focusChanged = new();
	private List<XWindowRef> _knownWindows = new();

	public XLibAdaptorService(IHostApplicationLifetime applicationLifetime)
	{
		_applicationLifetime = applicationLifetime;
	}

	public IObservable<XWindowRef> WindowCreated => _windowCreated;
	public IObservable<XWindowRef> WindowRemoved => _windowRemoved;
	public IObservable<XWindowRef> FocusChanged => _focusChanged;

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

				if (e.atom == XAtoms.NetClientList)
				{
					HandleWindowListUpdate();
				}
				else if (e.atom == XAtoms.NetActiveWindow)
				{
					var activeWindow = GetULongArray(_rootWindowRef, XAtoms.NetActiveWindow)[0];
					if (activeWindow == 0) continue;
					_focusChanged.OnNext(new XWindowRef() { Display = _rootWindowRef.Display, Window = activeWindow });
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

	public XClassHint GetClassHint(XWindowRef windowRef)
	{
		XLib.XGetClassHint(windowRef.Display, windowRef.Window, out var classHint);
		return classHint;
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

	public List<BitmapImage> GetIcons(XWindowRef windowRef)
	{
		var success = XLib.XGetWindowProperty(windowRef.Display, windowRef.Window, XAtoms.NetWmIcon, 0, 1024 * 1024 * 10, false, 0, out var actualTypeReturn, out var actualFormatReturn, out var actualLength, out var bytesLeft, out var dataPointer);

		if (success != 0 || actualTypeReturn == 0) return null;

		var data = new byte[actualLength * 8];
		Marshal.Copy(dataPointer, data, 0, data.Length);
		XLib.XFree(dataPointer);
		using var binaryReader = new BinaryReader(new MemoryStream(data));
		var icons = new List<BitmapImage>();

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

			icons.Add(new BitmapImage()
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

	// Can I use XSendEvent with propagate set to true instead of searching parents?
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

	private void ModifyNetWmState(XWindowRef windowRef, ulong[] properties, bool add)
	{
		var message = new XClientMessageEvent();
		message.window = windowRef.Window;
		message.display = windowRef.Display;
		message.ptr1 = (IntPtr) (add ? 1 : 0);
		message.format = 32;
		message.message_type = XAtoms.NetWmState;
		message.type = (int) Event.ClientMessage;
		message.send_event = 1;

		if (properties.Length >= 1) message.ptr2 = (IntPtr) properties[0];
		if (properties.Length >= 2) message.ptr3 = (IntPtr) properties[1];
		if (properties.Length >= 3) message.ptr4 = (IntPtr) properties[2];
		if (properties.Length >= 4) message.ptr5 = (IntPtr) properties[3];

		var pointer = Marshal.AllocHGlobal(24 * sizeof(long));
		Marshal.StructureToPtr(message, pointer, true);
		XLib.XSendEvent(windowRef.Display, windowRef.Window, true, (long)EventMask.SubstructureNotifyMask, pointer);
		XLib.XFlush(windowRef.Display);
		Marshal.FreeHGlobal(pointer);
	}

	public void MaximizeWindow(XWindowRef windowRef)
	{
		var state = GetULongArray(windowRef, XAtoms.NetWmState);
		var alreadyMaximized = state.Any(a => a == XAtoms.NetWmStateMaximizedHorz || a == XAtoms.NetWmStateMaximizedVert);
		ModifyNetWmState(windowRef, new[] { XAtoms.NetWmStateMaximizedHorz, XAtoms.NetWmStateMaximizedVert}, !alreadyMaximized);
	}

	public void MinimizeWindow(XWindowRef windowRef)
	{
		var state = GetULongArray(windowRef, XAtoms.NetWmState);
		var alreadyIconified = state.Any(a => a == XAtoms.NetWmStateHidden);

		if (alreadyIconified)
		{
			XLib.XMapWindow(windowRef.Display, windowRef.Window);
			XLib.XRaiseWindow(windowRef.Display, windowRef.Window);
		}
		else
		{
			XLib.XIconifyWindow(windowRef.Display, windowRef.Window, 0);
		}
	}

	public void MakeWindowVisible(XWindowRef windowRef)
	{
		XLib.XMapWindow(windowRef.Display, windowRef.Window);
		XLib.XRaiseWindow(windowRef.Display, windowRef.Window);
		XLib.XFlush(windowRef.Display);
	}

	public BitmapImage CaptureWindowScreenshot(XWindowRef windowRef)
	{
		XLib.XGetWindowAttributes(windowRef.Display, windowRef.Window, out var windowAttributes);
		var imagePointer = XLib.XGetImage(windowRef.Display, windowRef.Window, 0, 0, windowAttributes.width, windowAttributes.height, XConstants.AllPlanes, XConstants.ZPixmap);

		if (imagePointer == IntPtr.Zero)
		{
			var icons = GetIcons(windowRef);
			var biggestIcon = icons.MaxBy(i => i.Width);
			return biggestIcon;
		}

		var image = Marshal.PtrToStructure<XImage>(imagePointer);
		var imageData = new byte[image.bytes_per_line * image.height];
		Marshal.Copy(image.data, imageData, 0, imageData.Length);

		var bitmap = new BitmapImage() { Data = imageData, Height = image.height, Width = image.width };
		XLib.XDestroyImage(imagePointer);
		return bitmap;
	}

	public void CloseWindow(XWindowRef windowRef)
	{
		var message = new XClientMessageEvent();
		message.display = windowRef.Display;
		message.window = windowRef.Window;
		message.format = 32;
		message.type = (int) Event.ClientMessage;
		message.message_type = XAtoms.NetCloseWindow;
		message.send_event = 1;

		var pointer = Marshal.AllocHGlobal(24 * sizeof(long));
		Marshal.StructureToPtr(message, pointer, true);
		XLib.XSendEvent(windowRef.Display, windowRef.Window, true, (long)EventMask.SubstructureNotifyMask, pointer);
		XLib.XFlush(windowRef.Display);
		Marshal.FreeHGlobal(pointer);
	}


	public void StartResizing(XWindowRef windowRef)
	{
		XLib.XGetWindowAttributes(windowRef.Display, windowRef.Window, out var windowAttributes);
		XLib.XTranslateCoordinates(_rootWindowRef.Display, windowRef.Window, _rootWindowRef.Window, 0, 0, out var x, out var y, out _);
		XLib.XWarpPointer(windowRef.Display, 0, windowRef.Window, 0, 0, 0, 0, (int) windowAttributes.width, (int) windowAttributes.height);
		var args = new ulong[] { (ulong)(x + windowAttributes.width / 2), (ulong)(y + windowAttributes.height / 2), 4, 1, 1 };
		SendClientMessage(windowRef, XAtoms.NetWmMoveresize, args);
	}

	public void StartMoving(XWindowRef windowRef)
	{
		XLib.XGetWindowAttributes(windowRef.Display, windowRef.Window, out var windowAttributes);
		XLib.XTranslateCoordinates(_rootWindowRef.Display, windowRef.Window, _rootWindowRef.Window, 0, 0, out var x, out var y, out _);
		XLib.XWarpPointer(windowRef.Display, 0, windowRef.Window, 0, 0, 0, 0, (int) windowAttributes.width / 2, (int) windowAttributes.height / 2);
		var args = new ulong[] { (ulong)(x + windowAttributes.width / 2), (ulong)(y + windowAttributes.height / 2), 8, 1, 1 };
		SendClientMessage(windowRef, XAtoms.NetWmMoveresize, args);
	}

	private void SendClientMessage(XWindowRef windowRef, ulong messageType, ulong[] data)
	{
		var message = new XClientMessageEvent();
		message.display = windowRef.Display;
		message.window = windowRef.Window;
		message.format = 32;
		message.type = (int) Event.ClientMessage;
		message.message_type = XAtoms.NetWmMoveresize;
		message.send_event = 1;

		if (data.Length >= 1) message.ptr1 = (IntPtr) data[0];
		if (data.Length >= 2) message.ptr2 = (IntPtr) data[1];
		if (data.Length >= 3) message.ptr3 = (IntPtr) data[2];
		if (data.Length >= 4) message.ptr4 = (IntPtr) data[3];
		if (data.Length >= 5) message.ptr5 = (IntPtr) data[4];

		var pointer = Marshal.AllocHGlobal(24 * sizeof(long));
		Marshal.StructureToPtr(message, pointer, true);
		XLib.XSendEvent(windowRef.Display, windowRef.Window, true, (long)EventMask.SubstructureNotifyMask, pointer);
		XLib.XFlush(windowRef.Display);
		Marshal.FreeHGlobal(pointer);
	}
}
