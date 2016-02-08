﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Engine.Design;

namespace SiliconStudio.Xenko.Engine.Tests
{
    /// <summary>
    /// Tests for <see cref="Entity"/> and <see cref="EntityComponentCollection"/>.
    /// </summary>
    [TestFixture]
    public class TestEntity
    {
        /// <summary>
        /// Test various manipulation on Entity.Components with TransformComponent and CustomComponent
        /// </summary>
        [Test]
        public void TestComponents()
        {
            var entity = new Entity();

            // Plug an event handler to track events
            var events = new List<EntityComponentEvent>();
            entity.Owner = new DelegateEntityComponentNotify(evt => events.Add(evt));

            // Make sure that an entity has a transform component
            Assert.NotNull(entity.Transform);
            Assert.AreEqual(1, entity.Components.Count);
            Assert.AreEqual(entity.Transform, entity.Components[0]);

            // Remove Transform
            var oldTransform = entity.Transform;
            entity.Components.RemoveAt(0);
            Assert.Null(entity.Transform);

            // Check that events is correctly propagated
            Assert.AreEqual(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 0, oldTransform, null) }, events);
            events.Clear();

            // Re-add transform
            var transform = new TransformComponent();
            entity.Components.Add(transform);
            Assert.NotNull(entity.Transform);

            // Check that events is correctly propagated
            Assert.AreEqual(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 0, null, transform) }, events);
            events.Clear();

            // We cannot add a single component
            var invalidOpException = Assert.Catch<InvalidOperationException>(() => entity.Components.Add(new TransformComponent()));
            Assert.AreEqual($"Cannot add a component of type [{typeof(TransformComponent)}] multiple times", invalidOpException.Message);

            invalidOpException = Assert.Catch<InvalidOperationException>(() => entity.Components.Add(transform));
            Assert.AreEqual("Cannot add a same component multiple times. Already set at index [0]", invalidOpException.Message);

            // We cannot add a null component
            Assert.Catch<ArgumentNullException>(() => entity.Components.Add(null));

            // Replace Transform
            var custom = new CustomEntityComponent();
            entity.Components[0] = custom;
            Assert.Null(entity.Transform);

            // Check that events is correctly propagated
            Assert.AreEqual(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 0, transform, custom) }, events);
            events.Clear();

            // Add again transform component
            transform = new TransformComponent();
            entity.Components.Add(transform);
            Assert.NotNull(entity.Transform);
            Assert.AreEqual(transform, entity.Components[1]);

            // Check that TransformComponent is on index 1 now
            Assert.AreEqual(new List<EntityComponentEvent>() { new EntityComponentEvent(entity, 1, null, transform) }, events);
            events.Clear();

            // Clear components and check that Transform is also removed
            entity.Components.Clear();
            Assert.AreEqual(0, entity.Components.Count);
            Assert.Null(entity.Transform);

            // Check that events is correctly propagated
            Assert.AreEqual(new List<EntityComponentEvent>()
            {
                new EntityComponentEvent(entity, 1, transform, null),
                new EntityComponentEvent(entity, 0, custom, null),
            }, events);
            events.Clear();
        }

        /// <summary>
        /// Tests multiple components.
        /// </summary>
        [Test]
        public void TestMultipleComponents()
        {
            // Check that TransformComponent cannot be added multiple times
            Assert.False(EntityComponentAttributes.Get<TransformComponent>().AllowMultipleComponents);

            // Check that CustomEntityComponent can be added multiple times
            Assert.True(EntityComponentAttributes.Get<CustomEntityComponent>().AllowMultipleComponents);

            var entity = new Entity();

            var transform = entity.Get<TransformComponent>();
            Assert.NotNull(transform);
            Assert.AreEqual(entity.Transform, transform);

            var custom = entity.GetOrCreate<CustomEntityComponent>();
            Assert.NotNull(custom);

            var custom2 = new CustomEntityComponent();
            entity.Components.Add(custom2);
            Assert.AreEqual(custom, entity.Get<CustomEntityComponent>());

            var allComponents = entity.GetAll<CustomEntityComponent>().ToList();
            Assert.AreEqual(new List<EntityComponent>() { custom, custom2 }, allComponents);
        }

        private class DelegateEntityComponentNotify : IEntityComponentNotify
        {
            private readonly Action<EntityComponentEvent> action;

            public DelegateEntityComponentNotify(Action<EntityComponentEvent> action)
            {
                if (action == null) throw new ArgumentNullException(nameof(action));
                this.action = action;
            }

            public void OnComponentChanged(Entity entity, int index, EntityComponent oldComponent, EntityComponent newComponent)
            {
                action(new EntityComponentEvent(entity, index, oldComponent, newComponent));
            }
        }

        struct EntityComponentEvent : IEquatable<EntityComponentEvent>
        {
            public EntityComponentEvent(Entity entity, int index, EntityComponent oldComponent, EntityComponent newComponent)
            {
                Entity = entity;
                this.Index = index;
                OldComponent = oldComponent;
                NewComponent = newComponent;
            }

            public readonly Entity Entity;

            public readonly int Index;

            public readonly EntityComponent OldComponent;

            public readonly EntityComponent NewComponent;

            public bool Equals(EntityComponentEvent other)
            {
                return Equals(Entity, other.Entity) && Index == other.Index && Equals(OldComponent, other.OldComponent) && Equals(NewComponent, other.NewComponent);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is EntityComponentEvent && Equals((EntityComponentEvent)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Entity != null ? Entity.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ Index;
                    hashCode = (hashCode*397) ^ (OldComponent != null ? OldComponent.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (NewComponent != null ? NewComponent.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(EntityComponentEvent left, EntityComponentEvent right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(EntityComponentEvent left, EntityComponentEvent right)
            {
                return !left.Equals(right);
            }
        }
    }

    [DataContract()]
    [DefaultEntityComponentProcessor(typeof(CustomEntityComponentProcessor))]
    [AllowMultipleComponents]
    public sealed class CustomEntityComponent : CustomEntityComponentBase
    {
    }

    [DataContract()]
    public abstract class CustomEntityComponentBase : EntityComponent
    {
        public Action<CustomEntityComponentEventArgs> Changed;
    }
}