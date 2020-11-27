namespace Zebble
{
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    partial class BaseApi
    {
#if MVVM
      static Task HideWaiting()
      {
          Zebble.Mvvm.DialogViewModel.Current.HideWaiting();
          return Task.CompletedTask;
      }

      static Task ShowWaiting()
      {
           Zebble.Mvvm.DialogViewModel.Current.ShowWaiting();
           return Task.CompletedTask;
      }

      static Task Toast(string message)
      {
           Zebble.Mvvm.DialogViewModel.Current.Toast(message);
           return Task.CompletedTask;
      }
#else
        static Task HideWaiting() => Waiting.Hide();
        static Task ShowWaiting() => Waiting.Show();
        static Task Toast(string message) => Alert.Toast(message);
#endif 
    }
}