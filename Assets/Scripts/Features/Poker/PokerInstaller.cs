using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;
using FoldingFate.Features.Poker.UI.Views;

namespace FoldingFate.Features.Poker
{
    public class PokerInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<DeckModel>(Lifetime.Singleton);
            builder.Register<HandModel>(_ => new HandModel(8), Lifetime.Singleton);
            builder.Register<HandEvaluator>(Lifetime.Singleton);
            builder.Register<DealSystem>(Lifetime.Singleton);
            builder.Register<PokerViewModel>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RoundController>();
            builder.RegisterComponentInHierarchy<PokerView>();
        }
    }
}
