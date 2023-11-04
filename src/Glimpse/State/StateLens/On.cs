namespace Glimpse.State.StateLens;

public class On<TState> where TState : class
{
	public Func<TState, object, TState> Reduce { get; set; }
	public string[] Types { get; set; }
}
