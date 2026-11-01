import ModalOverlay from "./ModalOverlay";
import StudentPhaseCard from "../StudentPhaseCard";

const StudentPhaseOverlay = ({ onClose }: { onClose: () => void }) => (
  <ModalOverlay onClose={onClose}>
    <StudentPhaseCard readOnly={true} />
  </ModalOverlay>
);

export default StudentPhaseOverlay;
