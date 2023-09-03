using System.Xml.Linq;
using System.Xml.XPath;
using Glimpse.Services.DBus.Interfaces;
using Tmds.DBus.Protocol;

namespace Glimpse.Services.DBus.Introspection;

public class IntrospectionService
{
	private readonly Connection _connection;

	public IntrospectionService(Connections connections) => _connection = connections.Session;

	public async Task<DbusObjectDescription> FindDBusObjectDescription(string serviceName, string objectPath, Func<string, bool> match)
	{
		var introProxy = new OrgFreedesktopDBusIntrospectable(_connection, serviceName, objectPath);
		var rawXml = await introProxy.IntrospectAsync();
		var xml = XDocument.Parse(rawXml);
		var matchingInterfaces = xml.XPathSelectElements("//node/interface").Where(i => match(i.Attribute("name").Value)).ToList();

		if (matchingInterfaces.Any())
		{
			return new DbusObjectDescription
			{
				ServiceName = serviceName,
				ObjectPath = objectPath,
				Xml = rawXml,
				Interfaces = matchingInterfaces.Select(ParseDbusInterface).ToList()
			};
		}

		foreach (var n in xml.XPathSelectElements("//node/node"))
		{
			var nodeName = n.Attribute("name")?.Value;
			var childObjectPath = objectPath.Length == 1 ? "/" + nodeName : objectPath + "/" + nodeName;
			var result = await FindDBusObjectDescription(serviceName, childObjectPath, match);
			if (result != null) return result;
		}

		return null;
	}

	private static DbusInterface ParseDbusInterface(XElement x)
	{
		return new DbusInterface()
		{
			Name = x.Attribute("name").Value,
			Methods = x.XPathSelectElements("./method").Select(m => m.Attribute("name").Value).ToArray()
		};
	}
}
