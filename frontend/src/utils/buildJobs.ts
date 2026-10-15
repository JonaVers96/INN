import mockJobData from "../assets/mockJobData.json";
import type { Job } from "../types/jobTypes";

const MOCK_JOBS = [mockJobData[0].job1, mockJobData[0].job2, mockJobData[0].job3];

export const buildJobs = (): Job[] =>
  MOCK_JOBS.map((job, index) => ({
    id: `${index + 1}`,
    nameKey: job.nameKey,
    firstSalaryGrossKey: 0,
    firstSalaryNetKey: 0,
    secondSalaryGrossKey: 0,
    secondSalaryNetKey: 0,
    benefitsKey: job.nameKey,
    pensionKey: 0,
  }));