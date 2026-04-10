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
            _dealSystem.DrawToFull();

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
                }, AwaitOperation.Drop)
                .AddTo(_disposables);

            _vm.DrawCommand
                .Subscribe(_ => _dealSystem.DrawToFull())
                .AddTo(_disposables);

            _vm.DiscardCommand
                .Subscribe(_ => _dealSystem.DiscardSelected())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
