#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using SFTP_Browser.Models;
using SFTP_Browser.ViewModels;

namespace SFTP_Browser.Services;

public sealed class TransferQueueService : IDisposable
{
    private readonly ConcurrentQueue<(TransferItemViewModel vm, Func<CancellationToken, IProgress<double>?, Task> work)> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly CancellationTokenSource _cts = new();

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly SemaphoreSlim _concurrency;

    public int MaxConcurrency { get; }

    public TransferQueueService(DispatcherQueue dispatcherQueue, int maxConcurrency = 2)
    {
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        MaxConcurrency = maxConcurrency;
        _concurrency = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        // Dispatcher loop (not worker) - starts a task per dequeued item but concurrency-limited.
        _ = Task.Run(DispatchLoopAsync);
    }

    public void Enqueue(TransferItemViewModel vm, Func<CancellationToken, IProgress<double>?, Task> work)
    {
        _queue.Enqueue((vm, work));
        _signal.Release();
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _))
        {
        }
    }

    private void Ui(Action action)
    {
        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        _dispatcherQueue.TryEnqueue(() => action());
    }

    private async Task DispatchLoopAsync()
    {
        var token = _cts.Token;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (!_queue.TryDequeue(out var item))
                continue;

            await _concurrency.WaitAsync(token);

            _ = Task.Run(async () =>
            {
                try
                {
                    await RunOneAsync(item.vm, item.work, token);
                }
                finally
                {
                    _concurrency.Release();
                }
            }, token);
        }
    }

    private async Task RunOneAsync(TransferItemViewModel vm, Func<CancellationToken, IProgress<double>?, Task> work, CancellationToken token)
    {
        Ui(vm.SetRunning);

        using var throttled = new ThrottledProgress(_dispatcherQueue, vm);

        try
        {
            await work(token, throttled);
            Ui(vm.SetCompleted);
        }
        catch (OperationCanceledException)
        {
            Ui(() =>
            {
                vm.Status = TransferStatus.Canceled;
                vm.StatusText = "Canceled";
            });
        }
        catch (Exception ex)
        {
            Ui(() => vm.SetFailed(ex.Message));
        }
    }

    public void Dispose() => _cts.Cancel();

    private sealed class ThrottledProgress : IProgress<double>, IDisposable
    {
        private readonly DispatcherQueue _dq;
        private readonly TransferItemViewModel _vm;
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _localCts = new();

        private int _latestPermille; // 0..10000 maps to 0.0..1.0

        public ThrottledProgress(DispatcherQueue dq, TransferItemViewModel vm)
        {
            _dq = dq;
            _vm = vm;

            // ~10 updates/sec max
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            _ = Task.Run(LoopAsync);
        }

        public void Report(double value)
        {
            if (value < 0) value = 0;
            if (value > 1) value = 1;

            var scaled = (int)Math.Round(value * 10000, MidpointRounding.AwayFromZero);
            if (scaled < 0) scaled = 0;
            if (scaled > 10000) scaled = 10000;

            Interlocked.Exchange(ref _latestPermille, scaled);
        }

        private async Task LoopAsync()
        {
            var token = _localCts.Token;
            try
            {
                while (await _timer.WaitForNextTickAsync(token))
                {
                    var scaled = Interlocked.CompareExchange(ref _latestPermille, 0, 0);
                    var v = scaled / 10000d;
                    _dq.TryEnqueue(() => _vm.Progress = v);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void Dispose()
        {
            _localCts.Cancel();
            _timer.Dispose();
        }
    }
}
