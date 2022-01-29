using Husky.Stdout;

namespace Husky.Utils;
 public sealed class DisposableScope : IDisposable
{
   private readonly Action? _beforeDispose;
   private readonly Action? _afterDispose;

   // you can use ConcurrentQueue if you need thread-safe solution
   private readonly Queue<IDisposable> _disposables = new();

   public DisposableScope(Action? beforeDispose = default, Action? afterDispose = default)
   {
      _beforeDispose = beforeDispose;
      _afterDispose = afterDispose;
   }

   public T Using<T>(T disposable) where T : IDisposable
   {
      _disposables.Enqueue(disposable);
      return disposable;
   }

   public void Dispose()
   {
      _beforeDispose?.Invoke();
      foreach (var item in _disposables)
      {
         try
         {
            item.Dispose();
         }
         catch (Exception e)
         {
            e.Message.LogVerbose(ConsoleColor.DarkRed);
         }
      }
      _afterDispose?.Invoke();
   }
}

