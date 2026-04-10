using System;
using Cysharp.Threading.Tasks;
using R3;
using VContainer.Unity;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.Controllers
{
    public class RoundController : IStartable, IDisposable
    {
        private readonly DealSystem _dealSystem;
        private readonly PokerViewModel _vm;
        private readonly PokerConfig _config;
        private readonly CompositeDisposable _disposables = new();

        public RoundController(DealSystem dealSystem, PokerViewModel vm, PokerConfig config)
        {
            _dealSystem = dealSystem;
            _vm = vm;
            _config = config;
        }

        public void Start()
        {
            _dealSystem.InitializeDeck();
            DealToFullAsync().Forget();

            _vm.ToggleSelectCommand
                .Subscribe(index => _dealSystem.ToggleSelect(index))
                .AddTo(_disposables);

            _vm.SubmitCommand
                .SubscribeAwait(async (_, ct) =>
                {
                    var result = _dealSystem.EvaluateSelected();
                    _vm.BeginShowcase(result);

                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.ShowcaseDurationSeconds),
                        cancellationToken: ct);

                    _vm.EndShowcase();
                    _dealSystem.DiscardSelected();
                    _vm.PushHandResult(result);

                    await DealToFullAsync(ct);
                }, AwaitOperation.Drop)
                .AddTo(_disposables);

            _vm.DiscardCommand
                .SubscribeAwait(async (_, ct) =>
                {
                    _dealSystem.DiscardSelected();
                    await DealToFullAsync(ct);
                }, AwaitOperation.Drop)
                .AddTo(_disposables);
        }

        private async UniTask DealToFullAsync(System.Threading.CancellationToken ct = default)
        {
            int needed = _dealSystem.CardsNeeded();
            if (needed <= 0) return;

            _vm.BeginDealing();
            for (int i = 0; i < needed; i++)
            {
                _dealSystem.DrawOne();
                if (i < needed - 1)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_config.DealIntervalSeconds),
                        cancellationToken: ct);
                }
            }
            _vm.EndDealing();
        }

        public void Dispose() => _disposables.Dispose();
    }
}
