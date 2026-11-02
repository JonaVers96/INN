import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import axios from "axios";
import { useSignalR } from "./useSignalR";

export const usePlayerLanguage = () => {
  const { i18n } = useTranslation();
  const connection = useSignalR();

  // Bij laden, refresh of als laatkomer: de huidige taal van de kamer ophalen
  useEffect(() => {
    const roomCode = localStorage.getItem("roomCode");
    if (!roomCode) return;

    axios
      .get(`${import.meta.env.VITE_API_BASE_URL}/api/game/${roomCode}`)
      .then((response) => {
        if (response.data.language) {
          i18n.changeLanguage(response.data.language);
        }
      })
      .catch((error) => console.error("Fout bij ophalen van de taal:", error));
  }, [i18n]);

  // Live wisselen wanneer de supervisor een andere taal kiest
  useEffect(() => {
    if (!connection) return;

    const handleLanguageChanged = (language: string) => {
      i18n.changeLanguage(language);
    };

    connection.on("LanguageChanged", handleLanguageChanged);

    return () => {
      connection.off("LanguageChanged", handleLanguageChanged);
    };
  }, [connection, i18n]);
};
