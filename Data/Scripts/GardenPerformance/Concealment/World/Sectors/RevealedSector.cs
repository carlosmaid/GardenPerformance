﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.ModAPI;
using VRageMath;

using SEGarden.Logging;
using SEGarden.Math;

using GP.Concealment.Sessions;
using GP.Concealment.World.Entities;

namespace GP.Concealment.World.Sectors {

    /// <summary>
    /// The revealed sector tracks ingame entities that we might conceal,
    /// as well as those that might affect concealment.
    /// It determines what to conceal and what to reveal based on current game state
    /// </summary>
    /// <remarks>
    /// Do view, detect, communicate checks on controlled entities
    /// Do collide detects on moving cubegrids - need higher level org than just "entities with physics"
    /// Do spawn check on all spawned players
    /// </remarks>
    public class RevealedSector {

        protected static ConcealedSector Concealed {
            get { return ServerConcealSession.Instance.Manager.Concealed; }
        }

        private static Logger Log = 
            new Logger("GP.Concealment.World.Sectors.RevealedSector");

        #region Instance Fields

        // These cause us to reveal things

        // Populate this with everyone online
        // If someone isn't in a faction, store their factionId as 0
        //public Dictionary<long, List<long>> ActiveFactions = 
        //    new Dictionary<long, List<long>>();

        //public Dictionary<long, IMyFaction> ActiveFactions = 
        //    new Dictionary<long, List<long>>();

        private List<long> ActivePlayers = new List<long>();
        private List<long> SpawnOwnersNeeded = new List<long>();
        private bool UpdateSpawnOwnersNextUpdate;

        //private Dictionary<long, ControllableEntity> ControlledEntities =
        //    new Dictionary<long, ControllableEntity>();

        private Dictionary<long, ObservingEntity> ObservingEntities =
            new Dictionary<long, ObservingEntity>();

        // These can be concealed or marked to remain revealed
        private Dictionary<long, RevealedGrid> Grids =
            new Dictionary<long, RevealedGrid>();

        private AABBTree ObservingTree = new AABBTree();
        private AABBTree GridTree = new AABBTree();

        #endregion
        #region Public Field Access Helpers

        public List<RevealedGrid> RevealedGridsList() {
            List<RevealedGrid> revealedGridsList = Grids.Values.ToList();

            //Log.Trace("Returning revealed grids list of count " + 
            //    revealedGridsList.Count, "RevealedGridsList");

            return revealedGridsList;
        }

        public List<ObservingEntity> ObservingEntitiesList() {
            List<ObservingEntity> observingEntitiesList = 
                ObservingEntities.Values.ToList();

            //Log.Trace("Returning Observing Entities list of count " +
            //    observingEntitiesList.Count, "RevealedGridsList");

            return observingEntitiesList;
        }

        public RevealedGrid GetGrid(long entityId) {
            RevealedGrid grid;
            Grids.TryGetValue(entityId, out grid);
            return grid;
        }

        /// <summary>
        /// Tests whether the SpawnOwner (owner of a spawnable block) is amoung
        /// active players or their factions
        /// </summary>
        public bool SpawnOwnerNeeded(long ownerId) {
            //Log.Trace("Do we need spawn owner " + ownerId, "SpawnOwnerNeeded");
            //Log.Trace("Current owners " + String.Join(", ", SpawnOwnersNeeded), "SpawnOwnerNeeded");
            return SpawnOwnersNeeded.Contains(ownerId);
        }

        #endregion
        #region Private Field Access Helpers

