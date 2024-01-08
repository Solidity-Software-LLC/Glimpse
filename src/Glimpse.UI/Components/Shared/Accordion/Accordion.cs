using Gtk;

namespace Glimpse.UI.Components.Shared.Accordion;

public class Accordion : Bin
{
	private readonly List<AccordionSection> _sections = new();
	private readonly Box _sectionsContainer;

	public Accordion()
	{
		_sectionsContainer = new Box(Orientation.Vertical, 0);

		var accordion = new ScrolledWindow()
			.Prop(w => w.HscrollbarPolicy = PolicyType.Never)
			.Prop(w => w.Expand = true)
			.AddMany(_sectionsContainer);

		Add(accordion);
	}

	public void AddSection(string sectionName, Widget sectionHeader)
	{
		var sectionItemsContainer = new Box(Orientation.Vertical, 8);
		sectionItemsContainer.Visible = false;

		var sectionContainer = new Box(Orientation.Vertical, 8)
			.Prop(w => w.Halign = Align.Fill)
			.AddMany(new EventBox()
				.AddButtonStates()
				.AddClass("button")
				.AddMany(sectionHeader)
				.Prop(w => w.ObserveButtonRelease().Subscribe(_ =>
				{
					foreach (var s in _sections.Where(s => s.Name != sectionName)) s.ItemContainer.Visible = false;
					sectionItemsContainer.Visible = !sectionItemsContainer.Visible;
				})))
			.AddMany(sectionItemsContainer);

		_sections.Add(new AccordionSection() { Name = sectionName, Root = sectionContainer, ItemContainer = sectionItemsContainer });
		_sectionsContainer.Add(sectionContainer);
	}

	public void AddItemToSection(string sectionName, Widget item)
	{
		var section = _sections.First(s => s.Name == sectionName);
		section.ItemContainer.Add(item);
	}
}
