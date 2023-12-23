namespace Glimpse.Redux.Reducers;

public class ActionReducer<TState> where TState : class
{
	public Func<TState, object, TState> Reduce { get; set; }
	public string ActionType { get; set; }
}
