import { createContext } from "react";
import type { HubConnection } from "@microsoft/signalr";

export const SignalRContext = createContext<HubConnection | null>(null);