        /*
        private void RememberControlledEntity(ControllableEntity e) {
            long id = e.EntityId;
            if (ControlledEntities.ContainsKey(id)) {
                Log.Error("Already added " + id, "RememberControlledEntity");
                return;
            }

            Log.Trace("Adding " + id, "RememberControlledEntity");
            ControlledEntities.Add(id, e);
        }

        private void ForgetControlledEntity(ControllableEntity e) {
            long id = e.EntityId;
            if (!ControlledEntities.ContainsKey(id)) {
                Log.Error("Not stored " + id, "ForgetControlledEntity");
                return;
            }

            Log.Trace("Removing " + id, "ForgetControlledEntity");
            ControlledEntities.Remove(id);
        }

        
        private void RememberCharacter(Character e) {
        }

        private void ForgetCharacter(Character e) {
        }
        
        private void RememberPlayerInFaction(long playerId, long factionId) {
            List<long> players;
            ActiveFactions.TryGetValue(factionId, out players);
            if (players == null) {
                Log.Trace("Adding factionId " + factionId, "RememberPlayerInFaction");
                ActiveFactions[factionId] = new List<long>();
            }
            else {
                if (players.Contains(playerId)) {
                    Log.Error("Already added playerId: " + playerId, "RememberPlayer");
                    return;
                }
            }

            ActiveFactions[factionId].Add(playerId);
        }

        private void ForgetPlayerInFaction(long playerId, long factionId) {
            List<long> players;
            ActiveFactions.TryGetValue(factionId, out players);
            if (players == null) {
                Log.Error("Faction wasn't stored.", "ForgetPlayerInFaction");
                ActiveFactions[factionId] = new List<long>();
            }
            else {
                if (!players.Contains(playerId)) {
                    Log.Error("Player wasn't stored!" + playerId, "ForgetPlayerInFaction");
                    return;
                }
            }

            ActiveFactions[factionId].Remove(playerId);
        }
        
        private void ChangeRememberedPlayerFaction(long playerId, long oldFactionId, long newFactionId) {

        }
        */

        private void RememberPlayer(long playerId) {
            //Log.Trace("Adding playerId " + playerId, "RememberPlayer");

            /*
            IMyFaction faction = MyAPIGateway.Session.Factions.
                TryGetPlayerFaction(playerId);

            // Is the player solo?
            long factionId;
            if (faction != null) factionId = faction.FactionId;
            else factionId = 0;

            RememberPlayerInFaction(factionId, playerId);
             * */

            if (ActivePlayers.Contains(playerId)) {
                Log.Error("Adding already tracked player " + playerId, "RememberPlayer");
                return;
            }

            ActivePlayers.Add(playerId);
            Log.Trace("Added playerId " + playerId + ", count now " + ActivePlayers.Count, "RememberPlayer");
            UpdateSpawnOwnersNextUpdate = true;
        }

        private void ForgetPlayer(long playerId) {
            /*
            IMyFaction faction = MyAPIGateway.Session.Factions.
                TryGetPlayerFaction(playerId);

            // Is the player solo?
            long factionId;
            if (faction != null) factionId = faction.FactionId;
            else factionId = 0;

            ForgetPlayerInFaction(factionId, playerId);
             * */


            if (!ActivePlayers.Contains(playerId)) {
                Log.Error("Player not tracked " + playerId, "ForgetPlayer");
                return;
            }

            ActivePlayers.Remove(playerId);
            Log.Trace("Forgetting playerId " + playerId + ", count now " + ActivePlayers.Count, "RememberPlayer");
            UpdateSpawnOwnersNextUpdate = true;
        }

        private void RememberObservingEntity(ObservingEntity e) {
            long id = e.EntityId;
            if (ObservingEntities.ContainsKey(id)) {
                Log.Error("Already added " + id, "RememberObservingEntity");
                return;
            }

            Log.Trace("Adding " + id, "RememberObservingEntity");
            ObservingEntities.Add(id, e);
            ObservingTree.Add(e);
            Log.Trace("Finished Adding " + id, "RememberObservingEntity");
        }

        private void UpdateObservingGridPosition(ObservingEntity e) {
            ObservingTree.Move(e);
        }

        private void ForgetObservingEntity(ObservingEntity e) {
            long id = e.EntityId;
            if (!ObservingEntities.ContainsKey(id)) {
                Log.Error("Not stored " + id, "ForgetObservingEntity");
                return;
            }

            Log.Trace("Removing " + id, "ForgetObservingEntity");
            ObservingEntities.Remove(id);
            ObservingTree.Remove(e);
        }

        private void RememberGrid(RevealedGrid e) {
            long id = e.EntityId;
            if (Grids.ContainsKey(id)) {
                Log.Error("Already added " + id, "RememberGrid");
                return;
            }

            Log.Trace("Adding " + id, "RememberGrid");
            Grids.Add(id, e);
            GridTree.Add(e);
        }

        private void UpdateRememberedGridPosition(RevealedGrid e) {
            GridTree.Move(e);
        }

