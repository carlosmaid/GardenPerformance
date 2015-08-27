﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SEGarden.Logging;
using SEGarden.Logic;
using SEGarden.Messaging;
using SEGarden.Notifications;

using GP.Concealment.Messages;
using GP.Concealment.Messages.Responses;
using GP.Concealment.Sessions;
using GP.Concealment.World.Entities;





namespace GP.Concealment.MessageHandlers {
    class ClientMessageHandler : MessageHandlerBase {

        private static Logger Log =
            new Logger("GP.Concealment.Messaging.Handlers.ClientMessageHandler");

        private static ClientConcealSession Session {
            get { return ClientConcealSession.Instance; }
        }

        public ClientMessageHandler() : base((ushort)MessageDomain.ConcealClient) { }

        public override void HandleMessage(ushort messageTypeId, byte[] body,
            ulong senderSteamId, RunLocation sourceType) {

            Log.Trace("Received message typeId " + messageTypeId, "HandleMessage");
            MessageType messageType = (MessageType)messageTypeId;
            Log.Trace("Received message type " + messageType, "HandleMessage");

            switch (messageType) {
                case MessageType.ConcealedGridsResponse:
                    ReceiveConcealedGridsResponse(body);
                    break;
                case MessageType.ConcealResponse:
                    ReceiveConcealResponse(body);
                    break;
                case MessageType.RevealedGridsResponse:
                    ReceiveRevealedGridsResponse(body);
                    break;
                case MessageType.RevealResponse:
                    ReceiveRevealResponse(body);
                    break;
                case MessageType.StatusResponse:
                    ReceiveStatusResponse(body);
                    break;
            }

        }

        private void ReceiveConcealedGridsResponse(byte[] body) {
            Log.Trace("Receiving Concealed Grids Response", 
                "ReceiveConcealedGridsResponse");

            ConcealedGridsResponse response = ConcealedGridsResponse.FromBytes(body);

            Session.ConcealedGrids = response.ConcealedGrids;

            String result = "Concealed Grids:\n\n";

            int i = 1;
            foreach (World.Entities.ConcealedGrid grid in Session.ConcealedGrids) {
                result += String.Format("{0}: \"{1}\" - Revealability: {2}\n", 
                    i, grid.DisplayName, grid.Revealability);
                i++;
            }

            Notification notice = new WindowNotification() {
                Text = result,
                BigLabel = "Garden Performance",
                SmallLabel = "Concealed Grids"
            };

            notice.Raise();

        }

        private void ReceiveRevealedGridsResponse(byte[] body) {
            Log.Trace("Receiving Revealed Grids Response",
                "ReceiveRevealedGridsResponse");

            RevealedGridsResponse response = RevealedGridsResponse.FromBytes(body);

            Session.RevealedGrids = response.RevealedGrids;

            String result = Session.RevealedGrids.Count + " Revealed Grids:\n\n";

            int i = 1;
            foreach (RevealedGrid grid in Session.RevealedGrids) {
                result += String.Format("{0}: \"{1}\" - Concealability: {2}\n",
                    i, grid.DisplayName, grid.Concealability);
                i++;
            }

            Notification notice = new WindowNotification() {
                Text = result,
                BigLabel = "Garden Performance",
                SmallLabel = "Revealed Grids"
            };

            notice.Raise();
        }

        private void ReceiveConcealResponse(byte[] body) {
            Log.Trace("Receiving Conceal Response", "ReceiveConcealResponse");

            ConcealResponse response = ConcealResponse.FromBytes(body);

            String result = response.Success ? 
                "Successfully concealed" :
                "Failed to conceal";

            result += " grid " + response.EntityId;

            Notification notice = new ChatNotification() {
                Text = result,
                Sender = "GP"
            };

            notice.Raise();
        }

        private void ReceiveRevealResponse(byte[] body) {
            Log.Trace("Receiving Reveal Response", "ReceiveRevealResponse");

            RevealResponse response = RevealResponse.FromBytes(body);

            String result = response.Success ?
                "Successfully revealed" :
                "Failed to reveal";

            result += " grid " + response.EntityId;

            Notification notice = new ChatNotification() {
                Text = result,
                Sender = "GP"
            };

            notice.Raise();
        }

        private void ReceiveStatusResponse(byte[] body) {
            Log.Trace("Receiving Reveal Response", "ReceiveRevealResponse");

            StatusResponse response = StatusResponse.FromBytes(body);

            String result = "Server is ";
            result += response.ServerRunning ? "Running." : "Terminated.";

            Notification notice = new ChatNotification() {
                Text = result,
                Sender = "GP"
            };

            notice.Raise();
        }

    }
}
