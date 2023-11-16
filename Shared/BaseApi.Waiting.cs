namespace Zebble
{
    using System.Threading.Tasks;

    partial class BaseApi
    {
#if MVVM
        static Task HideWaiting()
        {
            Zebble.Mvvm.DialogsViewModel.Current.HideWaiting();
            return Task.CompletedTask;
        }

        static Task ShowWaiting()
        {
            Zebble.Mvvm.DialogsViewModel.Current.ShowWaiting();
            return Task.CompletedTask;
        }

        static Task Toast(string message)
        {
            Zebble.Mvvm.DialogsViewModel.Current.Toast(message);
            return Task.CompletedTask;
        }
#else
        static Task HideWaiting() => Dialogs.Current.HideWaiting();

        static Task ShowWaiting() => Dialogs.Current.ShowWaiting();

        static Task Toast(string message) => Dialogs.Current.Toast(message);
#endif 
    }
}