        private void ForgetGrid(RevealedGrid e) {
            long id = e.EntityId;
            if (!Grids.ContainsKey(id)) {
                Log.Error("Not stored " + id, "ForgetGrid");
                return;
            }

            Log.Trace("Removing " + id, "ForgetGrid");
            Grids.Remove(id);
            GridTree.Remove(e);
        }


        #endregion
        #region Public Hooks

        public void ControllableEntityAdded(ControllableEntity e) {
            Log.Trace("Controllable Entity Added", "ControllableEntityAdded");

            //if (e.IsControlled) RememberControlledEntity(e);

            ObservingEntity observer = e as ObservingEntity;
            if (observer != null) RememberObservingEntity(observer);

            RevealedGrid grid = e as RevealedGrid;
            if (grid != null) RememberGrid(grid);

            //Character character = e as Character;
            //if (character != null) RememberCharacter(character);
        }

        public void ControllableEntityMoved(ControllableEntity e) {
            /*
            //Log.Trace("Controllable Entity Moved", "ControllableEntityAdded");
            var notice = new SEGarden.Notifications.AlertNotification() {
                Text = "Controllable Entity " + e.EntityId + " Moved",
                DisplaySeconds = 5
            };
            notice.Raise();
            */

            ObservingEntity observer = e as ObservingEntity;
            if (observer != null) UpdateObservingGridPosition(observer);

            RevealedGrid grid = e as RevealedGrid;
            if (grid != null) UpdateRememberedGridPosition(grid);
        }

        public void ControllableEntityRemoved(ControllableEntity e) {
            Log.Trace("Controllable Entity Removed", "ControllableEntityRemoved");

            //if (e.IsControlled) ForgetControlledEntity(e);

            ObservingEntity observer = e as ObservingEntity;
            if (observer != null) ForgetObservingEntity(observer);

            RevealedGrid grid = e as RevealedGrid;
            if (grid != null) ForgetGrid(grid);

            //Character character = e as Character;
            //if (character != null) ForgetCharacter(character);
        }

        public void ControllableEntityControlled(ControllableEntity e) {
            var notice = new SEGarden.Notifications.AlertNotification() {
                Text = "Controllable Entity " + e.EntityId + " Controlled",
                DisplaySeconds = 5
            };

            notice.Raise();
        }

        public void ControllableEntityReleased(ControllableEntity e) {
            var notice = new SEGarden.Notifications.AlertNotification() {
                Text = "Controllable Entity " + e.EntityId + " Released",
                DisplaySeconds = 5
            };

            notice.Raise();
        }

        public void RevealedEntityAdded(RevealedEntity e) {
            var notice = new SEGarden.Notifications.AlertNotification() {
                Text = "Revealed Entity " + e.EntityId + " Added",
                DisplaySeconds = 5
            };

            notice.Raise();

            MarkNearbyObserversToObserve(e);
        }

        public void RevealedEntityRemoved(RevealedEntity e) {
            var notice = new SEGarden.Notifications.AlertNotification() {
                Text = "Revealed Entity " + e.EntityId + " Removed",
                DisplaySeconds = 5
            };

            notice.Raise();

            MarkNearbyObserversToObserve(e);
        }

        // Observing entities only pick up entity changes when they move.
        // They need to know about add/remove too
        private void MarkNearbyObserversToObserve(ObservableEntity observable) {
            Log.Trace("begin", "MarkNearbyObservingEntitiesForUpdate");

            var viewingSphere = new BoundingSphereD(observable.Position, Settings.Instance.RevealVisibilityMeters);
            List<ObservingEntity> nearbyObserving = ObservingInSphere(viewingSphere);

            if (nearbyObserving.Count == 0) {
                Log.Trace("No nearby observing entities", "MarkNearbyObservingEntitiesForUpdate");
                //Log.Trace("viewingSphere has center " + Position + " and radius " + RevealVisibilityMeters, "MarkNearbyObservingEntitiesForUpdate");
                return;
            }

            Log.Trace("Marking " + nearbyObserving.Count + " nearby observing entities for observe update", "MarkNearbyObservingEntitiesForUpdate");
            foreach (ObservingEntity observing in nearbyObserving) {
                observing.MarkForObservingUpdate();
            }

        }

