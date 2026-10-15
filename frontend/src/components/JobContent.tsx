import { useTranslation } from "react-i18next";
import JobWheel from "./JobWheel";
import ResultPane from "./ResultPane";
import { buildJobs } from "../utils/buildJobs";
import { useJobWheel } from "../hooks/useJobWheel";
import "../styles/General.css";

// The possible jobs are static mock data, so we build them once
// when the file loads instead of on every render.
const jobs = buildJobs();

/**
 * Page that shows a wheel with the possible jobs.
 * The player spins the wheel and the selected job opens in a result pane.
 */
const JobPage = () => {
  const { t } = useTranslation();

  // All spin and result-pane logic lives in this hook:
  // mustSpin / prizeIndex -> control the wheel
  // selectedJob / showPane -> control the result pane
  // spin / handleStop / close -> actions triggered by the UI
  const wheel = useJobWheel(jobs);

  // Wheel segments: one translated job name per segment
  const data = jobs.map((job) => ({ option: t(job.nameKey) }));

  return (
    <div className="job-page-container">
      <h1 className="h1">{t("profile_labels.possible_jobs")}</h1>

      {/* The wheel itself. handleStop runs when it stops spinning
          and opens the result pane for the winning job. */}
      <JobWheel
        data={data}
        mustSpin={wheel.mustSpin}
        prizeIndex={wheel.prizeIndex}
        onStop={wheel.handleStop}
      />

      {/* Starts the spin. In this mock version it always lands on job 2. */}
      <button className="button" onClick={wheel.spin}>
        Spin
      </button>

      {/* Shows the details of the selected job once the wheel has stopped */}
      <ResultPane
        job={wheel.selectedJob}
        isOpen={wheel.showPane}
        onClose={wheel.close}
        t={t}
      />
    </div>
  );
};

export default JobPage;