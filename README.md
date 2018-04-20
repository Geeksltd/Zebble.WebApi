[logo]: https://raw.githubusercontent.com/Geeksltd/Zebble.WebApi/master/Shared/NuGet/Icon.png "Zebble.WebApi"


## Zebble.WebApi

![logo]

A Zebble plugin that allow you to access to the server API.


[![NuGet](https://img.shields.io/nuget/v/Zebble.WebApi.svg?label=NuGet)](https://www.nuget.org/packages/Zebble.WebApi/)

> Most applications need to connect to a server to download or upload data. You can use the HttpClient class in your Zebble app to access the Web APIs with any server-side API architecture that you want to implement.
However, Zebble also comes with a layer on top of the raw HTTP tools of .NET to simplify things 

<br>


### Setup
* Available on NuGet: [https://www.nuget.org/packages/Zebble.WebApi/](https://www.nuget.org/packages/Zebble.WebApi/)
* Install in your platform client projects.
* Available for iOS, Android and UWP.
<br>


### Api Usage

#### CONNECTING ZEBBLE TO WEB API
In the .Net world, your application will usually be an ASP.NET MVC application hosted in the internet, perhaps in SQL Server database to hosted data. In the ASP.NET application you will have all the usually suspect like business logic class, services, data access layer and etc.

But you will also have ASP.NET Web API controllers to enable the App to connect to server to send and receive data.
In addition to that, many applications will also have a UI as part of the same application to provide Admin and content editing features for the client App owner.

The UI components can simply be MVC Controllers and be used utilize in the same database business logic and service classes that they are used by API controllers.

Of course in the same MVC application, they can be web pages for the end users interact with the platform through the browser too.
Obviously many serious digital businesses come with a web base version and in addition to the native app. So that are more accessible and usable by their market.

This is how you can use the WEB API from Zebble applications.


Here we have a web api with the URL: https://My-domain.com/api/products which will return a **JSON** of array of products.
Look at the server side api documentation later.
For now, we want to focus in the client-side coding in the mobile app.  



In the UI layer app, we have a page that show the list of products. Data source of that list module on that page will be called Domain name class product which has a static method name getList().
To implement the **GetList()** method, we will use the class in our project named Api: BaseApi which inherits from the base class in Zebble. The base Api class is effectively like simple an agent to send http request to the server with some useful features such as  exception handeling, cashing, offline supporting and etc.
Here is the implemention of **GetList()** method:
```csharp
public static Task<Product[]> getList()
{
      return Api.Get<Product[]>("api/products");
}
```
In this code, the domain name is missed. It is determined in `Config.XML` file:
```xml
<Api.Base.Url value="https://My-domain.com" />
```
You can use the URL in the parameter of `Api.GET()` method.

##### Caching the result
Get-based Web Apis are used to read data. When you invoke them for the first time, the situation is obvious in terms of error handling. But after a first successful call, if later on your app is calling the same URL again, the situation is different. In this example, they say that you have received a product's data from the Api once. If a few minutes later, the user goes back to product page, but this time there is no internet connection and so you can not get a fresh the list of products from the Api.

Instead of showing nothing or an error message perhaps, surely would be nicer to use the data that you previously received to give that functioning. So Cashing in this case would be great. Of course, in some other cases, this could call serious problems if the data you showing the user is not fresh. For example, it can confused the user or even call security or legally issues.

#### CALLING A GET API (IN THE MOBILE APP)

You can call `Api.Get<T>()` to invoke a GET based API. The parameters can either be embedded in the url (route) or added as query string. 
```csharp
await Api.Get<Order[]>("api/v1/orders");

// Or with query string parameter:
await Api.Get<Order[]>("api/v1/orders?from=" + fromDate);

// You can also use an anonymous object for sending query string parameters:
await Api.Get<Order[]>("api/v1/orders", new { from = fromDate, to = toDate, ... });
```
##### Caching Get call results
Whenever the server returns a valid response (which is often Json) it will be saved in the application as a local file under **Resources\-ApiCache\[URL-HASH].txt** which can then be used in the future when the same URL (i.e. the same API with the same parameters) is called when the network or server is unavailable or there is any error. This allows the application to function in a read-only mode when offline.

**Note:** In certain scenarios you should delete the cache. For example when logging out, it's very important to delete the cache folder to prevent previous user's data being accidentally revealed to a new user account, and to prevent errors. You can achieve that using the following:
```csharp
await Api.DisposeCache();
```
##### ApiResponseCache parameter
An optional parameter of `Api.Get()` is called cacheChoice which you can set to any of the following:


**Prefer:** Means if a cache is available, that's preferred and there is no need for a fresh Web Api request.

**PreferThenUpdate:**  If a cache is available, that's returned immediately. But a call will still be made to the server to check for an update, in which case a provided refresher delegate will be invoked.

**Accept (default):** Means a new request should be sent. But if it failed and a cache is available, then that's accepted.

**AcceptButWarn:** Means a new request should be sent. But if it failed and a cache is available, then that's accepted. However, a warning toast will be displayed to the user in that case to say: The latest data cannot be received from the server right now.

**Refuse:** Means only a fresh response from the server is acceptable, and any cache should be ignored.

**OnError parameter**:
Another optional parameter of `Api.Get()` is called errorAction, which allows you to specify what should happen in case the network or server is down, or there is any problem in processing the response. For example:
```csharp
await Api.Get<Order[]>("api/v1/orders",  OnError.Ignore);
```
The default option is to show a Toast.  Learn more about OnError.

##### Example configurations
If the API responds successfully with no error, the fresh result will always be returned and your extra parameters will be ignored anyway (unless Cache option of Prefer is used). But if an error occurred, then use one of the following options depending on what outcome is acceptable for you:

A: Accept the latest cache. If none is available, inform the user to prevent confusion and return null (don't throw):
```csharp
return await Api.Get<Order[]>("...");
// which means the default parameters of:
return await Api.Get<Order[]>("...", OnError.Toast, ApiResponseCache.Accept);
```
B: Don't accept the cache. Inform the user and return null (don't throw):
```csharp
return await Api.Get<Order[]>("...", OnError.Toast, ApiResponseCache.Refuse)
```
C: Don't accept the cache. Just throw. 
```csharp
return await Api.Get<Order[]>("...", OnError.Throw, ApiResponseCache.Refuse)
```
D: Accept the latest cache. If none is available, throw:
```csharp
return await Api.Get<Order[]>("...", OnError.Throw, ApiResponseCache.Accept)
```
E: Accept the latest cache. If none is available just return null without informing the user:
```csharp
return await Api.Get<Order[]>("...", OnError.Ignore)
```
F: If we already have it cached, just use that (fastest option) and don't even send a new request.
```csharp
return await Api.Get<Order[]>("...", ApiResponseCache.Prefer);
...
```
##### Where to use it?
It's recommended to make all API calls in your business domain or service classes in the App, which will be then called in your UI.

**Example:**
```csharp
public class Order
{
     ...
     public static Task<Order[]> GetOrders() => Api.HttpGet<Order[]>("v1/orders");
     ...
}
```
This simple line is enough to handle all things related to invoking an API including: deserializing the result, handling exceptions, using the latest cache if the network or API server is down, informing the user, ...

In the UI then you can use this as your list module's data source.
```xml
<ListView ... DataSource="@await Order.GetOrders()" />
```
##### Best practice for fastest UX -> ApiResponseCache.PreferThenUpdate
Imagine a page which uses GET APIs to fetch and show a list of some data. At the first time that you visit that page, fresh data will be downloaded, cached locally and rendered.

In a typical scenario, you will probably go to another page and then back to this page. When you go back, the remote data may or may not have been changed. And you don't know what's the case. So you have to call the Web API for fresh data and wait for the response before rendering the page which will slow things down and annoy the user.

##### Zebble provides a better way!

When calling Api.Get(...) you can select the **PreferThenUpdate** cache option which makes the framework to immediately return the previously cached data (if available), which makes the page render immediately.
It will then also, in parallel, send a fresh request to the server to ask for fresh data.
If and only if the returned data is different from its previous cache (from which the UI is already rendered) then the UI will be refreshed to show the updated data. To achieve that you should provide a delegate which will update your UI.
This way every page of your app will be displayed immediately, providing the best user experience. Then if the data was changed, it will then reload the UI to reflect that, often in less than a second.

**Example:**
```xml
...
<ListView z-of="Product, Row" Id="ProductsList" DataSource="Items">
      .....
</ListView>
```
And in the code behind:
```csharp
Product[] Items;
bool IsReturnVisitToCachedPage = false;

public override async Task OnInitializing()
{
      Items = await GetOrRefreshDataSource();

      await base.OnInitializing();
      await InitializeComponents();
      ...

    // If the page is cached, every time the user comes back to this page the ShownEvent will be fired (but not OnInitializing).
    // If you specifically don't want the page to be cached, you can remove it.
     await WhenShown(() =>
     {
           if (IsReturnVisitToCachedPage) await GetOrRefreshDataSource();
          else IsReturnVisitToCachedPage = true; // for next time
     });
}

Task<Product[]> GetOrRefreshDataSource()
{
     return Api.Get<Product[]>("api/v1/products", ApiResponseCache.PreferThenUpdate, Refresh);
}

Task Refresh(Product[] items) => WhenShown(() => ProductsList.UpdateSource(Items = items));
```

##### POST, PUT, PATCH AND DELETE APIS

For Web APIs that change data on the server, you should use an appropriate HTTP method for each function POST, PUT based API for every API function that:

- Use **POST** to create one or more new objects
- Use **PUT** to update one or more objects completely
- Use **PATCH** to update one or more objects partially
- Use **DELETE** to delete one or more objects.
- Use **POST** to performs a complex function involving a mix of the above

##### Parameters 
The parameters can be sent in either of the following methods:

Embedded in the url route or sent as query string
Added to a data transfer class and sent as FORM.

##### Url / querystring approach

The following example shows an API which takes its input parameters via route and query string. You can use only route, only querystring, or any combination of them. The following example takes in the userId parameter via route, and categoryId and message parameters as query string.
```csharp
[HttpPost, Route("user/notify/{userId}")
public IHttpActionResult Notify(string userId, int categoryId, string message)
{
     ....
     return Ok();
}
```
To invoke it from the mobile app you can use:
```csharp
if (await Api.Post("user/notify/" + myUserId +"?categoryId=" + myCatId +"&message=" + myMessage))
{
     // Successful
}
```
##### Form approach

You can also use the http request's form to send the data. Unfortunately the WebApi will not bind the form body parameters to direct API method arguments. Instead, you should define a single class that wraps all the parameters as its properties, and add an instance of that class as the API method parameter.
```csharp
public class NotifyUserArgs
{
     public string UserId;
     public int CategoryId;
     public string Message;
}
[HttpPost, Route("user/notify")]
public IHttpActionResult Notify(NotifyUserArgs args)
{
     ...
     return Ok();
}
```
To invoke it from the mobile app you can use an anonymous object:
```csharp
if (await Api.Post("user/notify" , new { UserId = myUserId, CategoryId=myCatId, Message= myMessage }))
{
     // Successful
}
```
The anonymous object's property names should match the same names as the properties that the API expects. Alternatively, you can send a mobile domain object as long as it has exactly the properties that the API method needs.

##### Returning a result
- If your API function should return void, you can use return `Ok();` 
- If your API function should return a literal (e.g. string, date or number) then use: `return Ok("some value");`
- If your API function should return an object then use `return Ok(resultObject);`
- If a POST Api returns something, then in the mobile app which calls it you should specify the type of the return object as a generic parameter. For example:
```csharp
// This example will receive just a number back:
var newCount = await Api.Post<int>("orders/add", new { Parameter1="...", Parameter2="..." });

// This example will receive objects back.
var result = await Api.Post<Order[]>("orders/add", new { Parameter1="...", Parameter1="..." });
```
##### Exception handling
Just like GET methods, the Post method takes an optional parameter named errorAction with the options of: Throw, Ignore, Toast and Alert. If you don't know how it works, make sure to check the documentation on calling the GET methods.

The default value is Alert. This means that if the API call failed for any reason, instead of throwing an exception or crashing the app, an error message will be displayed to the user to click OK. **This means that you don't need to wrap your calls to Api.Post in a try-catch block**.

When the `Api.Post()` method is called with the errorAction being anything other than Throw, if then an error occurs:

If it's a value returning Post call, it returns null (or default for value types).
If it's a void, then it will return false.
So in the mobile app code you can just handle the false or null case. For example:
```csharp
// Void returning API
if (await Api.Post("myUrl", ...))
{
     // Success code. E.g. redirect to another page.
     // Otherwise ignore (as the user will have seen the error message).
}

// Value returning API
var result = await Api.Post<Order[]>("myUrl", ...);
if (result != null)
{
     // Success code. Now you can use the result.
     // Otherwise ignore (as the user will have seen the error message).
}
```