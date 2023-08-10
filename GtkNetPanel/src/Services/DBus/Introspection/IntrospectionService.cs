using System.Xml.Linq;
using System.Xml.XPath;
using Tmds.DBus;

namespace GtkNetPanel.Services.DBus.Introspection;

public class IntrospectionService
{
	private readonly Connection _connection;

	public IntrospectionService(Connection connection) => _connection = connection;

	public async Task<DbusObjectDescription> FindDBusObjectDescription(string serviceName, string objectPath, Func<string, bool> match)
	{
		var introProxy = _connection.CreateProxy<IIntrospectable>(serviceName, objectPath);
		var rawXml = await introProxy.IntrospectAsync();
		var xml = XDocument.Parse(rawXml);

		foreach (var i in xml.XPathSelectElements("//node/interface"))
		{
			if (!match(i.Attribute("name").Value))
			{
				continue;
			}

			return new DbusObjectDescription
			{
				ServiceName = serviceName,
				ObjectPath = objectPath,
				Xml = rawXml,
				Interfaces = xml
					.XPathSelectElements("//interface")
					.Select(x => new DbusInterface { Name = x.Attribute("name").Value, Methods = x.XPathSelectElements("./method").Select(m => m.Attribute("name").Value).ToArray() }).ToList()
			};
		}

		foreach (var n in xml.XPathSelectElements("//node/node"))
		{
			var result = await FindDBusObjectDescription(serviceName, objectPath.Length == 1 ? "/" + n.Attribute("name").Value : objectPath + "/" + n.Attribute("name").Value, match);
			if (result != null)
			{
				return result;
			}
		}

		return null;
	}
}
