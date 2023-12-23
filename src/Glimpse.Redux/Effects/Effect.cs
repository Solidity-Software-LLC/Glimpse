namespace Glimpse.Redux.Effects;

public class Effect
{
	public Func<ReduxStore, IObservable<object>> Run { get; set; }
	public EffectConfig Config { get; set; }
}
