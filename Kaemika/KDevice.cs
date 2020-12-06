using System;
using System.Collections.Generic;
using System.Threading;
using SkiaSharp;
using GraphSharp;

namespace Kaemika {

    public interface DevicePainter : Painter { // interface for platform-dependent device rendering
        void Draw(KDeviceHandler.KDevice device, int canvasX, int canvasY, int canvasWidth, int canvasHeight);
    }

    // Platform-independent handler for the unique Device instance
    public abstract class KDeviceHandler {

        private static KDevice device; // <<============== the only KDevice

        public enum Direction { Lft=0, Top=1, Rht=2, Bot=3 };
        // indexed by Direction:
        public static Direction[] opposite = new Direction[4] { Direction.Rht, Direction.Bot, Direction.Lft, Direction.Top };

        public enum Animation { PullLft, PullTop, PullRht, PullBot, SplitLft, SplitTop, SplitRht, SplitBot, SizeHalf, SizeQuarter, None };
        // indexed by Direction:
        public static Animation[] phase0 = new Animation[4] { Animation.PullLft, Animation.PullTop, Animation.PullRht, Animation.PullBot };
        public static Animation[] phase1 = new Animation[4] { Animation.SplitLft, Animation.SplitTop, Animation.SplitRht, Animation.SplitBot };
        public static Animation[] phase2 = new Animation[4] { Animation.PullRht, Animation.PullBot, Animation.PullLft, Animation.PullTop };
        public static Animation[] phase3 = new Animation[4] { Animation.None, Animation.None, Animation.None, Animation.None };

        public static SKColor dropletColorRed = new SKColor(255, 0, 0, 191);
        public static SKColor dropletColorPurple = new SKColor(127, 0, 127, 191);
        public static SKColor dropletColorGrey = new SKColor(127, 127, 127, 191);

        public static string coldTemp = "20C"; static double coldTemperature = 293; // 19.85C to account for roundings
        public static string hotTemp = "40C"; static double hotTemperature = 313; // 39.85C to account for roundings
        public static SKColor deviceBackColor = SKColors.NavajoWhite;

        public static bool Exists() {
            return device != null;
        }
        public static void Start(int phaseDelay, int stepDelay) {
            device = new KDevice(phaseDelay, stepDelay);
            KGui.gui.GuiDeviceUpdate();
        }
        public static void Stop() {
            device = null;
        }
        //public static Swipe PinchPan() {
        //    if (device == null) return Swipe.Id();
        //    return device.pinchPan;
        //}
        public static void SetPinchPan(Swipe pinchPan) {
            if (device == null) return;
            lock (device) { device.sizeChanged = true; } // force redrawing of cached background
            device.pinchPan = pinchPan;
        }
        //public static void ResetPinchPan() {
        //    if (device == null) return;
        //    lock (device) { device.sizeChanged = true; } // force redrawing of cached background
        //    device.pinchPan = (Swipe.Same(device.pinchPan, Swipe.Id())) ? new Swipe(2.0f, new SKPoint(0, 0)) : Swipe.Id();
        //}
        //public static void SetPinchOrigin(SKPoint pinchOrigin) {
        //    if (device == null) return;
        //    device.pinchOrigin = pinchOrigin;
        //}
        //public static void DisplayPinchOrigin(bool displayPinchOrigin) {
        //    if (device == null) return;
        //    device.displayPinchOrigin = displayPinchOrigin;
        //}

        //STATIC ENTRY POINT FOR DRAWING THE GLOBAL DEVICE WITH A DEVICE PAINTER THAT CONTAINS A CANVAS:

        public static void Draw(DevicePainter painter, int canvasX, int canvasY, int canvasWidth, int canvasHeight) {
            if (device == null) return;
            painter.Draw(device, canvasX, canvasY, canvasWidth, canvasHeight);
        }

        public static void Clear(Style style) {
            if (device == null || !style.chartOutput) return;
            device = new KDevice(device.phaseDelay, device.stepDelay);
            KGui.gui.GuiDeviceUpdate();
        }

