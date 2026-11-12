
using Microsoft.Extensions.Options;
using Services.Identity.API.DTOs;


namespace Services.Identity.API.Services
{
    public sealed class SapConcurrencyService : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        public int MaxConcurrency { get; }

        public SapConcurrencyService(IOptions<SapSettings> sapSettings)
        {
            int configured = sapSettings.Value.MaxConcurrency;
            MaxConcurrency = configured > 0 ? configured : 10;
            _semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
        }

        public async Task<List<TResult>> EjecutarLoteAsync<TItem, TResult>(
            IEnumerable<TItem> items,
            Func<TItem, Task<TResult>> operacionSapAsync,
            CancellationToken cancellationToken = default)
        {
            var tasks = items.Select(async item =>
            {
                await _semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await operacionSapAsync(item);
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            var resultados = await Task.WhenAll(tasks);
            return resultados.ToList();
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }


}
