using System;
using System.Collections.Generic;
using System.Threading;
using SkiaSharp;
using GraphSharp;

namespace Kaemika
{

    public abstract class ProtocolDevice {
        // Protocol device; mirrors the Protocol class

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

        private static Device device = null; // the only device
        //public static int desiredWidth;
        //public static int desiredHeight;

        public static bool Exists() {
            return device != null;
        }
        public static void Start(int phaseDelay, int stepDelay) {
            device = new Device(phaseDelay, stepDelay);
            Gui.gui.DeviceUpdate();
        }
        public static void Stop() {
            device = null;
        }
        public static void Clear() {
            if (device == null) return;
            device = new Device(device.phaseDelay, device.stepDelay);
            Gui.gui.DeviceUpdate();
        }
        public static Swipe PinchPan() {
            if (device == null) return Swipe.Id;
            return device.pinchPan;
        }
        public static void SetPinchPan(Swipe pinchPan) {
            if (device == null) return;
            lock (device) { device.deviceImage = null; } // force redrawing of cached background
            device.pinchPan = pinchPan;
        }
        public static void ResetPinchPan() {
            if (device == null) return;
            lock (device) { device.deviceImage = null; } // force redrawing of cached background
            device.pinchPan = (Swipe.Same(device.pinchPan, Swipe.Id)) ? new Swipe(2.0f, new SKPoint(0, 0)) : Swipe.Id;
        }
        public static void SetPinchOrigin(SKPoint pinchOrigin) {
            if (device == null) return;
            device.pinchOrigin = pinchOrigin;
        }
        public static void DisplayPinchOrigin(bool displayPinchOrigin) {
            if (device == null) return;
            device.displayPinchOrigin = displayPinchOrigin;
        }
        public static void SetStyle(Style style) {
            if (device == null) return;
            device.SetStyle(style);
        }
        public static void Draw(SKCanvas canvas, int canvasX, int canvasY, int canvasWidth, int canvasHeight) {
            if (device == null) return;
            device.Draw(canvas, canvasX, canvasY, canvasWidth, canvasHeight);
        }

