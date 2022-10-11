using System;

public interface IRollback
{
	bool IsDisposed { get; }
	void Defer(Action          action);
	void RemoveDeferred(Action action);
}

/// <summary>
/// Container to defer actions to be executed on Dispose (resource cleanup, cancel side effects)
/// </summary>
public class Rollback : IRollback, IDisposable
{
	public           bool   IsDisposed { get; private set; }
	private readonly object lockObject;
	private          Action deferredActions;

	public Rollback()
	{
		lockObject = new object();
		IsDisposed = false;
	}

	/// <summary>
	/// Defer action to be executed upon Dispose
	/// </summary>
	/// <param name="action"></param>
	public void Defer(Action action)
	{
		lock (lockObject)
		{
			Asr.IsFalse(IsDisposed, "This rollback is disposed. Cannot defer action.");
			deferredActions = action + deferredActions; // first in last out order
		}
	}

	/// <summary>
	/// Remove deferred action so it won't be executed
	/// </summary>
	/// <param name="action"></param>
	public void RemoveDeferred(Action action)
	{
		lock (lockObject)
		{
			Asr.IsFalse(IsDisposed, "This rollback is disposed. Cannot remove deferred action.");
#if DEBUG
			var countPrev = deferredActions.GetInvocationList().Length;
			deferredActions -= action;
			if (deferredActions.GetInvocationList().Length == countPrev)
			{
				Asr.Fail("Trying to remove action which wasn't deferred");
			}
#else
		deferredActions -= action;
#endif
		}
	}

	/// <summary>
	/// Executes deferred actions in reverse order
	/// </summary>
	public void Dispose()
	{
		lock (lockObject)
		{
			Asr.IsFalse(IsDisposed, "Rollback is already disposed");
			IsDisposed = true;
			deferredActions?.Invoke();
			deferredActions = null;
		}
	}
}

public static class RollbackExtensions
{
	/// <summary>
	/// Opens a child rollback which will be disposed with current one. Allows cascade disposals
	/// </summary>
	/// <returns></returns>
	public static Rollback OpenRollback(this IRollback parentRollback)
	{
		Asr.IsFalse(parentRollback.IsDisposed, "This rollback is disposed. Cannot open rollback from it.");
		var result = new Rollback();
		parentRollback.Defer(DisposeChild);
		result.Defer(CancelDisposingChild);
		return result;

		void DisposeChild()
		{
			result.Dispose();
		}

		void CancelDisposingChild()
		{
			parentRollback.RemoveDeferred(DisposeChild);
		}
	}
}

public static class Asr
{
	[Conditional("DEBUG")]
	public static void Fail(string message)
	{
		UnityEngine.Assertions.Assert.IsTrue(false, message);
	}

	[Conditional("DEBUG")]
	public static void IsTrue(bool expression, string message = null)
	{
		if (expression)
			return;

		Fail(message);
	}

	[Conditional("DEBUG")]
	public static void IsFalse(bool expression, string message = null)
	{
		if (!expression)
			return;

		Fail(message);
	}
}