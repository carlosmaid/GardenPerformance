﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

using SEGarden.Extensions;
using SEGarden.Logic;
using SEGarden.Logging;

using GP.Concealment.Sessions;

namespace GP.Concealment.World.Entities {

    /// <summary>
    /// Controllable Entities can be moving and controlled
    /// </summary>
    /// <remarks>
    /// Check moving every update because there are no events for movement
    /// Physics has OnWorldPositionChanged, but it's just an action
    /// We might remove all these events and just put it in the sector directly eventually
    /// </remarks>
    public abstract class ControllableEntity : RevealedEntity {

        #region Static ControllableEntities Events

        private static Action<ControllableEntity> ControllableEntityAddition;
        public static event Action<ControllableEntity> ControllableEntityAdded {
            add { ControllableEntityAddition += value; }
            remove { ControllableEntityAddition -= value; }
        }

        private static Action<ControllableEntity> ControllableEntityMovement;
        public static event Action<ControllableEntity> ControllableEntityMoved {
            add { ControllableEntityMovement += value; }
            remove { ControllableEntityMovement -= value; }
        }

        private static Action<ControllableEntity> ControllableEntityRemoval;
        public static event Action<ControllableEntity> ControllableEntityRemoved {
            add { ControllableEntityRemoval += value; }
            remove { ControllableEntityRemoval -= value; }
        }

        private static Action<ControllableEntity> ControllableEntityControlAcquisition;
        public static event Action<ControllableEntity> ControllableEntityControlled {
            add { ControllableEntityControlAcquisition += value; }
            remove { ControllableEntityControlAcquisition -= value; }
        }

        private static Action<ControllableEntity> ControllableEntityControlRelease;
        public static event Action<ControllableEntity> ControllableEntityReleased {
            add { ControllableEntityControlRelease += value; }
            remove { ControllableEntityControlRelease -= value; }
        }

        private static void NotifyAdded(ControllableEntity entity) {
            //Log.Trace("ControllableEntity " + entity.DisplayName + " added.", "NotifyAdded");
            if (ControllableEntityAddition != null) ControllableEntityAddition(entity);
        }

        private static void NotifyMoved(ControllableEntity entity) {
            //Log.Trace("ControllableEntity " + entity.DisplayName + " moved ", "NotifyMoved");
            if (ControllableEntityMovement != null) ControllableEntityMovement(entity);
        }

        private static void NotifyRemoved(ControllableEntity entity) {
            //Log.Trace("ControllableEntity " + entity.DisplayName + " removed.", "NotifyRemoved");
            if (ControllableEntityRemoval != null) ControllableEntityRemoval(entity);
        }

        private void NotifyControlAcquired() {
            Log.Trace("ControllableEntity " + DisplayName + " controlled.", "NotifyRemoved");
            if (ControllableEntityControlAcquisition != null) ControllableEntityControlAcquisition(this);
        }

        private void NotifyControlReleased() {
            Log.Trace("ControllableEntity " + DisplayName + " released.", "NotifyRemoved");
            if (ControllableEntityControlRelease != null) ControllableEntityControlRelease(this);
        }

        #endregion
        #region Fields
        #endregion
        #region Properties

        public override Dictionary<uint, Action> UpdateActions {
            get {
                Dictionary<uint, Action> actions = base.UpdateActions;
                actions.Add(60, UpdateControl); // TODO: tweak resolution
                return actions;
            }
        }

        public override bool IsConcealableAuto {
            get { return base.IsConcealableAuto && !IsControlled; }
        }

        public bool IsMoving { get; private set; }
        public bool RecentlyMoved { get; private set; }
        public DateTime RecentlyMovedEnds { get; private set; }
        public virtual bool IsControlled { get { return IsMoving || RecentlyMoved;  } }

        #endregion
        #region Internal Events

        protected virtual void ControlAcquired() {
            NotifyControlAcquired();
        }

        protected virtual void ControlReleased() {
            NotifyControlReleased();
        }

        #endregion
        #region Constructors

        // Creation from ingame entity
        public ControllableEntity(IMyEntity entity) : base(entity) 
        {
            //Log.Trace("Running ControllableEntity ctr", "ctr");
            Log.ClassName = "GP.Concealment.World.Entities.ControllableEntity";
            Log.Trace("New Controllable Entity " + DisplayName, "ctr");
            //Log.Trace("Finished ControllableEntity ctr", "ctr");
        }

        // Byte Deserialization
        public ControllableEntity(VRage.ByteStream stream) : base(stream) {
            Log.ClassName = "GP.Concealment.World.Entities.ControllableEntity";
            IsMoving = stream.getBoolean();
            RecentlyMoved = stream.getBoolean();
            RecentlyMovedEnds = stream.getDateTime();
            Log.Trace("New Controllable Entity " + DisplayName, "ctr");
        }

        #endregion
        #region Serialization

        // Byte Serialization
        public override void AddToByteStream(VRage.ByteStream stream) {
            base.AddToByteStream(stream);
            stream.addBoolean(IsMoving);
            stream.addBoolean(RecentlyMoved);
            stream.addDateTime(RecentlyMovedEnds);
        }

        #endregion
        #region Updates

        public override void Initialize() {
            base.Initialize();
            NotifyAdded(this);
        }

        protected virtual void UpdateControl() {
            UpdateMoving();
        }

        public override void Terminate() {
            NotifyRemoved(this);
            base.Terminate();
        }

        #endregion
        #region Update Movement

        private void UpdateMoving() {
            //Log.Trace("Checking Physics of " + DisplayName, "CheckPhysics");

            bool wasMoving = IsMoving;
            bool wasControlled = IsControlled;

            IsMoving = Entity.IsMoving();

            if (IsMoving) {
                // mark for update asteroid proximity
                MovedSinceIsInAsteroidCheck = true;
            }
            else {

                // Update recently moved
                if (wasMoving) {
                    RecentlyMoved = true;
                    RecentlyMovedEnds = DateTime.UtcNow.AddSeconds(
                        Settings.Instance.ControlledMovingGraceTimeSeconds);
                }
                else if (RecentlyMoved && DateTime.UtcNow > RecentlyMovedEnds) {
                    RecentlyMoved = false;
                }
            }

            // Do events after above updates have finished
            if (IsMoving) {
                NotifyMoved(this);
            }
            if (IsControlled && !wasControlled) {
                ControlAcquired();
            }
            else if (!IsControlled && wasControlled) {
                ControlReleased();
            }
        }


        #endregion

    }

}
