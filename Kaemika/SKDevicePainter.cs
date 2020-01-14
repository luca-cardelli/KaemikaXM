using System;
using SkiaSharp;

namespace Kaemika {

    public class SKDevicePainter : SKPainter, DevicePainter {
        public static SKBitmap cachedBackground = null; // keep this field public and static to make sure it is not garbage collected !!

        public SKDevicePainter(SKCanvas canvas) : base(canvas) {
        }

        public /*interface DevicePainter*/ void Draw(ProtocolDevice.Device device, int canvasX, int canvasY, int canvasWidth, int canvasHeight) { 

            (float margin, float padRadius, float deviceWidth, float deviceHeight) = device.FittedDimensions(canvasWidth, canvasHeight);
            float strokeWidth = padRadius / 10.0f;
            float accentStrokeWidth = 1.5f * strokeWidth;
            float textStrokeWidth = accentStrokeWidth / 2.0f;
            float deviceX = canvasX + (canvasWidth - deviceWidth) / 2.0f;
            float deviceY = canvasY + (canvasHeight - deviceHeight) / 2.0f;

            // don't lock device here, it will dedlock
            if (device.sizeChanged || cachedBackground == null || cachedBackground.Width != canvasWidth || cachedBackground.Height != canvasHeight) {
                cachedBackground = new SKBitmap(canvasWidth, canvasHeight);
                using (var backgroundCanvas = new SKCanvas(cachedBackground)) {
                    DrawDevice(device, backgroundCanvas, 
                        canvasX, canvasY, canvasWidth, canvasHeight,
                        deviceX, deviceY, deviceWidth, deviceHeight,
                        padRadius, margin, device.pinchPan);
                }
                device.sizeChanged = false;
            }

            if (!device.sizeChanged) 
                canvas.DrawBitmap(cachedBackground, 0, 0); // do not apply pinchPan: background bitmap is alread scaled by it

            if (device.displayPinchOrigin) {
                // same as: GraphSharp.GraphLayout.CanvasDrawCircle(canvas, pinchOrigin, 20, false, SKColors.LightGray);
                using (var paint = FillPaint(SKColors.LightGray)) { DrawCircle(device.pinchOrigin, 20, paint); }
                //using (var paint = new SKPaint()) {
                //    paint.TextSize = 10; paint.IsAntialias = true; paint.Color = SKColors.LightGray; paint.IsStroke = false;
                //    canvas.DrawCircle(device.pinchOrigin.X, device.pinchOrigin.Y, 20, paint);
                //}
            }

            using (var dropletFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = device.dropletColor, IsAntialias = true })
            using (var dropletBiggieFillPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = device.dropletBiggieColor, IsAntialias = true })
            {
                ProtocolDevice.Place[,] places = device.places;
                ProtocolDevice.Placement placement = device.placement;
                for (int row = 0; row < places.GetLength(0); row++) {
                    for (int col = 0; col < places.GetLength(1); col++) {
                        ProtocolDevice.Place place = places[row, col];
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
                            if (place.IsAnimation(ProtocolDevice.Animation.None))
                                DrawDroplet(canvas, label, biggie, here, 
                                    padRadius, volumeRadius, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SizeHalf))
                                DrawDroplet(canvas, label, biggie, here,
                                    padRadius, volumeRadius / 2, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SizeQuarter))
                                DrawDroplet(canvas, label, biggie, here,
                                    padRadius, volumeRadius / 4, textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullRht))
                                DrawDropletPulledHor(canvas, label, biggie, here, rht, ProtocolDevice.Direction.Rht,
                                    padRadius, volumeRadius * 5 / 6, volumeRadius * 5 / 12, volumeRadius * 1 / 3, 
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitRht))
                                DrawDropletPulledHor(canvas, label, biggie, here, rht, ProtocolDevice.Direction.Rht,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3, 
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullLft))
                                DrawDropletPulledHor(canvas, label, biggie, lft, here, ProtocolDevice.Direction.Lft,
                                    padRadius, volumeRadius * 1 / 3, volumeRadius * 5 / 12, volumeRadius * 5 / 6,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitLft))
                                DrawDropletPulledHor(canvas, label, biggie, lft, here, ProtocolDevice.Direction.Lft,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullBot))
                                DrawDropletPulledVer(canvas, label, biggie, here, bot, ProtocolDevice.Direction.Bot,
                                    padRadius, volumeRadius * 5 / 6, volumeRadius * 5 / 12, volumeRadius * 1 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitBot))
                                DrawDropletPulledVer(canvas, label, biggie, here, bot, ProtocolDevice.Direction.Bot,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.PullTop))
                                DrawDropletPulledVer(canvas, label, biggie, top, here, ProtocolDevice.Direction.Top,
                                    padRadius, volumeRadius * 1 / 3, volumeRadius * 5 / 12, volumeRadius * 5 / 6,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
                            if (place.IsAnimation(ProtocolDevice.Animation.SplitTop))
                                DrawDropletPulledVer(canvas, label, biggie, top, here, ProtocolDevice.Direction.Top,
                                    padRadius, volumeRadius * 2 / 3, volumeRadius * 1 / 3, volumeRadius * 2 / 3,
                                    textStrokeWidth, fillPaint, strokeWidth, accentStrokeWidth, device.pinchPan);
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
            using (var backPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = ProtocolDevice.deviceBackColor }) {
                canvas.DrawRect(new SKRect(canvasX, canvasY, canvasX + canvasWidth, canvasY + canvasHeight), backPaint);
            }
        }

        public void DrawDevice(ProtocolDevice.Device device, SKCanvas canvas, 
            float canvasX, float canvasY, float canvasWidth, float canvasHeight,
            float deviceX, float deviceY, float deviceWidth, float deviceHeight,
            float padRadius, float margin, Swipe swipe) {

            DrawBackground(canvas, canvasX, canvasY, canvasWidth, canvasHeight); // excluding the area outside the fitted region covered by the deviceImage

            float padStrokeWidth = padRadius / 10.0f;
            float padStrokeWidthAccent = 0.75f * padStrokeWidth;

            SKRect coldZoneRect = new SKRect(deviceX + margin, deviceY + margin, deviceX + margin + device.coldZoneWidth * 2 * padRadius, deviceY + margin + device.rowsNo * 2 * padRadius);
            SKRect hotZoneRect = new SKRect(deviceX + margin + (device.coldZoneWidth + device.warmZoneWidth) * 2 * padRadius, deviceY + margin, deviceX + margin + (device.coldZoneWidth + device.warmZoneWidth + device.hotZoneWidth) * 2 * padRadius, deviceY + margin + device.rowsNo * 2 * padRadius);

            DrawHeatZone(canvas, coldZoneRect, padRadius, coldZoneRect.Width / (2 * device.coldZoneWidth), coldColor, swipe);
            DrawHeatZone(canvas, hotZoneRect, padRadius, hotZoneRect.Width / (2 * device.hotZoneWidth), hotColor, swipe);
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
                for (int j = 0; j < device.rowsNo; j++) for (int i = 0; i < device.colsNo; i++)
                        DrawPad(canvas, new SKPoint(deviceX + margin + padRadius + i * 2 * padRadius, deviceY + margin + padRadius + j * 2 * padRadius), padRadius, strokePaint, strokePaintStrokeWidth, fillPaint, holePaint, strokeAccentPaint, strokeAccentPaintStrokeWidth, holeAccentPaint, swipe);
            }
        }

        public void DrawHeatZone(SKCanvas canvas, SKRect zone, float halo, float groove, SKColor color, Swipe swipe) {
            //SKMatrix scaleMatrix = SKMatrix.MakeScale(coldZoneRect.Width/coldZoneRect.Height, 1.0f);
            var radialGradient = SKShader.CreateRadialGradient(swipe % new SKPoint(zone.MidX, zone.MidY), swipe % groove, new SKColor[2] { color, ProtocolDevice.deviceBackColor }, null, SKShaderTileMode.Mirror); //, scaleMatrix);
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

            using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true })
            using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true }) {
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(darkPath, accentDarkPaint);
                canvas.DrawPath(lightPath, accentLightPaint);
                canvas.DrawPath(path, strokePaint);
            }

            DrawDropletLabel(canvas, label, center, radius, textStrokeWidth, biggie, swipe);
        }

        public void DrawDropletPulledHor(SKCanvas canvas, string label, bool biggie, SKPoint c1, SKPoint c2, ProtocolDevice.Direction dir, float r, float r1, float neckY, float r2, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe) {
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

            using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true })
            using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true }) {
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(darkPath, accentDarkPaint);
                canvas.DrawPath(lightPath, accentLightPaint);
                canvas.DrawPath(path, strokePaint);
            }

            float rText = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Lft) ? r1 : r2) : (r1 > r2) ? r1 : r2;
            SKPoint cText = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Lft) ? c1 : c2) : (r1 > r2) ? c1 : c2;
            DrawDropletLabel(canvas, label, cText, rText, textStrokeWidth, biggie, swipe);
        }

        public void DrawDropletPulledVer(SKCanvas canvas, string label, bool biggie, SKPoint c1, SKPoint c2, ProtocolDevice.Direction dir, float r, float r1, float neckX, float r2, float textStrokeWidth, SKPaint fillPaint, float strokeWidth, float accentStrokeWidth, Swipe swipe) {
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

            using (var strokePaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 191), StrokeWidth = swipe % strokeWidth, IsAntialias = true })
            using (var accentLightPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(255, 255, 255, 191), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true })
            using (var accentDarkPaint = new SKPaint { Style = SKPaintStyle.Stroke, Color = new SKColor(0, 0, 0, 95), StrokeWidth = swipe % accentStrokeWidth, StrokeCap = SKStrokeCap.Round, IsAntialias = true }) {
                canvas.DrawPath(path, fillPaint);
                canvas.DrawPath(darkPath, accentDarkPaint);
                canvas.DrawPath(lightPath, accentLightPaint);
                canvas.DrawPath(path, strokePaint);
            }

            float rYtext = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Top) ? r1 : r2) : (r1 > r2) ? r1 : r2;
            SKPoint cText = (r1 == r2) ? ((dir == ProtocolDevice.Direction.Top) ? c1 : c2) : (r1 > r2) ? c1 : c2;
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
}
