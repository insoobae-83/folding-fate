using System;
using R3;
using VContainer.Unity;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;

namespace FoldingFate.Features.Poker.Controllers
{
    public class RoundController : IStartable, IDisposable
    {
        private readonly DealSystem _dealSystem;
        private readonly PokerViewModel _vm;
        private readonly CompositeDisposable _disposables = new();

        public RoundController(DealSystem dealSystem, PokerViewModel vm)
        {
            _dealSystem = dealSystem;
            _vm = vm;
        }

        public void Start()
        {
            _dealSystem.InitializeDeck();
            _dealSystem.DrawToFull();

            _vm.ToggleSelectCommand
                .Subscribe(index => _dealSystem.ToggleSelect(index))
                .AddTo(_disposables);

            _vm.SubmitCommand
                .Subscribe(_ =>
                {
                    var result = _dealSystem.EvaluateSelected();
                    _vm.PushHandResult(result);
                })
                .AddTo(_disposables);

            _vm.DrawCommand
                .Subscribe(_ => _dealSystem.DrawToFull())
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
