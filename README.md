# Scopes - a better way to Dispose

Scope is a new resource management, decoupling pattern, an alternative to Dispose. It provides a `Scope` dependency where you add the dispose logic dynamically, which then will be invoked in stack-like order by a caller.

The examples are intented for Unity developers. However, source code doesn't have any dependencies, so you could use it in vanilla C#.

## What is the problem?

When I analyze, use or support some service, one of the first questions arised are:

* What resources does it use?
* How to stop that feature correctly?

**Service** - code that runs continuous time (across multiple frames), but has limited lifetime. Limited means it needs to dispose.

**Dispose** - means roll back modifications of shared external dependencies, close connections, free up resources.

**Resource** - in a broad sense. It could be an event, another service, a database, visual object on a scene, file, unmanaged data.

Usually there're several pain-points in dispose:

* Store or pass `Dispose` dependencies: events, connections
* Make disposals in certain order
* Forget to dispose (common inspection in static analysis tools)
* To not dispose resources that weren't allocated yet
* Gracefully dispose in exceptional situation
* Not all dependencies implement `IDisposable` (static analysis won't help in that case)

Often proper maintenance of all of the mentioned points is not that simple task. Moreover, when you get all things covered, you'll probably find your code "polluted".

Hard-coded `Dispose` method:

TODO provide a better code sample

``` csharp
class MyService : IDisposable {
  // ... 
  AssetBundle assetBundle;
  ButtonClieckedEvent buttonOnClick;

  public async Task Flow(AssetBundle bundle, Button button) {
    bundle.Load();
    this.assetBundle = bundle;

    // await ...

    button.onClick.AddListener(OnClick);
    this.buttonOnClick = button.onClick;
  }

  public void Dispose(){
    this.assetBundle.Unload();
    this.buttonOnClick.RemoveListener(OnClick);
  }
}
```

Here we need to store `buttonOnClick` event and `assetBundle` to cleanup. But we could add `Scope` as an argument and put disposal logic as we go along, instead of hard-coding it in `Dispose`:

``` csharp
class MyService {
  public async Task Flow(AssetBundle bundle, Button button, Scope scope) {
    bundle.Load();
    scope.Add(b => b.Unload(), bundle);

    button.onClick.AddListener(OnClick);
    scope.Add((e, action) => e.RemoveListener(action), (button.onClick, OnClick));
  }
}
```

## What is a `Scope`?

`Scope` is an <span id="a1">[inverse of IDisposable](#f1)</span>.

Or you could think of it as an `IObservable<IDisposable>` that you could subscribe to.

Internally it's a stack of `IDisposable`. A dependency that you could pass to collect disposal logic that should invoke at certain moment by the user logic

## Benefits

* Cascading behaviour out of the box
* Gives undo feature as a side effect (ie, make the level restartable without reloading the scene)
* Track down resources or shared dependencies easier
* Cut down the dependencies which were leaved for `Dispose`
* Easier to maintain the code:
  * You know how to shut down the feature. It helps you to divide the "suspects" and find the source of issues
  * Logic is more future-proof. ~~If~~ When you change the way or moment when it needs to be disposed

### Let's dive into code

## Create a `Scope`

``` csharp
var scope = new Scope(out IDisposable disposeScope);
```

`disposeScope` is a handle which is triggers all disposables collected in Scope. The scope executes the disposals in a stack-like order.

## Add dispose logic

``` csharp
void SomeMethod(Scope scope) {
    var instance = Object.Instantiate(prefab);

    // add as IDisposable
    IDisposable disposeInstance = new Disposable<GameObject>(g => Object.Destroy(g), instance);
    scope.Add(disposeInstance);

    // or use the helper extension
    scope.Add(g => Object.Destroy(g), instance); // 2
}
```

## Plays well with `using`

Factory method made to return `IDisposable`

``` csharp
static IDisposable New(out Scope scope)
```

`IDisposable` then could be used in `using` statement, and omited:

``` csharp
using (Scope.New(out Scope mainScope)) { // New() returns IDisposable
    ShowStartPopup(scope);
    SpawnCharacter(scope);
    // ...
} // disposes the mainScope
```

## Plays well with `async`

``` csharp
async Task StartLevelAsync(){
  using (Scope.New(out Scope mainScope)) { 
      ShowStartPopup(scope);
      SpawnCharacter(scope);
      await StartLevelAsync(scope);
  } 
}
```

## Create sub-scopes

Sub-scopes are useful to be able to close some features separately. Yet not losing the upper scope disposal. <>

``` csharp
using (scope.SubScope(out Scope popupScope)){
    await ShowStartPopupAsync(popupScope); // popup scope will close 
} // will dispose popupScope separately
```

## Use it like a cancellation token

Usage:

``` csharp
async Task FlowAsync(Scope scope){
    // await ...
    scope.ThrowIfDisposed();
}
```


## Clarify lifetime in your API

Let's consider Photon API (network API)

```csharp
 TODO: flat singleton room-related methods 
```

```csharp
 TODO: could become Scoped room api
```

## Force to give a scope to an entity

Subscribe to event with a scope

```csharp
void OnNext<T>(Action<T> listener, IScope scope);
```

## Cancellation token alternative

You could use scope's disposal as a CancellationTokenSource.
Although it's not included, you could write your own extension method:

``` csharp
static void ThrowIfDisposed(this Scope scope){
    if (scope.IsDisposed)
        throw new OperationCanceledException();
}
```

## FAQ

* ### Why `Scope` doesn't implement `IDisposable`?

  To split responsibilities of subscribing and invoking. Ie, in Reactive Extensions mixing `IObservable` and `IObserver` into a single class (`Subject`) considered as a bad practice.

* ### Why `Scope.Add<T>(Action<T>, T)`, not just `Scope.Add(Action)`?

  While the latter is easier to call, it will force you to make excessive closures.
  You could use former to define explicitly the object(s) you need. If you need multiple,you could use ValueTuples.

* ### How to unsubscribe in-between from Scope

  Keep `IDisposable` which you passed to `scope.Add` and pass it to `scope.Remove`. Although it's not a common situation, but sometimes needed, ie the sub-scope uses it to remove itself if it was disposed before the parent scope

* ### Why disposals should execute in reverse (stack-like) order?

  Since it's common way to model a hierarchy

## <b id="f1"> Inverse of `IDisposable` </b>  [↩ back](#a1)

``` csharp
class MyFeature : IDisposable{
  GameObject instance;

  void Init(GameObject prefab){
    instance = Instantiate(prefab);
  }

  void Dispose(){
    Destroy(this.instance);
  }
}
```

Could be written as:

``` csharp
class MyFeature {
  void Init(GameObject prefab, IScope scope){
    instance = Instantiate(prefab);
    scope.Add(instance.ToDisposable(g => Destroy(g)));
  }
}
```

## Further topics

* ### Sample: make your level restartable without reloading the scene

* ### Scopes for better public API. Explicit OnClose event

* ### Rich debugging tools. Active scopes hierarchy, associated resources via [Calller*] attributes
