using UnityEngine;
using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Battle.Components;
using FoldingFate.Features.Battle.Controllers;
using FoldingFate.Features.Battle.Data;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Card.Systems;
using FoldingFate.Features.Entity.Systems;
using FoldingFate.Features.Poker.Controllers;
using FoldingFate.Features.Poker.Data;
using FoldingFate.Features.Poker.Models;
using FoldingFate.Features.Poker.Systems;
using FoldingFate.Features.Poker.UI.ViewModels;
using FoldingFate.Features.Poker.UI.Views;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Features.Battle
{
    public class BattleSceneInstaller : LifetimeScope
    {
        [Header("Battle Data")]
        [SerializeField] private BattleCharacterData[] _characterDataList;
        [SerializeField] private BattleMonsterData _monsterData;

        [Header("Entity Views")]
        [SerializeField] private EntityView[] _allyViews;
        [SerializeField] private EntityView _enemyView;

        [Header("Poker")]
        [SerializeField] private PokerConfig _pokerConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            // Infrastructure
            builder.Register<EventBus>(Lifetime.Singleton);

            // Entity Systems
            builder.Register<EntityFactory>(Lifetime.Singleton);
            builder.Register<StatsSystem>(Lifetime.Singleton);
            builder.Register<HealthSystem>(Lifetime.Singleton);

            // Battle Systems
            builder.Register<BattleSystem>(Lifetime.Singleton);
            builder.Register<ResolveSystem>(Lifetime.Singleton);
            builder.Register<ApplySystem>(Lifetime.Singleton);
            builder.Register<TurnSystem>(Lifetime.Singleton);

            // Battle Data
            builder.RegisterInstance(_characterDataList);
            builder.RegisterInstance(_monsterData);
            builder.RegisterInstance(_allyViews).As<EntityView[]>();
            builder.RegisterInstance(_enemyView);

            // Battle Controllers
            builder.RegisterEntryPoint<BattleController>();
            builder.RegisterComponentInHierarchy<BattleEffectController>();

            // Poker
            builder.RegisterInstance(_pokerConfig);
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
