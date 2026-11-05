import { useEffect, useState } from "react";
import { Navigate, Outlet } from "react-router-dom";
import axios from "axios";

type Status = "checking" | "valid" | "invalid";

const SESSION_KEYS = ["playerId", "roomCode", "nickName", "tableNumber"];

export default function RequirePlayerSession() {
  const playerId = localStorage.getItem("playerId");
  const roomCode = localStorage.getItem("roomCode");

  const [status, setStatus] = useState<Status>(
    playerId && roomCode ? "checking" : "invalid",
  );

  useEffect(() => {
    if (!playerId || !roomCode) return;

    let cancelled = false;

    axios
      .get(`${import.meta.env.VITE_API_BASE_URL}/api/game/${roomCode}`)
      .then((response) => {
        if (cancelled) return;
        const playerExists = response.data.players?.some(
          (p: { playerId: string }) => p.playerId === playerId,
        );
        if (!playerExists) {
          SESSION_KEYS.forEach((key) => localStorage.removeItem(key));
        }
        setStatus(playerExists ? "valid" : "invalid");
      })
      .catch((error) => {
        if (cancelled) return;
        // Kamer bestaat niet meer (404): oude sessie opruimen
        if (axios.isAxiosError(error) && error.response?.status === 404) {
          SESSION_KEYS.forEach((key) => localStorage.removeItem(key));
        }
        setStatus("invalid");
      });

    return () => {
      cancelled = true;
    };
  }, [playerId, roomCode]);

  if (status === "checking") return null;
  if (status === "invalid") return <Navigate to="/home" replace />;

  return <Outlet />;
}