# Scopes
Для меня одни из первых вопросов при анализе, использовании или поддержке  сервиса: 
1. Какие ресурсы он использует?
2. Как его корректно остановить? 

Под **сервисом** я имею в виду код, который работает продолжительное время, но ограниченный цикл жизни (дожнен уметь закрываться).
Под **остановить** я имею в виду откатить изменения разделенных внешних зависимостей, закрыть соединения, освободить ресурсы.
**Ресурс** - в широком смысле: event, другой сервис,  база данных, визуальный объект на экране, файл, объект в памяти, asset bundle (Unity), объект из пула. Словом то, что требует "подчистки" или сброса после окончания работы.

Как правило для корректной подчистки приходится сохранять ссылки на выделенные ресурсы. Распространенным подходом является - выносить их в поля, сохраняя их до вызова `Dispose`.  Это загрязняет код. 

Как поступать, когда нужно учесть несколько сценариев окончания работы сервиса? Например,  внештатные? 

Можно для всех ресурсов и сервиса реализовать интерфейс `IDisposable`. Но `IDisposable`  ни к чему не обязывает:
1. Можно забыть вызвать `Dispose`
2. Когда у сервиса несколько `IDisposable` полей, можно вызвать `Dispose`  не  в том порядке.
3. Если ресурсы не реализуют интерфейс `IDisposable`, это мешает унифицировать процесс, потеряем помощь статического анализатора

``` csharp
class MyService : IDisposable{
	// ... 
	AssetBundle bundle1;
	ButtonClieckedEvent buttonOnClick;
	
	public async Task Flow(AssetBundle bundle, Button button){
		bundle.Load();
		this.bundle1 = bundle;

		button.onClick.AddListener(OnClick);
		this.buttonOnClick = button.onClick;
	}
		
	public void Dispose(){
		this.bundle1.Unload();
		this.buttonOnClick.RemoveListener(OnClick);
	}
}
```
Здесь нам нужно запоминать ивент кнопки и бандл для того, чтобы подчистить. Но можно ввести обратное понятие от `IDisposable` - запросить аргументом `Scope` . И складывать в него все действия, которые мы хотели сделать в `Dispose`:




``` csharp
class MyService{
	public async Task Flow(AssetBundle bundle, Button button, Scope scope){
		bundle.Load();
		scope.Add(b => b.Unload(), bundle);

		button.onClick.AddListener(OnClick);
		scope.Add((e, action) => e.RemoveListener(action), (button.onClick, OnClick));
	}
}
```
`Scope` in the nutshell - is a stack of `IDisposable`'s. 

``` csharp
class Scope{
	// adds disposable to stack
	void Add(IDisposable)
	void Remove(IDisposable)
	void Dispose()
	IDisposable SubScope(out Scope)
}
```
