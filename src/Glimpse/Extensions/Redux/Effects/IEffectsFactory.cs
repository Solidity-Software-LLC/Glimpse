namespace Glimpse.Extensions.Redux.Effects;

public interface IEffectsFactory
{
	IEnumerable<Effect> Create();
}
