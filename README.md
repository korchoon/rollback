# Rollback - a better way to Dispose

The Rollback is new resource management, decoupling pattern, an alternative to Dispose. 
It's a container where you put actions that will be executed on Dispose.

The examples are intented for Unity developers. However, source code doesn't have any dependencies, so you could use it in vanilla C#.

## What is the problem?

When I analyze, use or support some service, one of the first questions arised are:

* What resources does it use?
* How to stop that feature correctly?

**Service** - code that runs continuous time (across multiple frames), but has limited lifetime. Limited means it needs to dispose.

**Dispose** - means roll back modifications of shared external dependencies, close connections, free up resources.

**Resource** - in a broad sense. It could be an event, another service, a database, visual object on a scene, file, unmanaged data.

Following pain points are related to resource management:
* Store or pass dependencies that you only need for dispose logic: events, connections
* Dispose actions need to be in a certain order
* Forget to dispose some of resources, which resoults ie in memory leaks
* Disposing resources that weren't allocated yet
* Degrade gracefully during exceptional situation

Often proper maintenance of all of the mentioned points is not that simple task. Moreover, when you get all things covered, you'll likely find your code "polluted".

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

Here we need to store `buttonOnClick` event and `assetBundle` to cleanup. But we could add `Rollback` as an argument and put disposal logic as we go along, instead of hard-coding it in `Dispose`:

``` csharp
class MyService {
  public async Task Flow(AssetBundle bundle, Button button, IRollback rollback) {
    bundle.Load();
    rollback.Add(b => b.Unload(), bundle);

    button.onClick.AddListener(OnClick);
    rollback.Add((e, action) => e.RemoveListener(action), (button.onClick, OnClick));
  }
}
```

## What is a `Rollback`?

`Rollback` is an <span id="a1">[inverse of IDisposable](#f1)</span>.

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

## Create a `Rollback`

``` csharp
var rollback = new Rollback();
```

`rollback` is a handle which is triggers all disposables collected in IRollback. The rollback executes the disposals in a stack-like order.

## Add dispose logic

``` csharp
void SomeMethod(IRollback rollback) {
    var instance = Object.Instantiate(prefab);
    rollback.Defer(g => Object.Destroy(g), instance); // 2
}
```

## Plays well with `using`

``` csharp
using (var childRollback = mainRollback.Open()) { 
    ShowStartPopup(childRollback);
    SpawnCharacter(childRollback);
    // ...
} // disposes the childRollback, but keeps mainRollback
```

## Plays well with `async`

``` csharp
async Task StartLevelAsync(){
  using (var childRollback = rollback.Open()) { 
      ShowStartPopup(childRollback);
      SpawnCharacter(childRollback);
      await StartLevelAsync(childRollback);
  } 
}
```

## Create child rollbacks

Sub-rollbacks are useful to be able to close some features separately. Yet not losing the upper rollback disposal. <>

``` csharp
using (var popupRollback = rollback.OpenRollback()){
    await ShowStartPopupAsync(popupRollback); // popup rollback will close 
} // will dispose popupRollback separately
```

## Use it like a cancellation token

Usage:

``` csharp
async Task FlowAsync(IRollback rollback){
    // await ...
    rollback.ThrowIfDisposed();
    // await ...
}
```


## Clarify lifetime in your API

Let's consider Photon API (network API)

## Force to give a rollback to an entity

Subscribe to event with a rollback

```csharp
void OnNext<T>(Action<T> listener, IRollback rollback);
```

## Cancellation token alternative

You could use rollback's disposal as a CancellationTokenSource.
Although it's not included, you could write your own extension method:

``` csharp
static void ThrowIfDisposed(this IRollback rollback){
    if (rollback.IsDisposed)
        throw new OperationCanceledException();
}
```

## FAQ
TODO
