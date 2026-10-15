import { useState } from "react";
import type { Job } from "../types/jobTypes";

const FORCED_PRIZE_INDEX = 1; // mock: always land on job2

const useSpinState = () => {
  const [mustSpin, setMustSpin] = useState(false);
  const [prizeIndex, setPrizeIndex] = useState(0);

  const spin = () => {
    setPrizeIndex(FORCED_PRIZE_INDEX);
    setMustSpin(true);
  };
  const stop = () => setMustSpin(false);

  return { mustSpin, prizeIndex, spin, stop };
};

const useResultPane = () => {
  const [selectedJob, setSelectedJob] = useState<Job | null>(null);
  const [showPane, setShowPane] = useState(false);

  const open = (job: Job) => {
    setSelectedJob(job);
    setShowPane(true);
  };
  const close = () => setShowPane(false);

  return { selectedJob, showPane, open, close };
};

export const useJobWheel = (jobs: Job[]) => {
  const spinState = useSpinState();
  const pane = useResultPane();

  const handleStop = () => {
    spinState.stop();
    pane.open(jobs[spinState.prizeIndex]);
  };

  return { ...spinState, ...pane, handleStop };
};