        public static void Sample(SampleValue sample, Style style) {
            if (device == null || Exec.IsVesselVariant(sample)) return;
            device.Sample(sample, style);
        }
        public static void Amount(SampleValue sample, SpeciesValue species, NumberValue initialValue, string dimension, Style style) {
            if (device == null || Exec.IsVesselVariant(sample)) return;
        }
        public static void Mix(SampleValue outSample, List<SampleValue> inSamples, Style style) {
            if (device == null || Exec.IsVesselVariant(outSample)) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            device.Mix(outSample, inSamples, style);
        }
        public static void Split(List<SampleValue> outSamples, SampleValue inSample, Style style) {
            if (device == null || Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            device.Split(outSamples, inSample, style);
        }
        public static void Dispose(List<SampleValue> samples, Style style) {
            if (device == null) return;
            foreach (SampleValue sample in samples) if (Exec.IsVesselVariant(sample)) return;
            device.Dispose(samples, style);
        }
        public static void Regulate(List<SampleValue> outSamples, List<SampleValue> inSamples, Style style) {
            if (device == null) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            device.Regulate(outSamples, inSamples, style);
        }
        public static void Concentrate(List<SampleValue> outSamples, List<SampleValue> inSamples, Style style) {
            if (device == null) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            device.Concentrate(outSamples, inSamples, style);
        }
        public static List<Place> StartEquilibrate(List<SampleValue> inSamples, double fortime, Style style) {
            if (device == null) return null;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return null;
            return device.StartEquilibrate(inSamples, fortime, style);
        }
        public static void EndEquilibrate(List<Place> goBacks, List<SampleValue> outSamples, List<SampleValue> inSamples, double fortime, Style style) {
            if (device == null) return;
            foreach (SampleValue inSample in inSamples) if (Exec.IsVesselVariant(inSample)) return;
            foreach (SampleValue outSample in outSamples) if (Exec.IsVesselVariant(outSample)) return;
            Thread.Sleep(device.stepDelay);
            device.EndEquilibrate(goBacks, outSamples, inSamples, fortime, style);
        }

        public class Device {
            public Place[,] places; // rows, columns   // PROTECT concurrent access BY LOCKING ProtocolDevice.Device
            public Placement placement;                // PROTECT concurrent access BY LOCKING ProtocolDevice.Device
            private Dictionary<string, int> zone;
            // for rendering:
            public Style style;
            public SKBitmap deviceImage;
            public SKColor dropletColor;
            public SKColor dropletBiggieColor;
            public int rowsNo; // >= Places.GetLength(0), visible pads
            public int colsNo; // >= Places.GetLength(1), visible pads
            public int coldZoneWidth;
            public int warmZoneWidth;
            public int hotZoneWidth;
            public int phaseDelay;
            public int stepDelay;

            public Swipe pinchPan = Swipe.Id;
            public bool displayPinchOrigin = false;
            public SKPoint pinchOrigin;

            public Device(int phaseDelay, int stepDelay) {
                this.places = new Place[0, 0];
                this.placement = new Placement();
                this.zone = new Dictionary<string, int> { { "staging", 0 }, { "mixing", 2 }, { "warm", 12 }, { "hot", 18 } };
                // for rendering:
                this.style = new Style();
                this.deviceImage = null;
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
            private (float, float) FittedDimensions(int canvasWidth, int canvasHeight) {
                float width = canvasWidth; // int to float
                float padRadius = width / (2 * (colsNo + 1)); // + 1 for margin = padRadius
                float margin = padRadius;
                float deviceWidth = canvasWidth;
                float deviceHeight = 2 * padRadius * rowsNo + 2 * margin;

                float widthRatio = 1.0F; // canvasWidth / deviceWidth
                float heightRatio =  ((float)canvasHeight) / deviceHeight;
                float ratio = Math.Min(widthRatio, heightRatio);
                //int fittedWidth = (int)(ratio * deviceWidth);
                //int fittedHeight = (int)(ratio * deviceHeight);
                float fittedMargin = ratio * margin;
                float fittedPadRadius = ratio * padRadius;
                return (fittedMargin, fittedPadRadius);
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
                    this.deviceImage = null;
                }
                Gui.gui.DeviceUpdate(); // outside of lock, or it will deadlock
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
                Gui.Log("Device: NEW DROPLET " + sample.FormatSymbol(style));
                Place source = ReservePlaceInColumn(zone["staging"]);
                placement.Appear(sample, source, style);
                //placement.Place(sample, source, style);
                //Gui.gui.DeviceUpdate();
            }
            public void Mix(SampleValue outSample, List<SampleValue> inSamples, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                Gui.Log("Device: MIX " + outSample.FormatSymbol(style) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)));
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
                Gui.Log("Device: SPLIT " + Style.FormatSequence(outSamples, ", ", x => x.FormatSymbol(style)) + " = " + inSample.FormatSymbol(style));

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
                Gui.Log("Device: DISPOSE " + Style.FormatSequence(samples, ", ", x => x.FormatSymbol(style)));
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
                Gui.Log("Device: REGULATE " + Style.FormatSequence(outSamples, ", ", x =>x.FormatSymbol(style)) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)));
                for (int i = 0; i < inSamples.Count; i++) {
                    string toZone = (outSamples[i].Temperature() < ProtocolDevice.coldTemperature) ? "staging" : (outSamples[i].Temperature() > ProtocolDevice.hotTemperature) ? "hot" : "warm";
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
                Gui.gui.DeviceUpdate();
            }
            public void Concentrate(List<SampleValue> outSamples, List<SampleValue> inSamples, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                Gui.Log("Device: CONCENTRATE " + Style.FormatSequence(outSamples, ", ", x =>x.FormatSymbol(style)) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)));
                for (int i = 0; i < inSamples.Count; i++) {
                    Place place = placement.PlaceOf(inSamples[i], style);
                    placement.Remove(inSamples[i], style);
                    placement.Place(outSamples[i], place, style);
                }
                Gui.gui.DeviceUpdate();
            }
            public List<Place> StartEquilibrate(List<SampleValue> inSamples, double fortime, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                Gui.Log("Device: EQUILIBRATE Start " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)) + " for " + style.FormatDouble(fortime));

                List<Place> goBacks = new List<Place> { };
                List<Place>[] routes = new List<Place>[inSamples.Count];
                for (int i = 0; i < inSamples.Count; i++) {
                    string toZone = (inSamples[i].Temperature() < ProtocolDevice.coldTemperature) ? "mixing" : (inSamples[i].Temperature() > ProtocolDevice.hotTemperature) ? "hot" : "warm";
                    Place goBack = placement.PlaceOf(inSamples[i], style);
                    goBacks.Add(goBack);
                    Place place = FreePlaceInColumn(zone[toZone], minRow: goBack.Row(), currentlyAt: goBack, skipNo: 0);
                    routes[i] = placement.PathHorFirst(placement.PlaceOf(inSamples[i], style), place);
                }
                placement.FollowRoutes(routes, clearance: 1, style);
                device.dropletColor = dropletColorGrey;
                device.dropletBiggieColor = dropletColorGrey;
                Gui.gui.DeviceUpdate();
                return goBacks;
            }
            public void EndEquilibrate(List<Place> goBacks, List<SampleValue> outSamples, List<SampleValue> inSamples, double fortime, Style style) {
                if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                Gui.Log("Device: EQUILIBRATE End " + Style.FormatSequence(outSamples, ", ", x => x.FormatSymbol(style)) + " = " + Style.FormatSequence(inSamples, ", ", x => x.FormatSymbol(style)) + " for " + style.FormatDouble(fortime));
                device.dropletColor = dropletColorRed;
                device.dropletBiggieColor = dropletColorPurple;
                for (int i = 0; i < inSamples.Count; i++) {
                    Place place = placement.PlaceOf(inSamples[i], style);
                    placement.Remove(inSamples[i], style, log: false);
                    placement.Place(outSamples[i], place, style, log: false);
                }
                Gui.gui.DeviceUpdate();
                List<Place>[] routes = new List<Place>[inSamples.Count];
                for (int i = 0; i < inSamples.Count; i++) {
                    routes[i] = placement.PathVerFirst(placement.PlaceOf(outSamples[i], style), goBacks[i]);
                }
                placement.FollowRoutes(routes, clearance: 1, style);
            }

            // === DEVICE DRAW === //

            public void SetStyle(Style style) {
                this.style = style;
            }

            public void Draw(SKCanvas canvas, int canvasX, int canvasY, int canvasWidth, int canvasHeigth) { 

                (float margin, float padRadius) = FittedDimensions(canvasWidth, canvasHeigth);
                float strokeWidth = padRadius / 10.0f;
                float accentStrokeWidth = 1.5f * strokeWidth;
                float textStrokeWidth = accentStrokeWidth / 2.0f;

                if (this.deviceImage == null || this.deviceImage.Width != canvasWidth || this.deviceImage.Height != canvasHeigth) {
                    this.deviceImage = new SKBitmap(canvasWidth, canvasHeigth);
                    using (var backgroundCanvas = new SKCanvas(this.deviceImage)) {
                        DrawDevice(backgroundCanvas, canvasX, canvasY, canvasWidth, canvasHeigth, padRadius, margin, pinchPan);
                    }
                }

                if (this.deviceImage != null) canvas.DrawBitmap(this.deviceImage, 0, 0); // do not apply pinchPan: background bitmap is alread scaled by it

                if (displayPinchOrigin) {
                    // same as: GraphSharp.GraphLayout.CanvasDrawCircle(canvas, pinchOrigin, 20, false, SKColors.LightGray);
                    using (var paint = new SKPaint()) {
                        paint.TextSize = 10; paint.IsAntialias = true; paint.Color = SKColors.LightGray; paint.IsStroke = false;
                        canvas.DrawCircle(pinchOrigin.X, pinchOrigin.Y, 20, paint);
                    }
                }

                using (var dropletFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = this.dropletColor, IsAntialias = true })
                using (var dropletBiggieFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = this.dropletBiggieColor, IsAntialias = true })
                {
                    Place[,] places = this.places;
                    Placement placement = this.placement;
                        for (int row = 0; row < places.GetLength(0); row++) {
                            for (int col = 0; col < places.GetLength(1); col++) {
                                Place place = places[row, col];
                                if (place != null && placement.IsOccupied(place)) {
                                    SampleValue sample = placement.SampleOf(place);
                                    float volumeRadius = padRadius * (float)Math.Sqrt((sample.Volume())*1000000.0); // normal radius = 1μL
                                    float diameter = 2 * padRadius;
                                    SKPaint fillPaint = dropletFillPaint;
                                    bool biggie = false;
                                    if (volumeRadius > 2 * padRadius) {
                                        biggie = true;
                                        volumeRadius = 2 * padRadius;
                                        fillPaint = dropletBiggieFillPaint;
                                    }
                                    SKPoint here = new SKPoint(canvasX + margin + padRadius + col * diameter, canvasY + margin + padRadius + row * diameter);
                                    SKPoint rht  = new SKPoint(canvasX + margin + padRadius + (col + 1) * diameter, canvasY + margin + padRadius + row * diameter);
                                    SKPoint lft  = new SKPoint(canvasX + margin + padRadius + (col - 1) * diameter, canvasY + margin + padRadius + row * diameter);
                                    SKPoint bot  = new SKPoint(canvasX + margin + padRadius + col * diameter, canvasY + margin + padRadius + (row + 1) * diameter);
                                    SKPoint top  = new SKPoint(canvasX + margin + padRadius + col * diameter, canvasY + margin + padRadius + (row - 1) * diameter);
                                    string label = sample.symbol.Raw(); // sample.FormatSymbol(placement.StyleOf(sample, style))
                                    if (place.IsAnimation(Animation.None))
                                        DrawDroplet(canvas, label, biggie, here, 
                                            padRadius, volumeRadius, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.SizeHalf))
                                        DrawDroplet(canvas, label, biggie, here,
                                            padRadius, volumeRadius / 2, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.SizeQuarter))
                                        DrawDroplet(canvas, label, biggie, here,
                                            padRadius, volumeRadius / 4, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.PullRht))
                                        DrawDropletPulledHor(canvas, label, biggie, here, rht, Direction.Rht,
                                            padRadius, volumeRadius * 5 / 6, volumeRadius * 5 / 12, volumeRadius * 1 / 3, 
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.SplitRht))
                                        DrawDropletPulledHor(canvas, label, biggie, here, rht, Direction.Rht,
                                            padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3, 
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.PullLft))
                                        DrawDropletPulledHor(canvas, label, biggie, lft, here, Direction.Lft,
                                            padRadius, volumeRadius * 1 / 3, volumeRadius * 5 / 12, volumeRadius * 5 / 6,
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.SplitLft))
                                        DrawDropletPulledHor(canvas, label, biggie, lft, here, Direction.Lft,
                                            padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.PullBot))
                                        DrawDropletPulledVer(canvas, label, biggie, here, bot, Direction.Bot,
                                            padRadius, volumeRadius * 5 / 6, volumeRadius * 5 / 12, volumeRadius * 1 / 3,
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.SplitBot))
                                        DrawDropletPulledVer(canvas, label, biggie, here, bot, Direction.Bot,
                                            padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.PullTop))
                                        DrawDropletPulledVer(canvas, label, biggie, top, here, Direction.Top,
                                            padRadius, volumeRadius * 1 / 3, volumeRadius * 5 / 12, volumeRadius * 5 / 6,
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                    if (place.IsAnimation(Animation.SplitTop))
                                        DrawDropletPulledVer(canvas, label, biggie, top, here, Direction.Top,
                                            padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                            textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, pinchPan);
                                }
                            }
                        }
                }

                canvas.Flush();            
            }

            public static SKColor coldColor = new SKColor(0, 127, 127, 63);
            public static SKColor hotColor = new SKColor(127, 0, 127, 63);

            public SKRect InflateRect(SKRect rect, float n) {
                SKRect inflated = rect;
                inflated.Inflate(n, n);
                return inflated;
            }

            public void DrawBackground(SKCanvas canvas, float canvasX, float canvasY, float canvasWidth, float canvasHeight) {
                using (var backPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = deviceBackColor }) {
                    canvas.DrawRect(new SKRect(canvasX, canvasY, canvasX + canvasWidth, canvasY + canvasHeight), backPaint);
                }
            }

            public void DrawDevice(SKCanvas canvas, float canvasX, float canvasY, float canvasWidth, float canvasHeight, float padRadius, float margin, Swipe swipe) {
                float padStrokeWidth = padRadius / 10.0f;
                float padStrokeWidthAccent = 0.75f * padStrokeWidth;

                SKRect coldZoneRect = new SKRect(canvasX + margin, canvasY + margin, canvasX + margin + coldZoneWidth * 2 * padRadius, canvasY + margin + rowsNo * 2 * padRadius);
                SKRect hotZoneRect = new SKRect(canvasX + margin + (coldZoneWidth + warmZoneWidth) * 2 * padRadius, canvasY + margin, canvasX + margin + (coldZoneWidth + warmZoneWidth + hotZoneWidth) * 2 * padRadius, canvasY + margin + rowsNo * 2 * padRadius);

                DrawBackground(canvas, canvasX, canvasY, canvasWidth, canvasHeight); // excluding the area outside the fitted region covered by the deviceImage

                DrawHeatZone(canvas, coldZoneRect, padRadius, coldZoneRect.Width / (2 * coldZoneWidth), coldColor, swipe);
                DrawHeatZone(canvas, hotZoneRect, padRadius, hotZoneRect.Width / (2 * hotZoneWidth), hotColor, swipe);
                DrawText(canvas, "< " + ProtocolDevice.coldTemp, new SKPoint(coldZoneRect.MidX, coldZoneRect.Bottom + margin), padRadius, SKColors.Blue, swipe);
                DrawText(canvas, "> " + ProtocolDevice.hotTemp, new SKPoint(hotZoneRect.MidX, hotZoneRect.Bottom + margin), padRadius, SKColors.Blue, swipe);

                float strokePaintStrokeWidth = padStrokeWidth;
                float strokeAccentPaintStrokeWidth = padStrokeWidthAccent;

                using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.Black, StrokeWidth = swipe % strokePaintStrokeWidth, StrokeJoin = SKStrokeJoin.Round, IsAntialias = true })
                using (var strokeAccentPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = SKColors.White, StrokeWidth = swipe % strokeAccentPaintStrokeWidth, StrokeJoin = SKStrokeJoin.Round, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
                using (var fillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Goldenrod, IsAntialias = true }) // SKColors.DarkGoldenrod / Goldenrod / PaleGoldenrod / LightGoldenrodYellow / Gold
                using (var holePaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.Black, IsAntialias = true })
                using (var holeAccentPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = SKColors.White, IsAntialias = true })
                {
                    for (int j = 0; j < rowsNo; j++) for (int i = 0; i < colsNo; i++)
                            DrawPad(canvas, new SKPoint(canvasX + margin + padRadius + i * 2 * padRadius, canvasY + margin + padRadius + j * 2 * padRadius), padRadius, strokePaint, strokePaintStrokeWidth, fillPaint, holePaint, strokeAccentPaint, strokeAccentPaintStrokeWidth, holeAccentPaint, swipe);
                }
            }

            public void DrawHeatZone(SKCanvas canvas, SKRect zone, float halo, float groove, SKColor color, Swipe swipe) {
                //SKMatrix scaleMatrix = SKMatrix.MakeScale(coldZoneRect.Width/coldZoneRect.Height, 1.0f);
                var radialGradient = SKShader.CreateRadialGradient(swipe % new SKPoint(zone.MidX, zone.MidY), swipe % groove, new SKColor[2] { color, deviceBackColor }, null, SKShaderTileMode.Mirror); //, scaleMatrix);
                using (var gradientPaint = new SKPaint { Style = SKPaintStyle.Fill, Shader = radialGradient }) {
                    canvas.DrawRoundRect(swipe % InflateRect(zone, halo), swipe % halo, swipe % halo, gradientPaint);
                }
            }
         
            public void DrawText(SKCanvas canvas, string text, SKPoint center, float size, SKColor color, Swipe swipe){
                using (var labelPaint = new SKPaint { Style = SKPaintStyle.Fill, TextSize = swipe % size, TextAlign = SKTextAlign.Center, Color = color, IsAntialias = true }) {
                    canvas.DrawText(text, swipe % center, labelPaint);
                }
            }

            public void DrawDropletLabel(SKCanvas canvas, string label, SKPoint center, float radius, float strokeWidth, bool biggie, Swipe swipe){
                using (var labelPaint = new SKPaint { Style = SKPaintStyle.Stroke, TextSize = swipe % (0.5f * radius), TextAlign = SKTextAlign.Center, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true }) {
                    canvas.DrawText(label, swipe % new SKPoint(center.X, center.Y + 0.1f * radius), labelPaint);
                    if (biggie) canvas.DrawText(">4μL", swipe % new SKPoint(center.X, center.Y + 0.6f * radius), labelPaint);
                }
            }

            public void DrawDroplet(SKCanvas canvas, string label, bool biggie, SKPoint center, float padRadius, float radius, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe) {
                float ratio = radius / padRadius;
                //strokeWidth = strokeWidth * ratio;
                textStrokeWidth = textStrokeWidth * ratio;
                accentStrokeWidth = accentStrokeWidth * ratio;

                float pull = 0.75f;
                float accentRadius = radius - (strokeWidth + accentStrokeWidth) / 2.0f;

                var path = new SKPath();
                path.MoveTo(swipe % new SKPoint(center.X, center.Y - radius));
                path.CubicTo(swipe % new SKPoint(center.X + pull* radius, center.Y - radius), swipe % new SKPoint(center.X + radius, center.Y - pull* radius), swipe % new SKPoint(center.X + radius, center.Y));
                path.CubicTo(swipe % new SKPoint(center.X + radius, center.Y + pull* radius), swipe % new SKPoint(center.X + pull* radius, center.Y + radius), swipe % new SKPoint(center.X, center.Y + radius));
                path.CubicTo(swipe % new SKPoint(center.X - pull* radius, center.Y + radius), swipe % new SKPoint(center.X - radius, center.Y + pull* radius), swipe % new SKPoint(center.X - radius, center.Y));
                path.CubicTo(swipe % new SKPoint(center.X - radius, center.Y - pull* radius), swipe % new SKPoint(center.X - pull* radius, center.Y - radius), swipe % new SKPoint(center.X, center.Y - radius));
                path.Close();

                var darkPath = new SKPath();
                darkPath.MoveTo(swipe % new SKPoint(center.X + accentRadius, center.Y));
                darkPath.CubicTo(swipe % new SKPoint(center.X + accentRadius, center.Y + pull * accentRadius), swipe % new SKPoint(center.X + pull * accentRadius, center.Y + accentRadius), swipe % new SKPoint(center.X, center.Y + accentRadius));

                var lightPath = new SKPath();
                lightPath.MoveTo(swipe % new SKPoint(center.X - accentRadius, center.Y));
                lightPath.CubicTo(swipe % new SKPoint(center.X - accentRadius, center.Y - pull * accentRadius), swipe % new SKPoint(center.X - pull * accentRadius, center.Y - accentRadius), swipe % new SKPoint(center.X, center.Y - accentRadius));

                using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = pinchPan % strokeWidth, IsAntialias = true })
                using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = pinchPan % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
                using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = pinchPan % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true }) {
                    canvas.DrawPath(path, fillPaint);
                    canvas.DrawPath(darkPath, accentDarkPaint);
                    canvas.DrawPath(lightPath, accentLightPaint);
                    canvas.DrawPath(path, strokePaint);
                }

                DrawDropletLabel(canvas, label, center, radius, textStrokeWidth, biggie, swipe);
            }

            public void DrawDropletPulledHor(SKCanvas canvas, string label, bool biggie, SKPoint c1, SKPoint c2, Direction dir, float r, float r1, float neckY, float r2, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe) {
                float ratio = ((r1+r2)/2) / r;
                //strokeWidth = strokeWidth * ratio;
                textStrokeWidth = textStrokeWidth * ratio;
                accentStrokeWidth = accentStrokeWidth * ratio;

                float lr = r1 - (strokeWidth + accentStrokeWidth) / 2.0f;
                float dr = r2 - (strokeWidth + accentStrokeWidth) / 2.0f;

                // place the neck in an X-position proportional to the rate between r1 and r2, with m1+r1X to the left leading to c1.X, and m2+r2X to the right leading to c2.X
                float m1 = (2 * r - (r1 + r2)) / (1 + r2 / r1);
                float m2 = m1 * r2 / r1;
                // position of the neck
                float nX = c1.X + r1 + m1;
                float nY = c1.Y;
                // Control points: a1*2 is on a line from nX,nY-neckY to c1.X,c.Y-r1 where it intersects c1.X+r1; we divide it by 2 to make the curve smoother
                float a1 = (m1 * (r1 - neckY) / (r1 + m1))/2;
                float a2 = (m2 * (r2 - neckY) / (r2 + m2))/2;

                var path = new SKPath();
                path.MoveTo( swipe % new SKPoint(c1.X, c1.Y - r1));
                path.CubicTo(swipe % new SKPoint(c1.X + 0.5f * r1, c1.Y - r1), swipe % new SKPoint(c1.X + r1, nY - (neckY + a1)), swipe % new SKPoint(nX, nY - neckY));
                path.CubicTo(swipe % new SKPoint(c2.X - r2, nY - (neckY + a2)), swipe % new SKPoint(c2.X - 0.5f * r2, c2.Y - r2), swipe % new SKPoint(c2.X, c2.Y - r2));

                path.CubicTo(swipe % new SKPoint(c2.X + 0.75f * r2, c2.Y - r2), swipe % new SKPoint(c2.X + r2, c2.Y - 0.75f * r2), swipe % new SKPoint(c2.X + r2, c2.Y));
                path.CubicTo(swipe % new SKPoint(c2.X + r2, c2.Y + 0.75f * r2), swipe % new SKPoint(c2.X + 0.75f*r2, c2.Y + r2), swipe % new SKPoint(c2.X, c2.Y + r2));

                path.CubicTo(swipe % new SKPoint(c2.X - 0.5f * r2, c2.Y + r2), swipe % new SKPoint(c2.X - r2, nY + (neckY + a2)), swipe % new SKPoint(nX, nY + neckY));
                path.CubicTo(swipe % new SKPoint(c1.X + r1, nY + (neckY + a1)), swipe % new SKPoint(c1.X + 0.5f * r1, c1.Y + r1), swipe % new SKPoint(c1.X, c1.Y + r1));

                path.CubicTo(swipe % new SKPoint(c1.X - 0.75f * r1, c1.Y + r1), swipe % new SKPoint(c1.X - r1, c1.Y + 0.75f * r1), swipe % new SKPoint(c1.X - r1, c1.Y));
                path.CubicTo(swipe % new SKPoint(c1.X - r1, c1.Y - 0.75f * r1), swipe % new SKPoint(c1.X - 0.75f * r1, c1.Y - r1), swipe % new SKPoint(c1.X, c1.Y - r1));
                path.Close();

                var darkPath = new SKPath();
                darkPath.MoveTo(swipe % new SKPoint(c2.X + dr, c2.Y));
                darkPath.CubicTo(swipe % new SKPoint(c2.X + dr, c2.Y + 0.75f * dr), swipe % new SKPoint(c2.X + 0.75f * dr, c2.Y + dr), swipe % new SKPoint(c2.X, c2.Y + dr));

                var lightPath = new SKPath();
                lightPath.MoveTo(swipe % new SKPoint(c1.X - lr, c1.Y));
                lightPath.CubicTo(swipe % new SKPoint(c1.X - lr, c1.Y - 0.75f * lr), swipe % new SKPoint(c1.X - 0.75f * lr, c1.Y - lr), swipe % new SKPoint(c1.X, c1.Y - lr));

                using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = pinchPan % strokeWidth, IsAntialias = true })
                using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = pinchPan % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
                using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = pinchPan % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true }) {
                    canvas.DrawPath(path, fillPaint);
                    canvas.DrawPath(darkPath, accentDarkPaint);
                    canvas.DrawPath(lightPath, accentLightPaint);
                    canvas.DrawPath(path, strokePaint);
                }

                float rText = (r1 == r2) ? ((dir == Direction.Lft) ? r1 : r2) : (r1 > r2) ? r1 : r2;
                SKPoint cText = (r1 == r2) ? ((dir == Direction.Lft) ? c1 : c2) : (r1 > r2) ? c1 : c2;
                DrawDropletLabel(canvas, label, cText, rText, textStrokeWidth, biggie, swipe);
            }

            public void DrawDropletPulledVer(SKCanvas canvas, string label, bool biggie, SKPoint c1, SKPoint c2, Direction dir, float r, float r1, float neckX, float r2, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe) {
                float ratio = ((r1 + r2) / 2) / r;
                //strokeWidth = strokeWidth * ratio;
                textStrokeWidth = textStrokeWidth * ratio;
                accentStrokeWidth = accentStrokeWidth * ratio;

                float lr = r1 - (strokeWidth + accentStrokeWidth) / 2.0f;
                float dr = r2 - (strokeWidth + accentStrokeWidth) / 2.0f;

                // place the neck in an Y-position proportional to the rate between r1 and r2, with m1+r1Y to the top leading to c1.Y, and m2+r2Y to the bot leading to c2.Y
                float m1 = (2 * r - (r1 + r2)) / (1 + r2 / r1);
                float m2 = m1 * r2 / r1;
                // position of the neck
                float nY = c1.Y + r1 + m1;
                float nX = c1.X;
                // Control points: a1*2 is on a line from nY,nX-neckX to c1.Y,c.X-r1 where it intersects c1.Y+r1; we divide it by 2 to make the curve smoother
                float a1 = (m1 * (r1 - neckX) / (r1 + m1))/2;
                float a2 = (m2 * (r2 - neckX) / (r2 + m2))/2;

                var path = new SKPath();
                path.MoveTo( swipe % new SKPoint(c1.X - r1, c1.Y));
                path.CubicTo(swipe % new SKPoint(c1.X - r1, c1.Y + 0.5f * r1), swipe % new SKPoint(nX - (neckX + a1), c1.Y + r1), swipe % new SKPoint(nX - neckX, nY));
                path.CubicTo(swipe % new SKPoint(nX - (neckX + a2), c2.Y - r2), swipe % new SKPoint(c2.X - r2, c2.Y - 0.5f * r2), swipe % new SKPoint(c2.X - r2, c2.Y));

                path.CubicTo(swipe % new SKPoint(c2.X - r2, c2.Y + 0.75f * r2), swipe % new SKPoint(c2.X - 0.75f * r2, c2.Y + r2), swipe % new SKPoint(c2.X, c2.Y + r2));
                path.CubicTo(swipe % new SKPoint(c2.X + 0.75f * r2, c2.Y + r2), swipe % new SKPoint(c2.X + r2, c2.Y + 0.75f* r2), swipe % new SKPoint(c2.X + r2, c2.Y));

                path.CubicTo(swipe % new SKPoint(c2.X + r2, c2.Y - 0.5f * r2), swipe % new SKPoint(nX + (neckX + a2), c2.Y - r2), swipe % new SKPoint(nX + neckX, nY));
                path.CubicTo(swipe % new SKPoint(nX + (neckX + a1), c1.Y + r1), swipe % new SKPoint(c1.X + r1, c1.Y + 0.5f * r1), swipe % new SKPoint(c1.X + r1, c1.Y));

                path.CubicTo(swipe % new SKPoint(c1.X + r1, c1.Y - 0.75f * r1), swipe % new SKPoint(c1.X + 0.75f * r1, c1.Y - r1), swipe % new SKPoint(c1.X, c1.Y - r1));
                path.CubicTo(swipe % new SKPoint(c1.X - 0.75f * r1, c1.Y - r1), swipe % new SKPoint(c1.X - r1, c1.Y - 0.75f * r1), swipe % new SKPoint(c1.X - r1, c1.Y));
                path.Close();

                var darkPath = new SKPath();
                darkPath.MoveTo(swipe % new SKPoint(c2.X, c2.Y + dr));
                darkPath.CubicTo(swipe % new SKPoint(c2.X + 0.75f * dr, c2.Y + dr), swipe % new SKPoint(c2.X + dr, c2.Y + 0.75f * dr), swipe % new SKPoint(c2.X + dr, c2.Y));

                var lightPath = new SKPath();
                lightPath.MoveTo(swipe % new SKPoint(c1.X, c1.Y - lr));
                lightPath.CubicTo(swipe % new SKPoint(c1.X - 0.75f * lr, c1.Y - lr), swipe % new SKPoint(c1.X - lr, c1.Y - 0.75f * lr), swipe % new SKPoint(c1.X - lr, c1.Y));

                using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = pinchPan % strokeWidth, IsAntialias = true })
                using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = pinchPan % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
                using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = pinchPan % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true }) {
                    canvas.DrawPath(path, fillPaint);
                    canvas.DrawPath(darkPath, accentDarkPaint);
                    canvas.DrawPath(lightPath, accentLightPaint);
                    canvas.DrawPath(path, strokePaint);
                }

                float rYtext = (r1 == r2) ? ((dir == Direction.Top) ? r1 : r2) : (r1 > r2) ? r1 : r2;
                SKPoint cText = (r1 == r2) ? ((dir == Direction.Top) ? c1 : c2) : (r1 > r2) ? c1 : c2;
                DrawDropletLabel(canvas, label, cText, rYtext, textStrokeWidth, biggie, swipe);
            }

            public void DrawPad(SKCanvas canvas, SKPoint center, float radius, SKPaint strokePaint, float strokePaintStrokeWidth, SKPaint fillPaint, SKPaint holePaint, SKPaint strokeAccentPaint, float strokeAccentPaintStrokeWidth, SKPaint holeAccentPaint, Swipe swipe) {
                float step = radius / 7.0f;
                float holeRadius = radius / 7.0f;
                float orthShift = (strokePaintStrokeWidth + strokeAccentPaintStrokeWidth) / 2.0f;
                float diagShift = orthShift / 1.41f;

                var path = new SKPath();
                path.MoveTo(swipe % new SKPoint(center.X - radius, center.Y - radius));
                path.LineTo(swipe % new SKPoint(center.X - radius, center.Y - radius + 1 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius + 1 * step, center.Y - radius + 2 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius - 1 * step, center.Y - radius + 4 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius + 1 * step, center.Y - radius + 6 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius - 1 * step, center.Y - radius + 8 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius + 1 * step, center.Y - radius + 10 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius - 1 * step, center.Y - radius + 12 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius, center.Y - radius + 13 * step));

                path.LineTo(swipe % new SKPoint(center.X - radius,             center.Y + radius));
                path.LineTo(swipe % new SKPoint(center.X - radius +  1 * step, center.Y + radius));
                path.LineTo(swipe % new SKPoint(center.X - radius +  2 * step, center.Y + radius - 1 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius +  4 * step, center.Y + radius + 1 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius +  6 * step, center.Y + radius - 1 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius +  8 * step, center.Y + radius + 1 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius + 10 * step, center.Y + radius - 1 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius + 12 * step, center.Y + radius + 1 * step));
                path.LineTo(swipe % new SKPoint(center.X - radius + 13 * step, center.Y + radius));

                path.LineTo(swipe % new SKPoint(center.X + radius,            center.Y + radius));
                path.LineTo(swipe % new SKPoint(center.X + radius,            center.Y + radius - 1 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 1 * step, center.Y + radius - 2 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius + 1 * step, center.Y + radius - 4 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 1 * step, center.Y + radius - 6 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius + 1 * step, center.Y + radius - 8 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 1 * step, center.Y + radius - 10 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius + 1 * step, center.Y + radius - 12 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius,            center.Y + radius - 13 * step));

                path.LineTo(swipe % new SKPoint(center.X + radius, center.Y - radius));
                path.LineTo(swipe % new SKPoint(center.X + radius - 1 * step, center.Y - radius));
                path.LineTo(swipe % new SKPoint(center.X + radius - 2 * step, center.Y - radius + 1 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 4 * step, center.Y - radius - 1 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 6 * step, center.Y - radius + 1 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 8 * step, center.Y - radius - 1 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 10 * step, center.Y - radius + 1 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 12 * step, center.Y - radius - 1 * step));
                path.LineTo(swipe % new SKPoint(center.X + radius - 13 * step, center.Y - radius));

                path.LineTo(swipe % new SKPoint(center.X - radius, center.Y - radius));
                path.Close();

                var accPathLft = new SKPath();
                accPathLft.MoveTo(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + orthShift));
                accPathLft.LineTo(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + orthShift + 1 * step));
                accPathLft.MoveTo(swipe % new SKPoint(center.X - radius + diagShift + 1 * step, center.Y - radius + diagShift + 2 * step));
                accPathLft.LineTo(swipe % new SKPoint(center.X - radius + diagShift - 1 * step, center.Y - radius + diagShift + 4 * step));
                accPathLft.MoveTo(swipe % new SKPoint(center.X - radius + diagShift + 1 * step, center.Y - radius + diagShift + 6 * step));
                accPathLft.LineTo(swipe % new SKPoint(center.X - radius + diagShift - 1 * step, center.Y - radius + diagShift + 8 * step));
                accPathLft.MoveTo(swipe % new SKPoint(center.X - radius + diagShift + 1 * step, center.Y - radius + diagShift + 10 * step));
                accPathLft.LineTo(swipe % new SKPoint(center.X - radius + diagShift - 1 * step, center.Y - radius + diagShift + 12 * step));
                accPathLft.MoveTo(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + 13 * step));
                accPathLft.LineTo(swipe % new SKPoint(center.X - radius + orthShift, center.Y - radius + 14 * step));

                var accPathTop = new SKPath();
                accPathTop.MoveTo(swipe % new SKPoint(center.X + radius, center.Y - radius + orthShift));
                accPathTop.LineTo(swipe % new SKPoint(center.X + radius + diagShift / 2 - 1 * step, center.Y - radius + orthShift));
                accPathTop.LineTo(swipe % new SKPoint(center.X + radius + diagShift - 2 * step, center.Y - radius + diagShift + 1 * step));
                accPathTop.MoveTo(swipe % new SKPoint(center.X + radius + diagShift - 4 * step, center.Y - radius + diagShift - 1 * step));
                accPathTop.LineTo(swipe % new SKPoint(center.X + radius + diagShift - 6 * step, center.Y - radius + diagShift + 1 * step));
                accPathTop.MoveTo(swipe % new SKPoint(center.X + radius + diagShift - 8 * step, center.Y - radius + diagShift - 1 * step));
                accPathTop.LineTo(swipe % new SKPoint(center.X + radius + diagShift - 10 * step, center.Y - radius + diagShift + 1 * step));
                accPathTop.MoveTo(swipe % new SKPoint(center.X + radius + diagShift - 12 * step, center.Y - radius + diagShift - 1 * step));
                accPathTop.LineTo(swipe % new SKPoint(center.X + radius + diagShift / 2 - 13 * step, center.Y - radius + orthShift));
                accPathTop.LineTo(swipe % new SKPoint(center.X + radius + orthShift - 14 * step, center.Y - radius + orthShift));

                canvas.DrawPath(path, fillPaint);

                canvas.DrawPath(accPathLft, strokeAccentPaint);
                canvas.DrawPath(accPathTop, strokeAccentPaint);
                canvas.DrawPath(path, strokePaint);
                canvas.DrawCircle(swipe % new SKPoint(center.X + diagShift/2, center.Y + diagShift/2), swipe % (holeRadius + diagShift/2), holeAccentPaint);
                canvas.DrawCircle(swipe % center, swipe % holeRadius, holePaint);
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
                lock (ProtocolDevice.device) { 
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
                lock (ProtocolDevice.device) { 
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    if (IsPlaced(sample)) throw new Error("ERROR Device.Placement.Place");
                    if (IsOccupied(place)) throw new Error("ERROR Device.Placement.Place");
                    if (log) Gui.Log("Device:   Place " + sample.FormatSymbol(style) + " into " + place.Format(style));
                    sampleToPlace[sample] = place;
                    placeToSample[place] = sample;
                    sampleToStyle[sample] = style;
                }
            }
            public Place Remove(SampleValue sample, Style style, bool log = true) {
                lock (ProtocolDevice.device) { 
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    CheckIsPlaced(sample, style);
                    Place place = sampleToPlace[sample];
                    if (log) Gui.Log("Device:   Remove " + sample.FormatSymbol(style) + " from " + place.Format(style));
                    sampleToPlace.Remove(sample);
                    placeToSample.Remove(place);
                    sampleToStyle.Remove(sample);
                    return place;
                }
            }
            public SampleValue Extract(Place place, Style style, bool log = true) {
                lock (ProtocolDevice.device) { 
                    if (!Exec.IsExecuting()) throw new ExecutionEnded("");
                    CheckIsOccupied(place);
                    SampleValue sample = placeToSample[place];
                    if (log) Gui.Log("Device:   Extract " + sample.FormatSymbol(style) + " from " + place.Format(style));
                    sampleToPlace.Remove(sample);
                    placeToSample.Remove(place);
                    sampleToStyle.Remove(sample);
                    return sample;
                }
            }

            public SampleValue Clear(Place place, Style style, bool log = true) {
                CheckIsOccupied(place);
                if (log) Gui.Log("Device:   Clear " + place.Format(style));
                SampleValue sample = SampleOf(place);
                Remove(sample, style, log); 
                return sample;
            }

            public bool CanStepTo(Place from, Place to, int clearance) {
                for (int i = to.Row() - clearance; i < to.Row() + clearance; i++) {
                    for (int j = to.Column() - clearance; j < to.Column() + clearance; j++) {
                        Place[,] places = ProtocolDevice.device.places;
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
                Gui.gui.DeviceUpdate(); Thread.Sleep(device.phaseDelay);
                StepPhase(1, direction, from, to, style);
                Gui.gui.DeviceUpdate(); Thread.Sleep(device.phaseDelay);
                StepPhase(2, direction, from, to, style);
                Gui.gui.DeviceUpdate(); Thread.Sleep(device.phaseDelay);
                StepPhase(3, direction, from, to, style);
                Gui.gui.DeviceUpdate(); Thread.Sleep(device.stepDelay);
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
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
                place.SetAnimation(Animation.SizeHalf);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                place.SetAnimation(Animation.None);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // disappear
            public void Disappear(Place place, Style style) {
                place.SetAnimation(Animation.SizeHalf);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                place.SetAnimation(Animation.SizeQuarter);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
                Clear(place, style, log: false);
                place.SetAnimation(Animation.None);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // split Lft into Lft and Rht
            public void SplitHor(SampleValue splitLft, SampleValue splitRht, Place placeLft, Place placeRht, Style style) {
                placeLft.SetAnimation(Animation.PullRht);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                placeLft.SetAnimation(Animation.SplitRht);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
                Clear(placeLft, style, log: false);
                placeLft.SetAnimation(Animation.None);
                Place(splitLft, placeLft, style);
                placeRht.SetAnimation(Animation.None);
                Place(splitRht, placeRht, style);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // split Top into Top and Bot
            public void SplitVer(SampleValue splitTop, SampleValue splitBot, Place placeTop, Place placeBot, Style style) {
                placeTop.SetAnimation(Animation.PullBot);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                placeTop.SetAnimation(Animation.SplitBot);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
                Clear(placeTop, style);
                placeTop.SetAnimation(Animation.None);
                Place(splitTop, placeTop, style);
                placeBot.SetAnimation(Animation.None);
                Place(splitBot, placeBot, style);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // mix Lft and Rht into Lft
            public void MixHor(SampleValue sample, Place placeLft, Place placeRht, Style style) {
                placeLft.SetAnimation(Animation.PullRht);
                placeRht.SetAnimation(Animation.PullLft);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                Clear(placeLft, style);
                Clear(placeRht, style);
                placeLft.SetAnimation(Animation.None);
                placeRht.SetAnimation(Animation.None);
                Place(sample, placeLft, style);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.stepDelay);
            }

            // mix Top and Bot into Top
            public void MixVer(SampleValue sample, Place placeTop, Place placeBot, Style style) {
                placeTop.SetAnimation(Animation.PullBot);
                placeBot.SetAnimation(Animation.PullTop);
                Gui.gui.DeviceUpdate();
                Thread.Sleep(device.phaseDelay);
                Clear(placeTop, style);
                Clear(placeBot, style);
                placeTop.SetAnimation(Animation.None);
                placeBot.SetAnimation(Animation.None);
                Place(sample, placeTop, style);
                Gui.gui.DeviceUpdate();
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
                        Gui.gui.DeviceUpdate();
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
                Gui.Log("Device:   Move " + sample.FormatSymbol(style) + " from " + PlaceOf(sample, style).Format(style) + " to " + place.Format(style));
                FollowRoute(PathHorFirst(PlaceOf(sample, style), place), clearance, style, log:false);
            }

            public void MoveVerFirst(SampleValue sample, Place place, int clearance, Style style) {
                Gui.Log("Device:   Move " + sample.FormatSymbol(style) + " from " + PlaceOf(sample, style).Format(style) + " to " + place.Format(style));
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
