# Rollback pattern

## What is it

It’s like “Dynamic Dispose”

1. Defers `Action` to be executed during [Dispose](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose)
2. Executes them on Dispose
3. Allows cascade calls

## How to use it

1. create “rollback point”, pass it down the execution flow, dispose when you want to clean up 
    
    ```csharp
    var rollback = new Rollback();
    runner.StartCoroutine(ShowPopup(rollback));
    //...
    rollback.Dispose();
    ```
    
2. pass down to dependent features, defer “dispose actions” in place when you make something that has side effects 

```csharp
IEnumerator ShowPopup(Rollback popupRollback) {
	window.SetActive(true);
	popupRollback.Defer(() => window.SetActive(false));

	someEvent += Callback;
	popupRollback.Defer(() => someEvent -= Callback);

	yield return WaitOkButton();

	var fxInstance = Instantiate(fxPrefab);
	popupRollback.Defer(() ⇒ Destroy(fxInstance));

	yield return new WaitForSeconds(fxInstance.Delay);
}
```

1. Main differences to Dispose
    1. don’t need to store dependencies you need only in Dispose(or similar) 
    2. you don’t need to guess 
        1. what needs to be cleaned up, and what doesn’t (in case of some interruption, or exception)
        2. in which order
    3. you actually don’t need Dispose method to clean-up