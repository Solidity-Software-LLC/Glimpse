
namespace Glimpse.Extensions.Redux.Selectors;

public static class SelectorFactory
{
	public static ISelector<TFinalResult> CreateFeatureSelector<TFinalResult>() where TFinalResult : class =>
		new SimpleSelector<TFinalResult>(s => s.GetFeatureState<TFinalResult>());

	public static ISelector<TFinalResult> CreateSelector<TFinalResult>(
		Func<StoreState, TFinalResult> projectorFunction,
		IEqualityComparer<TFinalResult> equalityComparer = null) =>
			new SimpleSelector<TFinalResult>(projectorFunction, equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,TResult>(
		ISelector<S1> s1,
		Func<Cache<TResult>,S1,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,TResult>(s1,proj,equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,TResult>(
		ISelector<S1> s1,
		Func<Cache<TResult>,S1,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,TResult>(s1,proj,new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,TResult>(
		ISelector<S1> s1,
		Func<S1,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,TResult>(s1,(_,x1) => proj(x1),equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,TResult>(
		ISelector<S1> s1,
		Func<S1,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,TResult>(s1,(_,x1) => proj(x1),new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,
		Func<Cache<TResult>,S1,S2,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,TResult>(s1,s2,proj,equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,
		Func<Cache<TResult>,S1,S2,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,TResult>(s1,s2,proj,new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,
		Func<S1,S2,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,TResult>(s1,s2,(_,x1,x2) => proj(x1,x2),equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,
		Func<S1,S2,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,TResult>(s1,s2,(_,x1,x2) => proj(x1,x2),new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,
		Func<Cache<TResult>,S1,S2,S3,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,TResult>(s1,s2,s3,proj,equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,
		Func<Cache<TResult>,S1,S2,S3,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,TResult>(s1,s2,s3,proj,new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,
		Func<S1,S2,S3,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,TResult>(s1,s2,s3,(_,x1,x2,x3) => proj(x1,x2,x3),equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,
		Func<S1,S2,S3,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,TResult>(s1,s2,s3,(_,x1,x2,x3) => proj(x1,x2,x3),new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,
		Func<Cache<TResult>,S1,S2,S3,S4,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,TResult>(s1,s2,s3,s4,proj,equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,
		Func<Cache<TResult>,S1,S2,S3,S4,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,TResult>(s1,s2,s3,s4,proj,new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,
		Func<S1,S2,S3,S4,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,TResult>(s1,s2,s3,s4,(_,x1,x2,x3,x4) => proj(x1,x2,x3,x4),equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,
		Func<S1,S2,S3,S4,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,TResult>(s1,s2,s3,s4,(_,x1,x2,x3,x4) => proj(x1,x2,x3,x4),new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,
		Func<Cache<TResult>,S1,S2,S3,S4,S5,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,TResult>(s1,s2,s3,s4,s5,proj,equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,
		Func<Cache<TResult>,S1,S2,S3,S4,S5,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,TResult>(s1,s2,s3,s4,s5,proj,new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,
		Func<S1,S2,S3,S4,S5,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,TResult>(s1,s2,s3,s4,s5,(_,x1,x2,x3,x4,x5) => proj(x1,x2,x3,x4,x5),equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,
		Func<S1,S2,S3,S4,S5,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,TResult>(s1,s2,s3,s4,s5,(_,x1,x2,x3,x4,x5) => proj(x1,x2,x3,x4,x5),new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,
		Func<Cache<TResult>,S1,S2,S3,S4,S5,S6,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,TResult>(s1,s2,s3,s4,s5,s6,proj,equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,
		Func<Cache<TResult>,S1,S2,S3,S4,S5,S6,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,TResult>(s1,s2,s3,s4,s5,s6,proj,new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,
		Func<S1,S2,S3,S4,S5,S6,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,TResult>(s1,s2,s3,s4,s5,s6,(_,x1,x2,x3,x4,x5,x6) => proj(x1,x2,x3,x4,x5,x6),equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,
		Func<S1,S2,S3,S4,S5,S6,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,TResult>(s1,s2,s3,s4,s5,s6,(_,x1,x2,x3,x4,x5,x6) => proj(x1,x2,x3,x4,x5,x6),new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,ISelector<S7> s7,
		Func<Cache<TResult>,S1,S2,S3,S4,S5,S6,S7,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(s1,s2,s3,s4,s5,s6,s7,proj,equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,ISelector<S7> s7,
		Func<Cache<TResult>,S1,S2,S3,S4,S5,S6,S7,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(s1,s2,s3,s4,s5,s6,s7,proj,new FuncEqualityComparer<TResult>(equalityComparer));

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,ISelector<S7> s7,
		Func<S1,S2,S3,S4,S5,S6,S7,TResult> proj,
		IEqualityComparer<TResult> equalityComparer = null) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(s1,s2,s3,s4,s5,s6,s7,(_,x1,x2,x3,x4,x5,x6,x7) => proj(x1,x2,x3,x4,x5,x6,x7),equalityComparer);

	public static ISelector<TResult> CreateSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(
		ISelector<S1> s1,ISelector<S2> s2,ISelector<S3> s3,ISelector<S4> s4,ISelector<S5> s5,ISelector<S6> s6,ISelector<S7> s7,
		Func<S1,S2,S3,S4,S5,S6,S7,TResult> proj,
		Func<TResult,TResult,bool> equalityComparer) =>
			 new MemoizedSelector<S1,S2,S3,S4,S5,S6,S7,TResult>(s1,s2,s3,s4,s5,s6,s7,(_,x1,x2,x3,x4,x5,x6,x7) => proj(x1,x2,x3,x4,x5,x6,x7),new FuncEqualityComparer<TResult>(equalityComparer));
}

