export interface RoadSegment {
  id: number;
  name: string;
  corridorName: string | null;
  geometry: string;
  lengthMeters: number | null;
}
