import { createServer } from "http";
import express from "express";
import cors from "cors";
import { Server } from "colyseus";
import { WebSocketTransport } from "@colyseus/ws-transport";
import { DreamRoom } from "./rooms/DreamRoom";

const port = Number(process.env.PORT) || 2567;
const app = express();
app.use(cors());
app.use(express.json());
app.get("/", (_req, res) => {
  res.json({ ok: true, name: "D.R.E.A.M. server", rooms: ["dream_room"] });
});

const httpServer = createServer(app);
const gameServer = new Server({
  transport: new WebSocketTransport({ server: httpServer }),
});

gameServer.define("dream_room", DreamRoom);

gameServer.listen(port).then(() => {
  // eslint-disable-next-line no-console
  console.log(`D.R.E.A.M. server listening on ws://localhost:${port}`);
});
