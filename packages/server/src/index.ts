import { createServer } from "http";
import express from "express";
import cors from "cors";
import { Server } from "colyseus";
import { WebSocketTransport } from "@colyseus/ws-transport";
import { DreamRoom } from "./rooms/DreamRoom";
import { profileRoutes } from "./profile/routes";
import { profileStore } from "./profile/ProfileStore";

const port = Number(process.env.PORT) || 2567;
const app = express();
app.use(cors());
app.use(express.json());
app.get("/", (_req, res) => {
  res.json({ ok: true, name: "D.R.E.A.M. server", rooms: ["dream_room"] });
});
app.use(profileRoutes());

const httpServer = createServer(app);
const gameServer = new Server({
  transport: new WebSocketTransport({ server: httpServer }),
});

gameServer.define("dream_room", DreamRoom);

gameServer.listen(port).then(() => {
  // eslint-disable-next-line no-console
  console.log(`D.R.E.A.M. server listening on ws://localhost:${port}`);
});

for (const signal of ["SIGINT", "SIGTERM"] as const) {
  process.on(signal, () => {
    profileStore.flushNow();
    process.exit(0);
  });
}
