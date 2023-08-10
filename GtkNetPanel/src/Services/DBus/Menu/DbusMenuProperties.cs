using Tmds.DBus;

namespace GtkNetPanel.Services.DBus.Menu;

[Dictionary]
public class DbusmenuProperties
{
	private uint _Version = default(uint);
	public uint Version
	{
		get
		{
			return _Version;
		}

		set
		{
			_Version = (value);
		}
	}

	private string _TextDirection = default(string);
	public string TextDirection
	{
		get
		{
			return _TextDirection;
		}

		set
		{
			_TextDirection = (value);
		}
	}

	private string _Status = default(string);
	public string Status
	{
		get
		{
			return _Status;
		}

		set
		{
			_Status = (value);
		}
	}

	private string[] _IconThemePath = default(string[]);
	public string[] IconThemePath
	{
		get
		{
			return _IconThemePath;
		}

		set
		{
			_IconThemePath = (value);
		}
	}
}
