namespace Glimpse.Redux.Effects;

public interface IEffectsFactory
{
	IEnumerable<Effect> Create();
}
