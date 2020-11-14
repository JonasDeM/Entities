using System;
using NUnit.Framework;
using Unity.Entities.Tests;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Unity.Entities.Editor.Tests
{
    class EntityDebuggerTests : ECSTestsFixture
    {
        private EntityDebugger m_Window;
        private ComponentSystem m_System;
        private EntityQuery entityQuery;
        private Entity m_Entity;

        class SingleGroupSystem : ComponentSystem
        {
            protected override void OnUpdate()
            {
                throw new NotImplementedException();
            }

            protected override void OnCreate()
            {
                GetEntityQuery(typeof(EcsTestData));
            }
        }

        private static void CloseAllDebuggers()
        {
            var windows = Resources.FindObjectsOfTypeAll<EntityDebugger>();
            foreach (var window in windows)
                if (window != null)
                    window.Close();
        }

        private const string World2Name = "Test World 2";
        private World World2;
        private PlayerLoopSystem m_PrevPlayerLoop;

        public override void Setup()
        {
            base.Setup();

            CloseAllDebuggers();

            m_Window = EditorWindow.GetWindow<EntityDebugger>();

            m_System = World.GetOrCreateSystem<SingleGroupSystem>();
            World.GetOrCreateSystem<SimulationSystemGroup>().AddSystemToUpdateList(m_System);

            var playerLoop = PlayerLoop.GetDefaultPlayerLoop(); // TODO(DOTS-2283): shouldn't stomp default player loop here
            ScriptBehaviourUpdateOrder.AddWorldToPlayerLoop(World, ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);

            World2 = new World(World2Name);
            var emptySys = World2.GetOrCreateSystem<EmptySystem>();
            World.GetOrCreateSystem<SimulationSystemGroup>().AddSystemToUpdateList(emptySys);
            World.GetOrCreateSystem<SimulationSystemGroup>().SortSystems();

            entityQuery = m_System.EntityQueries[0];

            m_Entity = m_Manager.CreateEntity(typeof(EcsTestData));
        }

        public override void TearDown()
        {
            CloseAllDebuggers();

            if (World2 != null)
            {
                World2.Dispose();
                World2 = null;
            }

            base.TearDown();
        }

        [Test]
        public void WorldPopup_RestorePreviousSelection()
        {
            World world = null;
            var popup = new WorldPopup(() => null, x => world = x, () => true, () => {}, () => true, v => {});
            popup.TryRestorePreviousSelection(false, WorldPopup.kNoWorldName);
            Assert.AreEqual(World.All[0], world);
            popup.TryRestorePreviousSelection(false, World2Name);
            Assert.AreEqual(World2, world);
        }

        [Test]
        public void EntityDebugger_SetSystemSelection()
        {
            // TODO EntityManager is no longer a system
            /*
            m_Window.SetSystemSelection(m_Manager, World.Active, true, true);

            Assert.AreEqual(World.Active, m_Window.SystemSelectionWorld);

            Assert.Throws<ArgumentNullException>(() => m_Window.SetSystemSelection(m_Manager, null, true, true));
            */
        }

        [Test]
        public void EntityDebugger_DestroyWorld()
        {
            m_Window.SetWorldSelection(World2, true);
            Assert.IsFalse(m_Window.systemListView.NeedsReload);
            World2.Dispose();
            World2 = null;
            Assert.IsTrue(m_Window.systemListView.NeedsReload);
        }

        [Test]
        public void EntityDebugger_DestroySystem()
        {
            m_Window.SetWorldSelection(World2, true);
            Assert.IsFalse(m_Window.systemListView.NeedsReload);
            var emptySystem = World2.GetExistingSystem<EmptySystem>();
            World2.DestroySystem(emptySystem);
            Assert.IsTrue(m_Window.systemListView.NeedsReload);
        }

        [Test]
        public void EntityDebugger_SetAllSelections()
        {
            var entityListQuery = new EntityListQuery(entityQuery);
            EntityDebugger.SetAllSelections(World, m_System, entityListQuery, m_Entity);

            Assert.AreEqual(World, m_Window.WorldSelection);
            Assert.AreEqual(m_System, m_Window.SystemSelection);
            Assert.AreEqual(entityQuery, m_Window.EntityListQuerySelection.Group);
            Assert.AreEqual(m_Entity, m_Window.EntitySelection);
        }

        [Test]
        public void EntityDebugger_RememberSelections()
        {
            var entityListQuery = new EntityListQuery(entityQuery);
            EntityDebugger.SetAllSelections(World, m_System, entityListQuery, m_Entity);

            m_Window.SetWorldSelection(null, true);

            m_Window.SetWorldSelection(World, true);

            Assert.AreEqual(World, m_Window.WorldSelection);
            Assert.AreEqual(m_System, m_Window.SystemSelection);
            Assert.AreEqual(entityQuery, m_Window.EntityListQuerySelection.Group);
            Assert.AreEqual(m_Entity, m_Window.EntitySelection);
        }

        [Test]
        public void EntityDebugger_SetAllEntitiesFilter()
        {
            var query = new EntityQueryDesc()
            {
                All = new ComponentType[] {ComponentType.ReadWrite<EcsTestData>() },
                Any = new ComponentType[0],
                None = new ComponentType[0]
            };
            var listQuery = new EntityListQuery(query);

            m_Window.SetWorldSelection(World, true);
            m_Window.SetSystemSelection(null, null, true, true);
            m_Window.SetAllEntitiesFilter(listQuery);
            Assert.AreEqual(query, m_Window.EntityListQuerySelection.QueryDesc);

            m_Window.SetEntityListSelection(null, true, true);
            m_Window.SetSystemSelection(null, World, true, true);
            m_Window.SetAllEntitiesFilter(listQuery);
            Assert.AreEqual(query, m_Window.EntityListQuerySelection.QueryDesc);

            m_Window.SetSystemSelection(m_System, World, true, true);
            m_Window.SetAllEntitiesFilter(listQuery);
            Assert.AreNotEqual(listQuery, m_Window.EntityListQuerySelection);
        }

        [Test]
        public void EntityDebugger_StylesIntact()
        {
            Assert.IsNotNull(EntityDebuggerStyles.ComponentRequired);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentExclude);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadOnly);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadWrite);

            Assert.IsNotNull(EntityDebuggerStyles.ComponentRequired.normal.background);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentExclude.normal.background);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadOnly.normal.background);
            Assert.IsNotNull(EntityDebuggerStyles.ComponentReadWrite.normal.background);
        }
    }
}
