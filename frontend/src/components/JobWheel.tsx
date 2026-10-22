import { Wheel } from "react-custom-roulette-r19";

const WHEEL_COLORS = ["#f9bc2f", "#f08666", "#11b0a5"];

type JobWheelProps = {
  data: { option: string }[];
  mustSpin: boolean;
  prizeIndex: number;
  onStop: () => void;
};

const JobWheel = ({ data, mustSpin, prizeIndex, onStop }: JobWheelProps) => (
  <Wheel
    mustStartSpinning={mustSpin}
    prizeNumber={prizeIndex}
    data={data}
    backgroundColors={WHEEL_COLORS}
    textColors={["#000000"]}
    perpendicularText={true}
    onStopSpinning={onStop}
  />
);

export default JobWheel;