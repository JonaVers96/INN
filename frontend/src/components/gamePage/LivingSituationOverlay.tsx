import ModalOverlay from "./ModalOverlay";
import LivingSituationCard from "../LivingSituationCard";

const LivingSituationOverlay = ({ onClose }: { onClose: () => void }) => (
  <ModalOverlay onClose={onClose}>
    <LivingSituationCard />
  </ModalOverlay>
);

export default LivingSituationOverlay;
