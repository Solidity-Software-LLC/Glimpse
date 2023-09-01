﻿using Tmds.DBus.Protocol;

namespace GtkNetPanel.Services.DBus.Core;

public static class ReaderExtensions
{
	public static string ReadMessage_s(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadString();
	}

	public static int ReadMessage_i(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadInt32();
	}

	public static ObjectPath ReadMessage_o(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		reader.ReadUInt32();
		return reader.ReadObjectPath();
	}

	public static bool ReadMessage_b(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadBool();
	}

	public static byte[] ReadArray_ay(this ref Reader reader)
	{
		List<byte> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.Byte);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadByte());
		}

		return items.ToArray();
	}

	public static (int, int, byte[]) ReadStruct_riiayz(this ref Reader reader)
	{
		reader.AlignStruct();
		return ValueTuple.Create(reader.ReadInt32(), reader.ReadInt32(), reader.ReadArray_ay());
	}

	public static (int, int, byte[])[] ReadArray_ariiayz(this ref Reader reader)
	{
		List<(int, int, byte[])> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.Struct);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadStruct_riiayz());
		}

		return items.ToArray();
	}

	public static (int, int, byte[])[] ReadMessage_ariiayz(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadArray_ariiayz();
	}

	public static (string, (int, int, byte[])[], string, string) ReadStruct_rsariiayzssz(this ref Reader reader)
	{
		reader.AlignStruct();
		return ValueTuple.Create(reader.ReadString(), reader.ReadArray_ariiayz(), reader.ReadString(), reader.ReadString());
	}

	public static (string, (int, int, byte[])[], string, string) ReadMessage_rsariiayzssz(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadStruct_rsariiayzssz();
	}

	public static string[] ReadArray_as(this ref Reader reader)
	{
		List<string> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.String);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadString());
		}

		return items.ToArray();
	}

	public static Dictionary<string, DBusVariantItem> ReadDictionary_aesv(this ref Reader reader)
	{
		Dictionary<string, DBusVariantItem> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.Struct);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadString(), reader.ReadDBusVariant());
		}

		return items;
	}

	public static DBusVariantItem[] ReadArray_av(this ref Reader reader)
	{
		List<DBusVariantItem> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.Variant);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadDBusVariant());
		}

		return items.ToArray();
	}

	public static (int, Dictionary<string, DBusVariantItem>, DBusVariantItem[]) ReadStruct_riaesvavz(this ref Reader reader)
	{
		reader.AlignStruct();
		return ValueTuple.Create(reader.ReadInt32(), reader.ReadDictionary_aesv(), reader.ReadArray_av());
	}

	public static (uint revision, (int, Dictionary<string, DBusVariantItem>, DBusVariantItem[]) layout) ReadMessage_uriaesvavz(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		var arg0 = reader.ReadUInt32();
		var arg1 = reader.ReadStruct_riaesvavz();
		return (arg0, arg1);
	}

	public static (int, Dictionary<string, DBusVariantItem>) ReadStruct_riaesvz(this ref Reader reader)
	{
		reader.AlignStruct();
		return ValueTuple.Create(reader.ReadInt32(), reader.ReadDictionary_aesv());
	}

	public static (int, Dictionary<string, DBusVariantItem>)[] ReadArray_ariaesvz(this ref Reader reader)
	{
		List<(int, Dictionary<string, DBusVariantItem>)> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.Struct);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadStruct_riaesvz());
		}

		return items.ToArray();
	}

	public static (int, Dictionary<string, DBusVariantItem>)[] ReadMessage_ariaesvz(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadArray_ariaesvz();
	}

	public static DBusVariantItem ReadMessage_v(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadDBusVariant();
	}

	public static int[] ReadArray_ai(this ref Reader reader)
	{
		List<int> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.Int32);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadInt32());
		}

		return items.ToArray();
	}

	public static int[] ReadMessage_ai(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadArray_ai();
	}

	public static (int[] updatesNeeded, int[] idErrors) ReadMessage_aiai(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		var arg0 = reader.ReadArray_ai();
		var arg1 = reader.ReadArray_ai();
		return (arg0, arg1);
	}

	public static (int, string[]) ReadStruct_riasz(this ref Reader reader)
	{
		reader.AlignStruct();
		return ValueTuple.Create(reader.ReadInt32(), reader.ReadArray_as());
	}

	public static (int, string[])[] ReadArray_ariasz(this ref Reader reader)
	{
		List<(int, string[])> items = new();
		var headersEnd = reader.ReadArrayStart(DBusType.Struct);
		while (reader.HasNext(headersEnd))
		{
			items.Add(reader.ReadStruct_riasz());
		}

		return items.ToArray();
	}

	public static ((int, Dictionary<string, DBusVariantItem>)[] updatedProps, (int, string[])[] removedProps) ReadMessage_ariaesvzariasz(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		var arg0 = reader.ReadArray_ariaesvz();
		var arg1 = reader.ReadArray_ariasz();
		return (arg0, arg1);
	}

	public static (uint revision, int parent) ReadMessage_ui(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		var arg0 = reader.ReadUInt32();
		var arg1 = reader.ReadInt32();
		return (arg0, arg1);
	}

	public static (int id, uint timestamp) ReadMessage_iu(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		var arg0 = reader.ReadInt32();
		var arg1 = reader.ReadUInt32();
		return (arg0, arg1);
	}

	public static uint ReadMessage_u(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadUInt32();
	}

	public static string[] ReadMessage_as(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadArray_as();
	}

	public static byte[] ReadMessage_ay(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadArray_ay();
	}

	public static Dictionary<string, DBusVariantItem> ReadMessage_aesv(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		return reader.ReadDictionary_aesv();
	}

	public static (string Item1, string Item2, string Item3) ReadMessage_sss(Message message, object? _)
	{
		var reader = message.GetBodyReader();
		var arg0 = reader.ReadString();
		var arg1 = reader.ReadString();
		var arg2 = reader.ReadString();
		return (arg0, arg1, arg2);
	}
}