        public static void Sample(SampleValue sample, Style style) {
            if (device == null || (!style.chartOutput) || Exec.IsVesselVariant(sample)) return;
            device.Sample(sample, style);
        }
        public static void Amount(SampleValue sample, AmountEntry amountEntry, Style style) {
            if (device == null || (!style.chartOutput) || Exec.IsVesselVariant(sample)) return;
        }
        public static void Mix(SampleValue outSample, List<SampleValue> inSamples, Style style) {
            if (device == null || (!style.chartOutput) || Exec.IsVesselVariant(outSample)) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            device.Mix(outSample, inSamples, style);
        }
        public static void Split(List<SampleValue> outSamples, SampleValue inSample, Style style) {
            if (device == null || (!style.chartOutput) || Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            device.Split(outSamples, inSample, style);
        }
        public static void Dispose(List<SampleValue> samples, Style style) {
            if (device == null || !style.chartOutput) return;
            foreach (SampleValue sample in samples) if (Exec.IsVesselVariant(sample)) return;
            device.Dispose(samples, style);
        }
        public static void Regulate(List<SampleValue> outSamples, List<SampleValue> inSamples, Style style) {
            if (device == null || !style.chartOutput) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            device.Regulate(outSamples, inSamples, style);
        }
        public static void Concentrate(List<SampleValue> outSamples, List<SampleValue> inSamples, Style style) {
            if (device == null || !style.chartOutput) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            device.Concentrate(outSamples, inSamples, style);
        }
        public static List<Place> StartEquilibrate(List<SampleValue> inSamples, double fortime, Style style) {
            if (device == null || !style.chartOutput) return null;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return null;
            return device.StartEquilibrate(inSamples, fortime, style);
        }
        public static void EndEquilibrate(List<Place> goBacks, List<SampleValue> outSamples, List<SampleValue> inSamples, double fortime, Style style) {
            if (device == null || !style.chartOutput) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            Thread.Sleep(device.stepDelay);
            device.EndEquilibrate(goBacks, outSamples, inSamples, fortime, style);
        }

        public class KDevice {
            public Place[,] places; // rows, columns   // PROTECT concurrent access BY LOCKING ProtocolDevice.Device
            public Placement placement;                // PROTECT concurrent access BY LOCKING ProtocolDevice.Device
            private Dictionary<string, int> zone;
            // for rendering:
            public bool sizeChanged; // to force redrawing of cached background, lock protected
            public SKColor dropletColor;
            public SKColor dropletBiggieColor;
            public int rowsNo; // >= Places.GetLength(0), visible pads
            public int colsNo; // >= Places.GetLength(1), visible pads
            public int coldZoneWidth;
            public int warmZoneWidth;
            public int hotZoneWidth;
            public int phaseDelay;
            public int stepDelay;

            public Swipe pinchPan = Swipe.Id();
            public bool displayPinchOrigin = false;
            public SKPoint pinchOrigin;

            public KDevice(int phaseDelay, int stepDelay) {
                this.places = new Place[0, 0];
                this.placement = new Placement();
                this.zone = new Dictionary<string, int> { { "staging", 0 }, { "mixing", 2 }, { "warm", 12 }, { "hot", 18 } };
                // for rendering:
                this.sizeChanged = true;
                this.dropletColor = dropletColorRed;
                this.dropletBiggieColor = dropletColorPurple;
                this.rowsNo = 11; // pads
                this.colsNo = 21; // pads
                this.coldZoneWidth = 8; // pads
                this.warmZoneWidth = 8; // pads
                this.hotZoneWidth = 5; // pads
                this.phaseDelay = phaseDelay;
                this.stepDelay = stepDelay;
            }
            
            // makes sure that the device fits both in width and height
            public (float fittedMargin, float fittedPadRadius, float fittedWidth, float fittedHeight) FittedDimensions(int canvasWidth, int canvasHeight) {
                float width = canvasWidth; // int to float
                float padRadius = width / (2 * (colsNo + 1)); // + 1 for margin = padRadius
                float margin = padRadius;
                float deviceWidth = canvasWidth;
                float deviceHeight = 2 * padRadius * rowsNo + 2 * margin;

                float widthRatio = 1.0F; // canvasWidth / deviceWidth
                float heightRatio =  ((float)canvasHeight) / deviceHeight;
                float ratio = Math.Min(widthRatio, heightRatio);
                int fittedWidth = (int)(ratio * deviceWidth);
                int fittedHeight = (int)(ratio * deviceHeight);
                float fittedMargin = ratio * margin;
                float fittedPadRadius = ratio * padRadius;
                return (fittedMargin, fittedPadRadius, fittedWidth, fittedHeight);
            }

            private void GrowToSize(int rows, int columns) {
                lock (this) { // i.e. lock(ProtocolDevice.device)
                    Place[,] newPlaces = new Place[rows, columns];
                    for (int i = 0; i < rows; i++) {
                        for (int j = 0; j < columns; j++)
                            if (i < places.GetLength(0) && j < places.GetLength(1))
                                newPlaces[i, j] = this.places[i, j];
                            else
                                newPlaces[i, j] = new Place(i, j);
                    }
                    this.places = newPlaces;
                    this.rowsNo = Math.Max(this.rowsNo, rows);
                    this.colsNo = Math.Max(this.colsNo, columns);
                    this.sizeChanged = true;
                }
                KGui.gui.GuiDeviceUpdate(); // outside of lock, or it will deadlock
            }
            private void StretchRowsToSize(int i) {
                if (i > places.GetLength(0)) GrowToSize(i, places.GetLength(1));
            }
            private void StretchColumnsToSize(int j) {
                if (j > places.GetLength(1)) GrowToSize(places.GetLength(0), j);
            }

            public Place PlaceAt(int row, int column) {
                return places[row,column]; // could be null
            }
            public Place ReservePlaceInColumn(int j) {
                Place place = FreePlaceInColumn(j, minRow: 0, currentlyAt: null, skipNo: 0);
                place.reserved = true;
                return place;
            }
            public Place FreePlaceInColumn(int column, int minRow, Place currentlyAt, int skipNo) {
                int skip = skipNo;
                int spacing = 2; // ignore alternate rows
                StretchColumnsToSize(column + 1);
                StretchRowsToSize(minRow + 1);
                for (int row = 0; row < places.GetLength(0); row = row + spacing) {
                    if (row >= minRow) {
                        Place place = places[row, column];
                        if ((place != null) && ((place == currentlyAt) || (!place.reserved) && !(placement.IsOccupied(place)))) {
                            if (skip == 0) return place;
                            else skip--;
                        }
                    }
                }
                StretchRowsToSize(places.GetLength(0) + spacing);
                return FreePlaceInColumn(column, minRow, currentlyAt, skipNo);
            }
            public Place[,] FreeAreaInColumn(int column, int minRow, int rows, int cols) {
                // find a free contiguous rectangle of places with 'column','fromRow' on the top left or below
                int spacing = 2; // skip alternate rows
                StretchColumnsToSize(column + cols);
                StretchRowsToSize(minRow + rows);
                Place[,] area = new Place[rows, cols];
                for (int row = 0; row < places.GetLength(0) && row + 1 < places.GetLength(0); row = row + spacing) {
                    if (row >= minRow) {
                        StretchRowsToSize(row + rows);
                        bool complete = true;
                        for (int i = 0; i < rows; i++) {
                            for (int j = 0; j < cols; j++) {
                                Place place = this.places[row + i, column + j];
                                if (place == null || place.reserved || placement.IsOccupied(place)) complete = false;
                                else area[i, j] = place;
                            }
                        }
                        if (complete) return area;
                    }
                }
                StretchRowsToSize(places.GetLength(0) + spacing);
                return FreeAreaInColumn(column, minRow, rows, cols);
            }

            // === DEVICE PROTOCOL OPERATIONS === //

            public void Sample(SampleValue sample, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                //Gui.Log("Device: NEW DROPLET " + sample.FormatSymbol(style));
                Place source = ReservePlaceInColumn(zone["staging"]);
                placement.Appear(sample, source, style);
                //placement.Place(sample, source, style);
                //KGui.gui.GuiDeviceUpdate();
            }
            public void Mix(SampleValue outSample, List<SampleValue> inSamples, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                //Gui.Log("Device: MIX " + outSample.FormatSymbol(style) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)));
                Place[,] area = FreeAreaInColumn(zone["mixing"], minRow: placement.PlaceOf(inSamples[0], style).Row(), rows: 1, cols: 2*inSamples.Count-1); // find free columns in "mixing" block, on the same row

                List<Place>[] routes = new List<Place>[inSamples.Count];
                for (int i = 0; i < inSamples.Count; i++) 
                    routes[i] = placement.PathHorFirst(placement.PlaceOf(inSamples[i], style), area[0, 2 * i]);
                placement.FollowRoutes(routes, clearance: 1, style);

                for (int i = 1; i < inSamples.Count; i++) {
                    placement.FollowRoute(placement.PathHorFirst(placement.PlaceOf(inSamples[i], style), area[0, 1]), clearance: 0, style);
                    SampleValue accumulator = placement.SampleOf(area[0, 0]);
                    SampleValue tempSample = new SampleValue(outSample.symbol, null, new NumberValue(accumulator.Volume() + inSamples[i].Volume()), new NumberValue(1), true);
                    placement.MixHor(tempSample, area[0, 0], area[0, 1], style);
                }
                placement.Clear(area[0, 0], style, log: false);
                placement.Place(outSample, area[0, 0], style, log: false);

                Place staging = FreePlaceInColumn(zone["staging"], minRow: 0, currentlyAt: area[0, 0], skipNo:0);
                placement.MoveVerFirst(outSample, staging, clearance:1, style);
            }
            public void Split(List<SampleValue> outSamples, SampleValue inSample, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                //Gui.Log("Device: SPLIT " + Style.FormatSequence(outSamples, ", ", x => x.FormatSymbol(style)) + " = " + inSample.FormatSymbol(style));

                Place[,] area = FreeAreaInColumn(zone["mixing"], minRow: placement.PlaceOf(inSample, style).Row(), rows: 1, cols: 2 * outSamples.Count - 1); // find free columns in "mixing" block, on the same row
                placement.MoveHorFirst(inSample, area[0, 0], clearance: 1, style);

                double volume = inSample.Volume();
                for (int i = outSamples.Count - 1; i >= 1; i--) {
                    volume = volume - outSamples[i].Volume();
                    SampleValue tempSample = new SampleValue(inSample.symbol, null, new NumberValue(volume), new NumberValue(1), true);
                    placement.SplitHor(tempSample, outSamples[i], area[0, 0], area[0, 1], style);
                    placement.FollowRoute(placement.PathHorFirst(placement.PlaceOf(outSamples[i], style), area[0, 2 * i]), clearance: 0, style);
                }
                placement.Clear(area[0, 0], style, log: false);
                placement.Place(outSamples[0], area[0, 0], style, log: false);

                List<Place>[] routes = new List<Place>[outSamples.Count];
                for (int i = 0; i < outSamples.Count; i++) {
                    Place place = FreePlaceInColumn(zone["staging"], minRow: 0, currentlyAt: area[0, 2 * i], skipNo: i);
                    routes[i] = placement.PathVerFirst(placement.PlaceOf(outSamples[i], style), place);
                }
                placement.FollowRoutes(routes, clearance: 1, style);
            }
            public void Dispose(List<SampleValue> samples, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                //Gui.Log("Device: DISPOSE " + Style.FormatSequence(samples, ", ", x => x.FormatSymbol(style)));
                Place[,] area = FreeAreaInColumn(zone["mixing"], minRow: 0, rows: 1, cols: 2 * samples.Count - 1); // find free columns in "mixing" block, on the same row

                List<Place>[] routes = new List<Place>[samples.Count];
                for (int i = 0; i < samples.Count; i++)
                    routes[i] = placement.PathHorFirst(placement.PlaceOf(samples[i], style), area[0, 2 * i]);
                placement.FollowRoutes(routes, clearance: 1, style);
                for (int i = 0; i < samples.Count; i++)
                    placement.Disappear(placement.PlaceOf(samples[i], style), style);
            }
            public void Regulate(List<SampleValue> outSamples, List<SampleValue> inSamples, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                List<Place>[] routes = new List<Place>[inSamples.Count];
                //Gui.Log("Device: REGULATE " + Style.FormatSequence(outSamples, ", ", x =>x.FormatSymbol(style)) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)));
                for (int i = 0; i < inSamples.Count; i++) {
                    string toZone = (outSamples[i].Temperature() < KDeviceHandler.coldTemperature) ? "staging" : (outSamples[i].Temperature() > KDeviceHandler.hotTemperature) ? "hot" : "warm";
                    Place from = placement.PlaceOf(inSamples[i], style);
                    Place place = FreePlaceInColumn(zone[toZone], minRow: from.Row(), currentlyAt: from, skipNo: 0);
                    routes[i] = placement.PathHorFirst(placement.PlaceOf(inSamples[i], style), place);
                }
                placement.FollowRoutes(routes, clearance: 1, style);
                for (int i = 0; i < inSamples.Count; i++) {
                    Place place = placement.PlaceOf(inSamples[i], style);
                    placement.Remove(inSamples[i], style);
                    placement.Place(outSamples[i], place, style);
                }
                KGui.gui.GuiDeviceUpdate();
            }
            public void Concentrate(List<SampleValue> outSamples, List<SampleValue> inSamples, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                //Gui.Log("Device: CONCENTRATE " + Style.FormatSequence(outSamples, ", ", x =>x.FormatSymbol(style)) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)));
                for (int i = 0; i < inSamples.Count; i++) {
                    Place place = placement.PlaceOf(inSamples[i], style);
                    placement.Remove(inSamples[i], style);
                    placement.Place(outSamples[i], place, style);
                }
                KGui.gui.GuiDeviceUpdate();
            }
            public List<Place> StartEquilibrate(List<SampleValue> inSamples, double fortime, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                //Gui.Log("Device: EQUILIBRATE Start " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)) + " for " + style.FormatDouble(fortime));

                List<Place> goBacks = new List<Place> { };
                List<Place>[] routes = new List<Place>[inSamples.Count];
                for (int i = 0; i < inSamples.Count; i++) {
                    string toZone = (inSamples[i].Temperature() < KDeviceHandler.coldTemperature) ? "mixing" : (inSamples[i].Temperature() > KDeviceHandler.hotTemperature) ? "hot" : "warm";
                    Place goBack = placement.PlaceOf(inSamples[i], style);
                    goBacks.Add(goBack);
                    Place place = FreePlaceInColumn(zone[toZone], minRow: goBack.Row(), currentlyAt: goBack, skipNo: 0);
                    routes[i] = placement.PathHorFirst(placement.PlaceOf(inSamples[i], style), place);
                }
                placement.FollowRoutes(routes, clearance: 1, style);
                device.dropletColor = dropletColorGrey;
                device.dropletBiggieColor = dropletColorGrey;
                KGui.gui.GuiDeviceUpdate();
                return goBacks;
            }
            public void EndEquilibrate(List<Place> goBacks, List<SampleValue> outSamples, List<SampleValue> inSamples, double fortime, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                //Gui.Log("Device: EQUILIBRATE End " + Style.FormatSequence(outSamples, ", ", x => x.FormatSymbol(style)) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)) + " for " + style.FormatDouble(fortime));
                device.dropletColor = dropletColorRed;
                device.dropletBiggieColor = dropletColorPurple;
                for (int i = 0; i < inSamples.Count; i++) {
                    Place place = placement.PlaceOf(inSamples[i], style);
                    placement.Remove(inSamples[i], style, log: false);
                    placement.Place(outSamples[i], place, style, log: false);
                }
                KGui.gui.GuiDeviceUpdate();
                List<Place>[] routes = new List<Place>[inSamples.Count];
                for (int i = 0; i < inSamples.Count; i++) {
                    routes[i] = placement.PathVerFirst(placement.PlaceOf(outSamples[i], style), goBacks[i]);
                }
                placement.FollowRoutes(routes, clearance: 1, style);
            }

        }

        public class Place {
            private int col;
            private int row;
            public bool reserved;
            private Animation animation;
            public Place(int row, int column) {
                this.col = column;
                this.row = row;
                this.reserved = false;
                this.animation = Animation.None;
            }
            public int Column() { return col; }
            public int Row() { return row; }
            public string Format(Style style) {
                return "Place(col=" + col.ToString() + ", row=" + row.ToString() + ")";
            }
            public void SetAnimation(Animation animation) {
                lock (KDeviceHandler.device) { 
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    this.animation = animation;
                }
            }
            public bool IsAnimation(Animation animation) {
                return this.animation == animation;
            }
        }

        public class Placement {
            private Dictionary<SampleValue, Place> sampleToPlace;
            private Dictionary<Place, SampleValue> placeToSample;
            private Dictionary<SampleValue, Style> sampleToStyle;
            public Placement () {
                this.sampleToPlace = new Dictionary<SampleValue, Place> { };
                this.placeToSample = new Dictionary<Place, SampleValue> { };
                this.sampleToStyle = new Dictionary<SampleValue, Style> { };
            }

            public bool IsOccupied(Place place) {
                return placeToSample.ContainsKey(place);
            }
            public void CheckIsOccupied(Place place) {
                if (!IsOccupied(place)) 
                    throw new Error("No sample found on device at row " + place.Row().ToString() + ", column " + place.Column().ToString());
            }
            public bool IsPlaced(SampleValue sample) {
                return sampleToPlace.ContainsKey(sample);
            }
            public void CheckIsPlaced(SampleValue sample, Style style) {
                if (!IsPlaced(sample)) throw new Error("Sample not found on device: '" + sample.FormatSymbol(style) + "'" + (sample.IsConsumed() ? " (sample is already consumed)" : ""));
            }
            public Place PlaceOf(SampleValue sample, Style style) {
                CheckIsPlaced(sample, style);
                return sampleToPlace[sample];
            }
            public SampleValue SampleOf(Place place) {
                CheckIsOccupied(place);
                return placeToSample[place];
            }
            public Style StyleOf(SampleValue sample, Style style) {
                CheckIsPlaced(sample, style);
                return sampleToStyle[sample];
            }
            public bool IsAt(SampleValue sample, Place place) {
                if ((!IsOccupied(place)) || (!IsPlaced(sample))) return false;
                return placeToSample[place] == sample;
            }

            public void Place(SampleValue sample, Place place, Style style, bool log = true) {
                lock (KDeviceHandler.device) { 
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    if (IsPlaced(sample)) throw new Error("ERROR Device.Placement.Place");
                    if (IsOccupied(place)) throw new Error("ERROR Device.Placement.Place");
                    //if (log) Gui.Log("Device:   Place " + sample.FormatSymbol(style) + " into " + place.Format(style));
                    sampleToPlace[sample] = place;
                    placeToSample[place] = sample;
                    sampleToStyle[sample] = style;
                }
            }
            public Place Remove(SampleValue sample, Style style, bool log = true) {
                lock (KDeviceHandler.device) { 
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    CheckIsPlaced(sample, style);
                    Place place = sampleToPlace[sample];
                    //if (log) Gui.Log("Device:   Remove " + sample.FormatSymbol(style) + " from " + place.Format(style));
                    sampleToPlace.Remove(sample);
                    placeToSample.Remove(place);
                    sampleToStyle.Remove(sample);
                    return place;
                }
            }
            public SampleValue Extract(Place place, Style style, bool log = true) {
                lock (KDeviceHandler.device) { 
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    CheckIsOccupied(place);
                    SampleValue sample = placeToSample[place];
                    //if (log) Gui.Log("Device:   Extract " + sample.FormatSymbol(style) + " from " + place.Format(style));
                    sampleToPlace.Remove(sample);
                    placeToSample.Remove(place);
                    sampleToStyle.Remove(sample);
                    return sample;
                }
            }

            public SampleValue Clear(Place place, Style style, bool log = true) {
                CheckIsOccupied(place);
                //if (log) Gui.Log("Device:   Clear " + place.Format(style));
                SampleValue sample = SampleOf(place);
                Remove(sample, style, log); 
                return sample;
            }

            public bool CanStepTo(Place from, Place to, int clearance) {
                for (int i = to.Row() - clearance; i < to.Row() + clearance; i++) {
                    for (int j = to.Column() - clearance; j < to.Column() + clearance; j++) {
                        Place[,] places = KDeviceHandler.device.places;
                        if (i >= 0 && i < places.GetLength(0) && j >= 0 && j < places.GetLength(1)) {
                            Place place = places[i, j];
                            if (place != from && place != null && IsOccupied(place)) return false;
                        }
                    }
                }
                return true;
            }

            public Direction StepDirection(Place from, Place to, Style style) {
                if (from.Column() + 1 == to.Column() && from.Row() == to.Row()) return Direction.Rht;
                else if (from.Column() - 1 == to.Column() && from.Row() == to.Row()) return Direction.Lft;
                else if (from.Column() == to.Column() && from.Row() + 1 == to.Row()) return Direction.Bot;
                else if (from.Column() == to.Column() && from.Row() - 1 == to.Row()) return Direction.Top;
                else throw new Error("ERROR: StepDirection");
            }

            public void Step(Direction direction, Place from, Place to, Style style) {
                StepPhase(0, direction, from, to, style);
                KGui.gui.GuiDeviceUpdate(); Thread.Sleep(device.phaseDelay);
                StepPhase(1, direction, from, to, style);
                KGui.gui.GuiDeviceUpdate(); Thread.Sleep(device.phaseDelay);
                StepPhase(2, direction, from, to, style);
                KGui.gui.GuiDeviceUpdate(); Thread.Sleep(device.phaseDelay);
                StepPhase(3, direction, from, to, style);
                KGui.gui.GuiDeviceUpdate(); Thread.Sleep(device.stepDelay);
            }

            public void StepPhase(int phase, Direction direction, Place from, Place to, Style style) {
                if (phase == 0){
                    from.SetAnimation(phase0[(int)direction]);
                } else if (phase == 1) {
                    from.SetAnimation(phase1[(int)direction]);
                } else if (phase == 2) {
                    SampleValue sample = Extract(from, style, log: false);
                    Place(sample, to, style, log: false);
                    to.SetAnimation(phase2[(int)direction]);
                } else if (phase == 3) {
                    to.SetAnimation(phase3[(int)direction]);
                } else throw new Error("ERROR: StepPhase");
            }

            // disappear
            public void Appear(SampleValue sample, Place place, Style style) {
                Place(sample, place, style);
                place.SetAnimation(Animation.SizeQuarter);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
                place.SetAnimation(Animation.SizeHalf);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                place.SetAnimation(Animation.None);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // disappear
            public void Disappear(Place place, Style style) {
                place.SetAnimation(Animation.SizeHalf);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                place.SetAnimation(Animation.SizeQuarter);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
                Clear(place, style, log: false);
                place.SetAnimation(Animation.None);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // split Lft into Lft and Rht
            public void SplitHor(SampleValue splitLft, SampleValue splitRht, Place placeLft, Place placeRht, Style style) {
                placeLft.SetAnimation(Animation.PullRht);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                placeLft.SetAnimation(Animation.SplitRht);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
                Clear(placeLft, style, log: false);
                placeLft.SetAnimation(Animation.None);
                Place(splitLft, placeLft, style);
                placeRht.SetAnimation(Animation.None);
                Place(splitRht, placeRht, style);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // split Top into Top and Bot
            public void SplitVer(SampleValue splitTop, SampleValue splitBot, Place placeTop, Place placeBot, Style style) {
                placeTop.SetAnimation(Animation.PullBot);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                placeTop.SetAnimation(Animation.SplitBot);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
                Clear(placeTop, style);
                placeTop.SetAnimation(Animation.None);
                Place(splitTop, placeTop, style);
                placeBot.SetAnimation(Animation.None);
                Place(splitBot, placeBot, style);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // mix Lft and Rht into Lft
            public void MixHor(SampleValue sample, Place placeLft, Place placeRht, Style style) {
                placeLft.SetAnimation(Animation.PullRht);
                placeRht.SetAnimation(Animation.PullLft);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                Clear(placeLft, style);
                Clear(placeRht, style);
                placeLft.SetAnimation(Animation.None);
                placeRht.SetAnimation(Animation.None);
                Place(sample, placeLft, style);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // mix Top and Bot into Top
            public void MixVer(SampleValue sample, Place placeTop, Place placeBot, Style style) {
                placeTop.SetAnimation(Animation.PullBot);
                placeBot.SetAnimation(Animation.PullTop);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                Clear(placeTop, style);
                Clear(placeBot, style);
                placeTop.SetAnimation(Animation.None);
                placeBot.SetAnimation(Animation.None);
                Place(sample, placeTop, style);
                KGui.gui.GuiDeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            public void FollowRoute(List<Place> route, int clearance, Style style, bool log = true) {
                Place current = null;
                foreach (Place next in route) {
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    if (current == null) current = next; // starting place
                    else {
                        if (CanStepTo(current, next, clearance)) {
                            Step(StepDirection(current, next, style), current, next, style);
                            current = next;
                        } else throw new Error("ERROR: Droplet FollowRoute failed");
                    }
                }
            }
            
            public void FollowRoutes(List<Place>[] routes, int clearance, Style style) {
                bool[] currentCanStep = new bool[routes.Length]; // on phase 0 cache CanStep for all the 4 phases
                int[] nextStep = new int[routes.Length]; // step each path forward on phase 3, if it CanStep
                for (int i = 0; i < nextStep.Length; i++) nextStep[i] = 1;
                bool allFinished;
                do {
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    allFinished = true;
                    for (int phase = 0; phase < 4; phase++) {
// DEBUG BREAK HERE:
                        bool stepped = false;
                        for (int r = 0; r < routes.Length; r++) {
                            if (nextStep[r] < routes[r].Count) {
                                allFinished = false;
                                Place current = routes[r][nextStep[r] - 1];
                                Place next = routes[r][nextStep[r]];
                                if (phase == 0) 
                                    currentCanStep[r] = CanStepTo(current, next, clearance);
                                if (currentCanStep[r]) {
                                    StepPhase(phase, StepDirection(current, next, style), current, next, style);
                                    stepped = true;
                                    if (phase == 3) 
                                        nextStep[r]++;
                                }
                            }
                        }
                        if (!allFinished && !stepped) throw new Error("ERROR: FollowRoutes failed");
                        KGui.gui.GuiDeviceUpdate();
                        if (phase < 3) Thread.Sleep(device.phaseDelay); else Thread.Sleep(device.stepDelay);
                    }
                } while (!allFinished);
            }

            public List<Place> PathHorFirst(Place from, Place to) {
                List<Place> path = new List<Place> { from };
                Place current = from;
                int col = from.Column();
                while (col != to.Column()) {
                    int newCol = (col < to.Column()) ? col + 1 : col - 1;
                    Place next = device.PlaceAt(current.Row(), newCol);
                    path.Add(next);
                    col = newCol;
                    current = next;
                }
                int row = current.Row();
                while (row != to.Row()) {
                    int newRow = (row < to.Row()) ? row + 1 : row - 1;
                    Place next = device.PlaceAt(newRow, current.Column());
                    path.Add(next);
                    row = newRow;
                    current = next;
                }
                return path;
            }
            public List<Place> PathVerFirst(Place from, Place to) {
                List<Place> path = new List<Place> { from };
                Place current = from;
                int row = current.Row();
                while (row != to.Row()) {
                    int newRow = (row < to.Row()) ? row + 1 : row - 1;
                    Place next = device.PlaceAt(newRow, current.Column());
                    path.Add(next);
                    row = newRow;
                    current = next;
                }
                int col = from.Column();
                while (col != to.Column()) {
                    int newCol = (col < to.Column()) ? col + 1 : col - 1;
                    Place next = device.PlaceAt(current.Row(), newCol);
                    path.Add(next);
                    col = newCol;
                    current = next;
                }
                return path;
            }

            public void MoveHorFirst(SampleValue sample, Place place, int clearance, Style style) {
                //Gui.Log("Device:   Move " + sample.FormatSymbol(style) + " from " + PlaceOf(sample, style).Format(style) + " to " + place.Format(style));
                FollowRoute(PathHorFirst(PlaceOf(sample, style), place), clearance, style, log:false);
            }

            public void MoveVerFirst(SampleValue sample, Place place, int clearance, Style style) {
                //Gui.Log("Device:   Move " + sample.FormatSymbol(style) + " from " + PlaceOf(sample, style).Format(style) + " to " + place.Format(style));
                FollowRoute(PathVerFirst(PlaceOf(sample, style), place), clearance, style, log: false);
            }

            //public void MoveTo(SampleValue sample, Place place, int clearance, Style style){
            //    Gui.Log("Device:   Move " + sample.FormatSymbol(style) + " from " + PlaceOf(sample).Format(style) + " to " + place.Format(style));
            //    FollowRoute(RouteTo(sample, place, clearance), clearance, style, log: false);
            //}

            //public List<Place> RouteTo(SampleValue sample, Place place, int clearance) {
            //    // #### need to run a shortest distance algorithm
            //    Place from = PlaceOf(sample);
            //    Place to = place;
            //    Place current = from;
            //    List<Place> route = new List<Place> { from };
            //    int row = from.Row();
            //    int col = from.Column();
            //    while (row != to.Row() || col != to.Column()) {
            //        Place next = null;
            //        bool stepped = false;
            //        int newCol = (col < to.Column()) ? col + 1 : (col > to.Column()) ? col - 1 : col;
            //        int newRow = (row < to.Row()) ? row + 1 : (row > to.Row()) ? row - 1 : row;
            //        if (col != newCol) {
            //            next = device.PlaceAt(row, newCol);
            //            if (CanStepTo(current, next, clearance)) { col = newCol; stepped = true;  }
            //        }
            //        if (!stepped && row != newRow) {
            //            next = device.PlaceAt(newRow, col);
            //            if (CanStepTo(current, next, clearance)) { row = newRow; stepped = true; }
            //        }
            //        if (!stepped) throw new Error("ERROR: Droplet RouteTo failed");
            //        route.Add(next);
            //        current = next;
            //    }
            //    return route;
            //}

        }

    }
}
