using Glimpse.Common.Images;
using Glimpse.Redux;
using Glimpse.Redux.Selectors;
using static Glimpse.Redux.Selectors.SelectorFactory;

namespace Glimpse.Xorg.State;

public class XorgSelectors
{
	public static readonly ISelector<DataTable<ulong, IGlimpseImage>> Screenshots = CreateFeatureSelector<DataTable<ulong, IGlimpseImage>>();
	public static readonly ISelector<DataTable<ulong, WindowProperties>> Windows = CreateFeatureSelector<DataTable<ulong, WindowProperties>>();
}