        /*
        public void ControllableEntityControlled(ControllableEntity e) {
            Log.Trace("Controllable Entity Added", "ControllableEntityAdded");
            RememberControlledEntity(e);
        }

        public void ControllableEntityReleased(ControllableEntity e) {
            Log.Trace("Controllable Entity Added", "ControllableEntityAdded");
            ForgetControlledEntity(e);
        }
        */

        public void PlayerLoggedIn(long playerId, long factionId) {
            Log.Trace("Player " + playerId + " logged in.", "PlayerLoggedIn");
            RememberPlayer(playerId);
            UpdateSpawnOwnersNextUpdate = true;
        }

        public void PlayerChangedFactions(long playerId, long oldFactionId, long newFactionId) {
            UpdateSpawnOwnersNextUpdate = true;
        }

        public void PlayerLoggedOut(long playerId, long factionId) {
            ForgetPlayer(playerId);
            UpdateSpawnOwnersNextUpdate = true;
        }

        #endregion
        #region Updates

        public void UpdateSpawnOwnersIfNeeded() {
            if (UpdateSpawnOwnersNextUpdate) {
                UpdateSpawnOwners();
                UpdateSpawnOwnersNextUpdate = false;
            }
        }

        private void UpdateSpawnOwners() {
            Log.Trace("Updating Spawn Owners", "UpdateSpawnOwners");
            SpawnOwnersNeeded.Clear();

            foreach (long playerId in ActivePlayers) {
                //Log.Trace("Updating for Active player " + playerId, "UpdateSpawnOwners");
                IMyFaction faction = MyAPIGateway.Session.Factions.
                    TryGetPlayerFaction(playerId);

                if (faction != null) {
                    foreach (var kvp in faction.Members) {
                        SpawnOwnersNeeded.Add(kvp.Key);
                    }
                }
                else {
                    SpawnOwnersNeeded.Add(playerId);
                }
            }

            //Log.Trace("Before distinct select: " + 
            //    String.Join(", ", SpawnOwnersNeeded), "UpdateSpawnOwners");

            SpawnOwnersNeeded = SpawnOwnersNeeded.Distinct().ToList();

            Log.Trace("Finished Updating Spawn Owners, new list: " + 
                String.Join(", ", SpawnOwnersNeeded), "UpdateSpawnOwners");

            foreach (RevealedGrid grid in Grids.Values) {
                grid.MarkSpawnUpdateNeeded();
            }

            Concealed.UpdateSpawn();
        }

        #endregion
        #region Marking

        public List<ObservableEntity> ObservableInSphere(BoundingSphereD bounds) {
            var results = new List<ObservableEntity>();
            
            GridTree.GetAllEntitiesInSphere<ObservableEntity>(ref bounds, results);
            /*
            if (results.Count > 0) {
                Log.Trace(results.Count + " observable entities found in sphere " + bounds +
                    " : " + String.Join(", ", results.Select((e) => e.EntityId).ToList()), "ObservableInSphere");
            }
            */
            return results;
        }

        public List<ObservingEntity> ObservingInSphere(BoundingSphereD bounds) {
            var results = new List<ObservingEntity>();
            ObservingTree.GetAllEntitiesInSphere<ObservingEntity>(ref bounds, results);
            if (results.Count > 0) {
                Log.Trace(results.Count + " observing entities found in sphere " + bounds +
                    " : " + String.Join(", ", results.Select((e) => e.EntityId).ToList()), "ObservingInSphere");
            }
            return results;
        }

        /*
        public List<ObservableEntity> GridsInBox(BoundingBoxD bounds) {
            var results = new List<ObservableEntity>();
            GridTree.GetAllEntitiesInBox<RevealedGrid>(ref bounds, results);
            return results;
        }

        public List<ObservableEntity> GridsInSphere(Vector3D center, double Radius) {
            BoundingSphereD sphere2 = new BoundingSphereD()
            var results = new List<ObservableEntity>();
            GridTree.GetAllEntitiesInSphere<RevealedGrid>(ref sphere, results);
            return results;
        }

        public List<ObservableEntity> GridsInSphere(BoundingSphereD sphere) {
            var results = new List<ObservableEntity>();
            GridTree.GetAllEntitiesInSphere<RevealedGrid>(ref sphere, results);
            return results;
        }
         * */

        #endregion

    }

}
