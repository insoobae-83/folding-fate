using VContainer;
using VContainer.Unity;
using FoldingFate.Features.Battle.Controllers;
using FoldingFate.Features.Battle.Data;
using FoldingFate.Features.Battle.Systems;
using FoldingFate.Features.Entity.Systems;
using FoldingFate.Infrastructure.EventBus;

namespace FoldingFate.Features.Battle
{
    public class BattleSceneInstaller : LifetimeScope
    {
        [UnityEngine.SerializeField] private BattleCharacterData[] _characterDataList;
        [UnityEngine.SerializeField] private BattleMonsterData _monsterData;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<EventBus>(Lifetime.Singleton);
            builder.Register<EntityFactory>(Lifetime.Singleton);
            builder.Register<StatsSystem>(Lifetime.Singleton);
            builder.Register<HealthSystem>(Lifetime.Singleton);
            builder.Register<BattleSystem>(Lifetime.Singleton);
            builder.Register<ResolveSystem>(Lifetime.Singleton);
            builder.Register<ApplySystem>(Lifetime.Singleton);
            builder.Register<TurnSystem>(Lifetime.Singleton);
            builder.RegisterInstance(_characterDataList);
            builder.RegisterInstance(_monsterData);
            builder.RegisterEntryPoint<BattleController>();
            builder.RegisterComponentInHierarchy<BattleEffectController>();
        }
    }
}
