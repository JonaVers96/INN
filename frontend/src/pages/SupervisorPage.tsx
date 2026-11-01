import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import axios from "axios";
import { useSignalR } from "../hooks/useSignalR";
import type { JoinTableResponse, LeaveTableResponse } from "../types/Reponses";
import type { Player } from "../types/Models";
import TableGrid from "../components/TableGrid";
import { FaBars, FaTimes } from "react-icons/fa";

const supervisorId = "18822955-30ec-4456-96f4-f7539a297ad1";
const SupervisorPage = () => {
  const { t } = useTranslation();
  const connection = useSignalR();

  const [roomCode, setRoomCode] = useState<string | null>(null);
  const [players, setPlayers] = useState<Player[]>([]);
  const [isStarting, setIsStarting] = useState(false);
  const [isGameStarted, setIsGameStarted] = useState(false);
  const [isClosing, setIsClosing] = useState(false);
  const [tableCount, setTableCount] = useState(0);
  const [isAddingTable, setIsAddingTable] = useState(false);
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [skipChecks, setSkipChecks] = useState(false);
  const [isSavingSkipChecks, setIsSavingSkipChecks] = useState(false);
  
  useEffect(() => {
    axios
      .get(
        `${import.meta.env.VITE_API_BASE_URL}/api/supervisor/${supervisorId}/skip-checks`,
      )
      .then((response) => setSkipChecks(response.data.skipChecks))
      .catch((error) =>
        console.error("Fout bij ophalen van de controle-instelling:", error),
      );
  }, []);

  useEffect(() => {
    const restoreSession = async () => {
      const savedRoomCode = localStorage.getItem("supervisorRoomCode");

      if (savedRoomCode) {
        try {
          // 1. Haal ALTIJD eerst de data op via de REST API (UI herstellen)
          const response = await axios.get(
            `${import.meta.env.VITE_API_BASE_URL}/api/game/${savedRoomCode}`,
          );
          const roomState = response.data;

          setRoomCode(roomState.roomCode);
          setIsGameStarted(roomState.isActive);
          setPlayers(roomState.players);
          setTableCount(roomState.tableCount);

          if (connection) {
            if (connection.state === "Connected") {
              await connection.invoke("SubscribeToRoom", savedRoomCode);
            } else {
              setTimeout(() => {
                if (connection.state === "Connected") {
                  connection
                    .invoke("SubscribeToRoom", savedRoomCode)
                    .catch(console.error);
                }
              }, 500);
            }
          }
        } catch (error: unknown) {
          console.error("Error restoring room:", error);

          // Gebruik de veilige Axios type guard in plaats van 'any'
          if (axios.isAxiosError(error) && error.response?.status === 404) {
            localStorage.removeItem("supervisorRoomCode");
            setRoomCode(null);
          }
        }
      }
    };

    restoreSession();
  }, [connection]);

  useEffect(() => {
    if (!connection) return;

    connection.on("PlayerJoined", (data: JoinTableResponse) => {
      setPlayers((prevPlayers) => {
        if (prevPlayers.some((p) => p.playerId === data.playerId))
          return prevPlayers;
        return [
          ...prevPlayers,
          {
            playerId: data.playerId,
            nickName: data.nickName,
            tableNumber: data.tableNumber,
            isOffline: false,
            currentPhase: data.currentPhase || "waiting",
          },
        ];
      });
    });

    connection.on(
      "PlayerPhaseUpdated",
      (data: { playerId: string; newPhase: string }) => {
        setPlayers((prevPlayers) =>
          prevPlayers.map((p) =>
            p.playerId === data.playerId
              ? { ...p, currentPhase: data.newPhase }
              : p,
          ),
        );
      },
    );

    connection.on("PlayerDisconnected", (data: { playerId: string }) => {
      setPlayers((prevPlayers) =>
        prevPlayers.map((p) =>
          p.playerId === data.playerId ? { ...p, isOffline: true } : p,
        ),
      );
    });

    connection.on("PlayerReconnected", (data: { playerId: string }) => {
      console.log(`Speler met ID ${data.playerId} is succesvol ge-reconnect!`);
      setPlayers((prevPlayers) =>
        prevPlayers.map((p) =>
          p.playerId === data.playerId ? { ...p, isOffline: false } : p,
        ),
      );
    });

    connection.on("PlayerLeft", (data: LeaveTableResponse) => {
      setPlayers((prevPlayers) =>
        prevPlayers.filter((p) => p.playerId !== data.playerId),
      );
    });

    return () => {
      connection.off("PlayerJoined");
      connection.off("PlayerLeft");
      connection.off("PlayerDisconnected");
      connection.off("PlayerReconnected");
      connection.off("PlayerPhaseUpdated");
    };
  }, [connection]);

    const handleToggleSkipChecks = async () => {
    if (isSavingSkipChecks) return;

    const newValue = !skipChecks;
    setIsSavingSkipChecks(true);
    try {
      const response = await axios.put(
        `${import.meta.env.VITE_API_BASE_URL}/api/supervisor/${supervisorId}/skip-checks`,
        newValue,
        { headers: { "Content-Type": "application/json" } },
      );
      setSkipChecks(response.data.skipChecks);
    } catch (error) {
      console.error("Fout bij wijzigen van de controle-instelling:", error);
    } finally {
      setIsSavingSkipChecks(false);
    }
  };

  const handleCreateRoom = async () => {
    try {
      const response = await axios.post(
        `${import.meta.env.VITE_API_BASE_URL}/api/game/create`,
        { supervisorId },
      );
      const code = response.data.roomCode;

      setRoomCode(code);
      setTableCount(response.data.tableCount);
      // Sla code op voor bij browser refresh
      localStorage.setItem("supervisorRoomCode", code);

      if (connection) {
        await connection.invoke("SubscribeToRoom", code);
      }
    } catch (error) {
      console.error("Error creating room:", error);
    }
  };

  const handleCloseRoom = async () => {
    if (!roomCode) return;

    const confirmed = window.confirm(
      t(
        "supervisor_page.close_room_confirm",
        "Weet je zeker dat je de kamer wil sluiten? Alle spelers worden terugestuurd naar het startscherm.",
      ),
    );
    if (!confirmed) return;

    setIsClosing(true);
    try {
      await axios.delete(
        `${import.meta.env.VITE_API_BASE_URL}/api/game/${roomCode}`,
      );
      localStorage.removeItem("supervisorRoomCode");
      setRoomCode(null);
      setPlayers([]);
      setIsGameStarted(false);
      setIsSettingsOpen(false);
    } catch (error) {
      console.error("Fout bij het sluiten van de kamer:", error);
    } finally {
      setIsClosing(false);
    }
  };

  const handleStartGame = async () => {
    if (!roomCode) return;

    setIsStarting(true);
    try {
      await axios.post(
        `${import.meta.env.VITE_API_BASE_URL}/api/game/${roomCode}/start`,
      );
      console.log("Startsignaal succesvol naar de klas gestuurd!");
      setIsGameStarted(true);
      setPlayers((prevPlayers) =>
        prevPlayers.map((p) => ({ ...p, currentPhase: "student-phase" })),
      );
    } catch (error) {
      console.error("Fout bij het starten van de kamer:", error);
    } finally {
      setIsStarting(false);
    }
  };

    const handleAddTable = async () => {
    if (!roomCode || isAddingTable) return;

    setIsAddingTable(true);
    try {
      const response = await axios.post(
        `${import.meta.env.VITE_API_BASE_URL}/api/game/${roomCode}/tables`,
      );
      setTableCount(response.data.tableCount);
    } catch (error) {
      console.error("Fout bij het toevoegen van een tafel:", error);
    } finally {
      setIsAddingTable(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-100 p-6 md:p-10 font-sans">
      <div className="max-w-4xl mx-auto bg-white rounded-[2rem] shadow-sm border-2 border-teal-50 p-8 mb-10 text-center">
        <h1 className="text-4xl md:text-5xl font-black text-teal-800 mb-8 tracking-tight">
          LIV<span className="text-amber-500">€</span> YOUR LIF
          <span className="text-amber-500">€</span>
          <span className="block text-2xl text-teal-600 mt-2 font-bold uppercase tracking-widest">
            {t("supervisor_page.title", "Dashboard")}
          </span>
        </h1>

        {!roomCode ? (
          <button
            className="bg-teal-500 hover:bg-teal-600 text-white font-extrabold py-4 px-10 rounded-full shadow-lg shadow-teal-500/30 transition-transform hover:scale-105 active:scale-95 text-lg uppercase tracking-wider"
            onClick={handleCreateRoom}
          >
            {t("supervisor_page.create_room_button", "Maak een kamer aan")}
          </button>
        ) : (
          <div className="flex flex-col items-center gap-6">
            <div className="inline-flex flex-col items-center justify-center bg-teal-50 border-2 border-teal-100 rounded-3xl py-4 px-10 shadow-inner">
              <span className="text-teal-600 font-bold text-sm uppercase tracking-widest mb-1">
                {t("supervisor_page.room_code_label", "Jouw Kamer Code")}
              </span>
              <span className="text-teal-800 font-black text-6xl tracking-widest drop-shadow-sm">
                {roomCode}
              </span>
            </div>

            {isGameStarted ? (
              <div className="bg-teal-100 text-teal-800 font-black py-4 px-10 rounded-full shadow-inner border-2 border-teal-200 text-lg uppercase tracking-wider flex items-center gap-3">
                <span className="relative flex h-3 w-3">
                  <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-teal-400 opacity-75"></span>
                  <span className="relative inline-flex rounded-full h-3 w-3 bg-teal-500"></span>
                </span>
                {t("supervisor_page.in_progress", "SPEL IS BEZIG")}
              </div>
            ) : (
              <button
                onClick={handleStartGame}
                disabled={isStarting || players.length === 0}
                className={`font-extrabold py-4 px-10 rounded-full shadow-lg transition-transform text-lg uppercase tracking-wider ${
                  players.length === 0 || isStarting
                    ? "bg-slate-300 text-slate-500 cursor-not-allowed"
                    : "bg-amber-500 hover:bg-amber-600 text-white hover:scale-105 active:scale-95 shadow-amber-500/30"
                }`}
              >
                {isStarting
                  ? t("supervisor_page.starting", "Starten...")
                  : t("supervisor_page.start_game", "Start het Spel!")}
              </button>
            )}

            {!isGameStarted && players.length === 0 && (
              <p className="text-sm text-slate-400 font-medium italic">
                {t(
                  "supervisor_page.waiting_for_players",
                  "Wacht op spelers om te kunnen starten...",
                )}
              </p>
            )}

          </div>
        )}
      </div>

      <button
        onClick={() => setIsSettingsOpen(true)}
        aria-label={t("supervisor_page.settings", "Instellingen")}
        className="fixed top-6 left-6 z-30 flex items-center gap-2 bg-white text-teal-700 font-extrabold py-3 px-5 rounded-full shadow-md border-2 border-teal-100 hover:bg-teal-50 transition-colors text-sm uppercase tracking-wider"
      >
        <FaBars />
        {t("supervisor_page.settings", "Instellingen")}
      </button>

      {isSettingsOpen && (
        <div
          className="fixed inset-0 z-40 bg-slate-900/40"
          onClick={() => setIsSettingsOpen(false)}
        />
      )}

      <aside
        className={`fixed top-0 left-0 z-50 h-full w-80 max-w-[85vw] bg-white shadow-2xl p-6 flex flex-col gap-6 transition-transform duration-300 ${
          isSettingsOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-black text-teal-800 uppercase tracking-wider">
            {t("supervisor_page.settings", "Instellingen")}
          </h2>
          <button
            onClick={() => setIsSettingsOpen(false)}
            aria-label={t("supervisor_page.close_settings", "Sluiten")}
            className="text-slate-400 hover:text-slate-600 text-xl"
          >
            <FaTimes />
          </button>
        </div>

        <label className="flex items-center justify-between gap-4 cursor-pointer">
          <span className="flex flex-col">
            <span className="font-bold text-slate-700">
              {t("supervisor_page.skip_checks", "Controles uitschakelen")}
            </span>
            <span className="text-xs text-slate-400">
              {t(
                "supervisor_page.skip_checks_info",
                "Spelers kunnen alle getallen invullen zonder dat ze gecontroleerd worden.",
              )}
            </span>
          </span>
          <button
            type="button"
            role="switch"
            aria-checked={skipChecks}
            onClick={handleToggleSkipChecks}
            disabled={isSavingSkipChecks}
            className={`relative inline-flex h-7 w-12 shrink-0 rounded-full transition-colors disabled:opacity-50 ${
              skipChecks ? "bg-amber-500" : "bg-slate-300"
            }`}
          >
            <span
              className={`absolute top-1 left-1 h-5 w-5 rounded-full bg-white shadow transition-transform ${
                skipChecks ? "translate-x-5" : "translate-x-0"
              }`}
            />
          </button>
        </label>

        {roomCode && (
          <div className="flex flex-col gap-3 border-t border-slate-100 pt-6">
            <button
              onClick={handleAddTable}
              disabled={isAddingTable}
              className="font-extrabold py-3 px-8 rounded-full border-2 border-teal-300 text-teal-600 hover:bg-teal-500 hover:text-white hover:border-teal-500 transition-all duration-200 text-sm uppercase tracking-wider disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isAddingTable
                ? t("supervisor_page.adding_table", "Tafel toevoegen...")
                : t("supervisor_page.add_table_button", "Tafel toevoegen")}
            </button>

            <button
              onClick={handleCloseRoom}
              disabled={isClosing}
              className="font-extrabold py-3 px-8 rounded-full border-2 border-rose-300 text-rose-500 hover:bg-rose-500 hover:text-white hover:border-rose-500 transition-all duration-200 text-sm uppercase tracking-wider disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isClosing
                ? t("supervisor_page.closing", "Sluiten...")
                : t("supervisor_page.close_room_button", "Kamer sluiten")}
            </button>
          </div>
        )}
      </aside>

      {roomCode && (
        <div className="max-w-[90rem] mx-auto">
                    <TableGrid players={players} tableCount={tableCount} />
        </div>
      )}
    </div>
  );
};

export default SupervisorPage;
